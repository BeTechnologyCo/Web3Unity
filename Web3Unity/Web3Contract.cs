using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

public class Web3Contract
{
    public string Address { get; set; }

    public string RpcUrl { get; set; }

    // Private key from etherjs doc (Address 0x7357589f8e367c2C31F51242fB77B350A11830F3)
    private string privateKey = "0x3141592653589793238462643383279502884197169399375105820974944592";

    public string PrivateKey
    {
        get { return privateKey; }
        set { privateKey = value; }
    }

    private Web3 web3;

    public bool UseMetamask { get; set; }


    public Web3Contract(string _address, bool _useMetamask = true)
    {
        this.Address = _address;
        this.UseMetamask = _useMetamask;
    }

    public Web3Contract(string _address, string _rpcUrl, bool _useMetamask = true, string _privateKey = "0x3141592653589793238462643383279502884197169399375105820974944592")
    {
        Address = _address;
        RpcUrl = _rpcUrl;
        PrivateKey = _privateKey;
        this.UseMetamask = _useMetamask;

        var account = new Account(PrivateKey);
        web3 = new Web3(account, RpcUrl);
    }


    public async Task<U> Call<T, U>(T _function) where T : FunctionMessage, new() where U : IFunctionOutputDTO, new()
    {
        if (IsWebGL() && UseMetamask)
        {
            return await Web3GL.Call<T, U>(_function, Address);
        }
        else
        {
            var contractHandler = web3.Eth.GetContractQueryHandler<T>();
            return await contractHandler.QueryAsync<U>(Address, _function);
        }

    }

    public async Task<string> Send<T>(T _function) where T : FunctionMessage, new()
    {
        if (IsWebGL() && UseMetamask)
        {
            return await Web3GL.Send<T>(_function, Address);
        }
        else if(IsDeeplinking() && UseMetamask)
        {
            return await Web3WalletConnect.Send<T>(_function, Address);
        }
        else
        {
            var contractHandler = web3.Eth.GetContractTransactionHandler<T>();
            return await contractHandler.SendRequestAsync(Address, _function);
        }
    }

    public async Task<TransactionReceipt> SendWaitForReceipt<T>(T _function) where T : FunctionMessage, new()
    {
        if (IsWebGL() && UseMetamask)
        {
            return await Web3GL.SendAndWaitForReceipt<T>(_function, Address);
        }
        else if (IsDeeplinking() && UseMetamask)
        {
            return await Web3WalletConnect.SendAndWaitForReceipt<T>(_function, Address);
        }
        else
        {
            var contractHandler = web3.Eth.GetContractTransactionHandler<T>();
            return await contractHandler.SendRequestAndWaitForReceiptAsync(Address, _function);
        }
    }

    public async Task<U> SendWaitForEvent<T, U>(T _function) where T : FunctionMessage, new() where U : new()
    {
        TransactionReceipt receipt = await SendWaitForReceipt(_function);

        if (receipt != null && receipt.Succeeded())
        {
            var events = receipt.DecodeAllEvents<U>();
            if (events.Count > 0)
            {
                return events[0].Event;
            }
            throw (new Exception("No event found"));
        }
        throw (new Exception($"Transaction failed, tx hash : ${receipt?.TransactionHash}"));
    }

    public async Task<List<U>> SendWaitForEventList<T, U>(T _function) where T : FunctionMessage, new() where U : new()
    {
        TransactionReceipt receipt = await SendWaitForReceipt(_function);
        if (receipt != null && receipt.Succeeded())
        {
            var events = receipt.DecodeAllEvents<U>();
            if (events.Count > 0)
            {
                return events.Select(x => x.Event).ToList();
            }
            throw (new Exception("No event found"));
        }
        throw (new Exception($"Transaction failed, tx hash : ${receipt?.TransactionHash}"));
    }


    public async Task<HexBigInteger> EstimateGas<T>(T _function) where T : FunctionMessage, new()
    {
        if (IsWebGL() && UseMetamask)
        {
            return await Web3GL.EstimateGas(_function, Address);
        }
        else if (IsDeeplinking() && UseMetamask)
        {
            return await Web3WalletConnect.EstimateGas<T>(_function, Address);
        }
        else
        {
            var contractHandler = web3.Eth.GetContractTransactionHandler<T>();
            return await contractHandler.EstimateGasAsync(Address, _function);
        }
    }

    public async Task<string> SignFunction<T>(T _function) where T : FunctionMessage, new()
    {
        if (IsWebGL() && UseMetamask)
        {
            return await Web3GL.SignFunction(_function, Address);
        }
        else if (IsDeeplinking() && UseMetamask)
        {
            return await Web3WalletConnect.SignFunction(_function, Address);
        }
        else
        {
            var contractHandler = web3.Eth.GetContractTransactionHandler<T>();
            return await contractHandler.SignTransactionAsync(Address, _function);
        }
    }

    public bool IsWebGL()
    {
#if UNITY_EDITOR
        return false;
#elif UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }

    public bool IsDeeplinking()
    {
#if UNITY_EDITOR
        return true;
#elif UNITY_IOS
        return true;
#elif UNITY_ANDROID
        return true;
#elif UNITY_WSA
        return true;
#elif UNITY_WSA_10_0
        return true;
#else
        return false;
#endif
    }
}
