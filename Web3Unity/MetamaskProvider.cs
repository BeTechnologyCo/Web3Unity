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
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using RpcError = Nethereum.JsonRpc.Client.RpcError;

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

    private static Dictionary<int, UniTaskCompletionSource<string>> utcs = new Dictionary<int, UniTaskCompletionSource<string>>();
    private static UniTaskCompletionSource<string> utcsConnected;

    public RequestInterceptor OverridingRequestInterceptor { get; set; }

    [MonoPInvokeCallback(typeof(Action<int, string>))]
    private static void RequestCallResult(int key, string val)
    {
        if (utcs.ContainsKey(key))
        {
            utcs[key].TrySetResult(val);
            Debug.Log($"Key found Web3GL {key}");
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
                    OnAccountConnected(new Web3GL(), result);
                }

                break;
            case 2:
                if (OnChainChanged != null)
                {
                    OnChainChanged(new Web3GL(), BigInteger.Parse(result));
                }
                break;
            case 3:
                if (OnAccountChanged != null)
                {
                    OnAccountChanged(new Web3GL(), result);
                }
                break;
            case 4:
                if (OnAccountDisconnected != null)
                {
                    OnAccountDisconnected(new Web3GL(), new EventArgs());
                }
                break;
        }

    }

    public async UniTask<RpcResponseMessage> RequestCallAsync(int val, string jsonCall)
    {
        utcs[val] = new UniTaskCompletionSource<string>();
        Request(jsonCall, RequestCallResult);
        string result = await utcs[val].Task;
        return JsonConvert.DeserializeObject<RpcResponseMessage>(result);
    }


    public MetamaskProvider()
    {
        ConnectAccount();
    }


    public async Task<string> ConnectAccount()
    {
        utcsConnected = new UniTaskCompletionSource<string>();
        Connect(Connected);
        string result = await utcsConnected.Task;
        return result;
    }

    public async Task<RpcRequestResponseBatch> SendBatchRequestAsync(RpcRequestResponseBatch rpcRequestResponseBatch)
    {
        foreach (var i in rpcRequestResponseBatch.BatchItems)
        {
            var request = i.RpcRequestMessage;
            int val = ++id;
            MetamaskRequest rpcRequest = new MetamaskRequest(val, request.Method, GetSelectedAddress(), request.RawParameters);
            var jsonCall = JsonConvert.SerializeObject(rpcRequest);
            RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
            var resp = new RpcResponseMessage(request.Id, response.Result);
            rpcRequestResponseBatch.UpdateBatchItemResponses(new List<RpcResponseMessage>() { resp });
        }
        return rpcRequestResponseBatch;
    }

    public async Task<T> SendRequestAsync<T>(RpcRequest request, string route = null)
    {
        int val = ++id;
        MetamaskRequest rpcRequest = new MetamaskRequest(val, request.Method, GetSelectedAddress(), null);
        var jsonCall = JsonConvert.SerializeObject(rpcRequest);
        RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
        HandleRpcError(response, request.Method);
        try
        {
            return response.GetResult<T>();
        }
        catch (FormatException formatException)
        {
            throw new RpcResponseFormatException("Invalid format found in RPC response", formatException);
        }
    }

    public async Task<T> SendRequestAsync<T>(string method, string route = null, params object[] paramList)
    {
        int val = ++id;
        MetamaskRequest rpcRequest = new MetamaskRequest(val, method, GetSelectedAddress(), paramList);
        var jsonCall = JsonConvert.SerializeObject(rpcRequest);
        RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
        HandleRpcError(response, method);
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
        int val = ++id;
        MetamaskRequest rpcRequest = new MetamaskRequest(val, request.Method, GetSelectedAddress(), null);
        var jsonCall = JsonConvert.SerializeObject(rpcRequest);
        RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
        HandleRpcError(response, request.Method);
    }

    public async Task SendRequestAsync(string method, string route = null, params object[] paramList)
    {
        int val = ++id;
        MetamaskRequest rpcRequest = new MetamaskRequest(val, method, GetSelectedAddress(), paramList);
        var jsonCall = JsonConvert.SerializeObject(rpcRequest);
        RpcResponseMessage response = await RequestCallAsync(val, jsonCall);
        HandleRpcError(response, method);
    }

    protected void HandleRpcError(RpcResponseMessage response, string reqMsg)
    {
        if (response.HasError)
            throw new RpcResponseException(new RpcError(response.Error.Code, response.Error.Message + ": " + reqMsg,
                response.Error.Data));
    }
}