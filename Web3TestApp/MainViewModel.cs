using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WalletConnectSharp.Desktop;
using Web3Unity;

namespace Web3TestApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand RequestConnectionCommand { get; private set; }

        public ICommand GetTokenBalanceCommand { get; private set; }

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
        }

        public void Connect()
        {
            Uri = Web3Connect.Instance.ConnectWalletConnect();
            //Uri = Web3Connect.Instance.Web3WC.Uri;
            Debug.WriteLine($"URI {Uri}");
        }

        public async Task TokenBalance()
        {

            // usdc on ethereum
            var contract = new Web3Contract("0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48");
            var tokenBalance = new TokenDefinition.BalanceOfFunction() { Account = "0xf977814e90da44bfa03b6295a0616a897441acec" };
            var balance = await contract.Call<TokenDefinition.BalanceOfFunction, TokenDefinition.BalanceOfOutputDTO>(tokenBalance);

            //Uri = Web3Connect.Instance.Web3WC.Uri;
            Debug.WriteLine($"balance {balance.ReturnValue1}");
        }

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
