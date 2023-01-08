
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace Web3Unity
{
    public class Web3Connect
    {
        public string RpcUrl { get; private set; }

        public string PrivateKey { get; private set; }

        public string ChainId { get; set; }

        public ConnectionType ConnectionType { get; private set; }

        public Web3WC Web3WC { get; private set; }

        private static readonly Lazy<Web3Connect> lazy =
        new Lazy<Web3Connect>(() => new Web3Connect());

        public Web3 Web3 { get; private set; }


        public static Web3Connect Instance { get { return lazy.Value; } }

        private Web3Connect()
        {

        }


        /// <summary>
        /// Etablish a connection with the desired provider
        /// </summary>
        /// <param name="connectionType">Connection Type 0 for direct RPC connection, 1 For WalletConnect, 2 for Metamask (Only in WebGL)</param>
        /// <param name="chainId">Chain Id you want to connect</param>
        /// <param name="rpcUrl">Rpc url, usefull for RPC & WalletConnect connexion</param>
        /// <param name="privateKey"></param>
        public void Connect(ConnectionType connectionType, string chainId = "1", string rpcUrl = "https://rpc.builder0x69.io", string privateKey = "0x3141592653589793238462643383279502884197169399375105820974944592")
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
            MetamaskProvider provider = new MetamaskProvider();
            Web3 = new Web3(provider);

        }

        /// <summary>
        /// Etablish a connection with wallet connect
        /// </summary>
        /// <param name="rpcUrl">the rpc url to call contract</param>
        /// <returns>the uri to connect to wallet connect</returns>
        public string ConnectWalletConnect(string rpcUrl = "https://rpc.builder0x69.io")
        {
            ConnectionType = ConnectionType.WalletConnect;
            RpcUrl = rpcUrl;
            Web3WC = new Web3WC(rpcUrl);
            Web3 = Web3WC.Web3Client;
            return Web3WC.Uri;
        }



        public void Disconnect()
        {
        }
    }
}
