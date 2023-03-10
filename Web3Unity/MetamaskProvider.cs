using AOT;
using Cysharp.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using RpcError = Nethereum.JsonRpc.Client.RpcError;

namespace Web3Unity
{
    public class MetamaskProvider : IClient
    {

        [DllImport("__Internal")]
        private static extern void Connect(Action<int, string> callback);

        [DllImport("__Internal")]
        private static extern void Request(string jsonCall, Action<int, string> callback);

        [DllImport("__Internal")]
        private static extern bool IsMetamaskAvailable();

        [DllImport("__Internal")]
        private static extern string GetSelectedAddress();

        [DllImport("__Internal")]
        private static extern bool IsConnected();

        private static int id = 0;

        public static event EventHandler<string> OnAccountConnected;
        public static event EventHandler<string> OnAccountChanged;
        public static event EventHandler<BigInteger> OnChainChanged;
        public static event EventHandler OnAccountDisconnected;

        private static Dictionary<int, TaskCompletionSource<string>> utcs = new Dictionary<int, TaskCompletionSource<string>>();
        private static TaskCompletionSource<string> utcsConnected;

        public RequestInterceptor OverridingRequestInterceptor { get; set; }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        private static void RequestCallResult(int key, string val)
        {
            if (utcs.ContainsKey(key))
            {
                utcs[key].TrySetResult(val);
                Debug.Log($"Key found Web3GL {key}");
                Debug.Log($"val Web3GL {val}");
            }
            else
            {
                Debug.LogWarning($"Key not found Web3GL {key}");
            }
        }

        [MonoPInvokeCallback(typeof(Action<int, string>))]
        private static void Connected(int changeType, string result)
        {
            switch (changeType)
            {
                case 1:
                    utcsConnected?.TrySetResult(result);
                    if (OnAccountConnected != null)
                    {
                        OnAccountConnected(Web3Connect.Instance.MetamaskProvider, result);
                    }

                    break;
                case 2:
                    if (OnChainChanged != null)
                    {
                        OnChainChanged(Web3Connect.Instance.MetamaskProvider, BigInteger.Parse(result));
                    }
                    break;
                case 3:
                    if (OnAccountChanged != null)
                    {
                        OnAccountChanged(Web3Connect.Instance.MetamaskProvider, result);
                    }
                    break;
                case 4:
                    if (OnAccountDisconnected != null)
                    {
                        OnAccountDisconnected(Web3Connect.Instance.MetamaskProvider, new EventArgs());
                    }
                    break;
            }

        }

        public async UniTask<RpcResponseMessage> RequestCallAsync(int val, string jsonCall)
        {
            utcs[val] = new TaskCompletionSource<string>();
            Request(jsonCall, RequestCallResult);
            string result = await utcs[val].Task;
            return JsonConvert.DeserializeObject<RpcResponseMessage>(result);
        }


        public MetamaskProvider(bool autoConnect)
        {
            if (autoConnect)
            {
                ConnectAccount();
            }
        }


        public async Task<string> ConnectAccount()
        {
            utcsConnected = new TaskCompletionSource<string>();
            Connect(Connected);
            string result = await utcsConnected.Task;
            return result;
        }

        public async Task<RpcRequestResponseBatch> SendBatchRequestAsync(RpcRequestResponseBatch rpcRequestResponseBatch)
        {
            foreach (var i in rpcRequestResponseBatch.BatchItems)
            {
                var request = i.RpcRequestMessage;
                RpcResponseMessage response = await SendAsync(request.Method, request.RawParameters);
                var resp = new RpcResponseMessage(request.Id, response.Result);
                rpcRequestResponseBatch.UpdateBatchItemResponses(new List<RpcResponseMessage>() { resp });
            }
            return rpcRequestResponseBatch;
        }

        public async Task<T> SendRequestAsync<T>(RpcRequest request, string route = null)
        {
            Debug.Log($"SendRequestAsync T {typeof(T)}");
            RpcResponseMessage response = await SendAsync(request.Method, request.RawParameters);
            Debug.Log($"Response  {response.Result}");
            try
            {
                var result = response.GetResult<T>();
                Debug.Log($"Result  {result}");
                return result;
            }
            catch (FormatException formatException)
            {
                throw new RpcResponseFormatException("Invalid format found in RPC response", formatException);
            }
        }

        public async Task<T> SendRequestAsync<T>(string method, string route = null, params object[] paramList)
        {
            RpcResponseMessage response = await SendAsync(method, paramList);
            Debug.Log($"SendRequestAsync Method T {typeof(T)}");
            try
            {
                return response.GetResult<T>();
            }
            catch (FormatException formatException)
            {
                throw new RpcResponseFormatException("Invalid format found in RPC response", formatException);
            }
        }

