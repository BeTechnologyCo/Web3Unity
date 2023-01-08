using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TokenContract;
using WalletConnectSharp.Desktop;
using Web3Unity;

namespace Web3TestApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand RequestConnectionCommand { get; private set; }

        public ICommand GetTokenBalanceCommand { get; private set; }

        public ICommand ApproveCommand { get; private set; }

        private string uri;

        public string Uri
        {
            get { return uri; }
            set
            {
                uri = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            RequestConnectionCommand = new Command(() => Connect());
            GetTokenBalanceCommand = new Command(async () => TokenBalance());
            ApproveCommand = new Command(async () => Approve());

        }

        public void Connect()
        {
            Uri = Web3Connect.Instance.ConnectWalletConnect("https://rpc.ankr.com/fantom_testnet");
            //Uri = Web3Connect.Instance.Web3WC.Uri;
            Debug.WriteLine($"URI {Uri}");
        }

        public async Task TokenBalance()
        {

            // usdc on ethereum
            var contract = new TokenContractService("0x61A154Ef11d64309348CAA98FB75Bd82e58c9F89");
            //var tokenBalance = new TokenDefinition.BalanceOfFunction() { Account = "0xf977814e90da44bfa03b6295a0616a897441acec" };
            //var balance = await contract.Call<TokenDefinition.BalanceOfFunction, TokenDefinition.BalanceOfOutputDTO>(tokenBalance);

            var balance = await contract.BalanceOfQueryAsync("0xDBf0DC3b7921E9Ef897031db1DAe239B4E45Af5f");
            //Uri = Web3Connect.Instance.Web3WC.Uri;
            Debug.WriteLine($"balance {balance}");
        }

        public async Task Approve()
        {
            var contract = new TokenContractService("0x61A154Ef11d64309348CAA98FB75Bd82e58c9F89");
            var receipt = await contract.ApproveRequestAndWaitForReceiptAsync(new ApproveFunction() { FromAddress= "0xd6c39eff66c319c60ca4e85e6a5585a35c850f24", Amount = 10, Spender = "0x0b33fA091642107E3a63446947828AdaA188E276" });
            bool success = receipt.Succeeded();
            Debug.WriteLine($"receipt {success}");
        }

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
