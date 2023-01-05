using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using System.Diagnostics;
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.Desktop;
using WalletConnectSharp.NEthereum;


namespace Web3Unity
{
    public class Test
    {
        private string uri = string.Empty;
        private WalletConnect client;
        public Test()
        {
            var metadata = new ClientMeta()
            {
                Description = "This is a test of the Nethereum.WalletConnect feature",
                Icons = new[] { "https://app.warriders.com/favicon.ico" },
                Name = "WalletConnect Test",
                URL = "https://app.warriders.com"
            };
            client = new WalletConnect(metadata);
            //var nethereum = new Web3(walletConnect.CreateProvider(new Uri("https//rpc.testnet.fantom.network/")));
            client.OnSessionCreated += Client_OnSessionCreated;
            client.OnTransportConnect += Client_OnTransportConnect;
            client.OnSend += Client_OnSend;
            client.OnSessionConnect += Client_OnSessionConnect;
            uri = client.URI;

            Connect();
        }

        private void Client_OnSessionConnect(object? sender, WalletConnectSharp.Core.WalletConnectSession e)
        {

            Debug.WriteLine($"session connect");
        }

        private void Client_OnSend(object? sender, WalletConnectSharp.Core.WalletConnectSession e)
        {
            Debug.WriteLine($"send");
        }

        private void Client_OnTransportConnect(object? sender, WalletConnectSharp.Core.WalletConnectProtocol e)
        {
            Debug.WriteLine($"Transport");
        }

        private void Client_OnSessionCreated(object? sender, WalletConnectSharp.Core.WalletConnectSession e)
        {
            Debug.WriteLine($"session");
        }

        public async Task Connect()
        {
            await client.Connect();

            Debug.WriteLine($"Address: {client.Accounts[0]}");
            Debug.WriteLine($"Chain ID: {client.ChainId}");

        }

        public string GetUri()
        {
            return uri;
        }
    }
}