        public async Task SendRequestAsync(RpcRequest request, string route = null)
        {
            await SendAsync(request.Method, request.RawParameters);
        }

        public async Task SendRequestAsync(string method, string route = null, params object[] paramList)
        {
            await SendAsync(method, paramList);
        }

        private async Task<RpcResponseMessage> SendAsync(string method, params object[] paramList)
        {
            int val = ++id;
            var account = GetSelectedAddress();
            if (paramList != null && paramList.Length > 0)
            {
                var callInput = paramList[0] as CallInput;
                if (callInput != null)
                {
                    callInput.From = account;
                }
                else
                {
                    var transactionInput = paramList[0] as TransactionInput;
                    if (transactionInput != null)
                    {
                        transactionInput.From = account;
                    }
                }
            }
            MetamaskRequest rpcRequest = new MetamaskRequest(val, method, account, paramList);

            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            HandleRpcError(response, method);
            return response;
        }

        protected void HandleRpcError(RpcResponseMessage response, string reqMsg)
        {
            if (response.HasError)
                throw new RpcResponseException(new RpcError(response.Error.Code, response.Error.Message + ": " + reqMsg,
                    response.Error.Data));
        }

        public  async Task<U> Call<T, U>(T _function, string _address) where T : FunctionMessage, new() where U : IFunctionOutputDTO, new()
        {

            var callInput = _function.CreateCallInput(_address);
            var account = GetSelectedAddress();
            callInput.From = account;
            var parameters = new object[1] { callInput };
            int val = ++id;
            RpcRequestMessage rpcRequest = new RpcRequestMessage(val, "eth_call", parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            Console.WriteLine("jsoncall " + jsonCall);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            Console.WriteLine("result " + response.GetResult<string>());
            var decode = new FunctionCallDecoder().DecodeFunctionOutput<U>(response.GetResult<string>());
            return decode;
        }


        public async Task<string> Send<T>(T _function, string _address) where T : FunctionMessage, new()
        {
            var transactioninput = _function.CreateTransactionInput(_address);
            var account = GetSelectedAddress();
            transactioninput.From = account;
            var parameters = new object[1] { transactioninput };
            int val = ++id;
            MetamaskRequest rpcRequest = new MetamaskRequest(val, "eth_sendTransaction", account, parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            Console.WriteLine("result " + response.GetResult<string>());
            return response.GetResult<string>();
        }

        public async Task<TransactionReceipt> SendAndWaitForReceipt<T>(T _function, string _address) where T : FunctionMessage, new()
        {
            var getReceipt = await Send(_function, _address);
            var parameters = new object[1] { getReceipt };
            int val = ++id;
            RpcRequestMessage rpcRequest = new RpcRequestMessage(val, "eth_getTransactionReceipt", parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            TransactionReceipt transaction = response.GetResult<TransactionReceipt>();
            Console.WriteLine("result " + transaction.TransactionHash);
            return transaction;
        }

        public  async Task<HexBigInteger> EstimateGas<T>(T _function, string _address) where T : FunctionMessage, new()
        {
            var transactioninput = _function.CreateTransactionInput(_address);
            var account = GetSelectedAddress();
            transactioninput.From = account;
            var parameters = new object[1] { transactioninput };
            int val = ++id;
            MetamaskRequest rpcRequest = new MetamaskRequest(val, "eth_estimateGas", account, parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            Console.WriteLine("result " + response.GetResult<HexBigInteger>());
            return response.GetResult<HexBigInteger>();
        }

        public  async Task<string> SignFunction<T>(T _function, string _address) where T : FunctionMessage, new()
        {
            string account = GetSelectedAddress();
            var transactioninput = _function.CreateTransactionInput(_address);           
            transactioninput.From = account;
            var parameters = new object[2] { account, transactioninput.Value };
            int val = ++id;
            RpcRequestMessage rpcRequest = new RpcRequestMessage(val, "eth_sign", parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            Console.WriteLine("jsoncall " + jsonCall);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            Console.WriteLine("result " + response.GetResult<string>());
            return response.GetResult<string>();
        }

        public async Task<string> Sign(string message, MetamaskSignature sign)
        {
            var account = GetSelectedAddress();
            var parameters = new object[2] { GetSelectedAddress(), message };
            int val = ++id;
            RpcRequestMessage rpcRequest = new RpcRequestMessage(val, Enum.GetName(typeof(MetamaskSignature), sign), parameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            if (!string.IsNullOrEmpty(response.Error?.Message))
            {
                throw new Exception(response.Error?.Message);
            }
            Console.WriteLine("result " + response.GetResult<string>());
            return response.GetResult<string>();
        }
    }
}