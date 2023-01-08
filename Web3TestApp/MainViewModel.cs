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

        public ICommand RequestConnection { get; private set; }

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
            RequestConnection = new Command(() => Connect());
        }

        public void Connect()
        {
            Uri = Web3Connect.Instance.ConnectWalletConnect();
            //Uri = Web3Connect.Instance.Web3WC.Uri;
            Debug.WriteLine($"URI {Uri}");
        }

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
