
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using WalletConnectSharp.NEthereum;
using WalletConnectSharp.Unity;

namespace Web3Unity
{
    public class Web3Connect
    {
        public string RpcUrl { get; private set; }

        public string PrivateKey { get; private set; }

        public string ChainId { get; set; }

        public ConnectionType ConnectionType { get; private set; }

        public Web3WC Web3WC { get; private set; }

        public MetamaskProvider MetamaskProvider{ get; private set; }

        private static readonly Lazy<Web3Connect> lazy =
        new Lazy<Web3Connect>(() => new Web3Connect());

        public Web3 Web3 { get; private set; }


        public static Web3Connect Instance { get { return lazy.Value; } }

        private Web3Connect()
        {

        }


        /// <summary>
        /// Etablish a connection with nethereum classic RPC
        /// </summary>
        /// <param name="rpcUrl">rpc url to connect</param>
        /// <param name="privateKey">private key to sign call</param>
        public void ConnectRPC(string rpcUrl = "https://rpc.builder0x69.io", string privateKey = "0x3141592653589793238462643383279502884197169399375105820974944592")
        {
            ConnectionType = ConnectionType.RPC;
            PrivateKey = privateKey;
            RpcUrl = rpcUrl;
            var account = new Account(PrivateKey);
            Web3 = new Web3(account, RpcUrl);
        }

        /// <summary>
        /// Etablish a connection with metasmaks browser plugin (only for webGL)
        /// </summary>
        public void ConnectMetamask()
        {
            ConnectionType = ConnectionType.Metamask;
            MetamaskProvider = new MetamaskProvider();
            Web3 = new Web3(MetamaskProvider);
        }

        /// <summary>
        /// Etablish a connection with wallet connect
        /// </summary>
        /// <param name="rpcUrl">The rpc url to call contract</param>
        /// <param name="name">Name of the dapp who appears in the popin in the wallet</param>
        /// <param name="description">Description of the dapp</param>
        /// <param name="icon">Icon show on the popin</param>
        /// <param name="url">Url to the project</param>
        /// <returns>The uri to connect to wallet connect</returns>
        public string ConnectWalletConnect(string rpcUrl = "https://rpc.builder0x69.io", string name = "Test Unity", string description = "Test dapp", string icon = "https://unity.com/favicon.ico", string url = "https://unity.com/")
        {
            ConnectionType = ConnectionType.WalletConnect;
            RpcUrl = rpcUrl;
            Web3WC = new Web3WC(rpcUrl, name, description, icon, url);
            var wcProtocol = WalletConnect.Instance.Protocol;
            Web3 = new Web3(wcProtocol.CreateProvider(new Uri(RpcUrl)));

            return Web3WC.Uri;
        }

        private void Client_OnSessionConnect(object sender, WalletConnectSharp.Core.WalletConnectSession e)
        {
            //  Web3 = new Web3(Web3WC.Client.CreateProvider(new Uri(RpcUrl)));
           
        }

        public void Disconnect()
        {
        }
    }
}
