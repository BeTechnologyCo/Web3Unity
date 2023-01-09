using System.Text;

namespace Web3TestApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc = System.Text.Encoding.GetEncoding("gb2312");
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}