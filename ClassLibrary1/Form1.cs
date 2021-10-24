using Styletronix.CloudSyncProvider;
using System;
using System.Windows.Forms;

namespace ClassLibrary1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private SyncProvider SyncProvider;

        private void Form1_Load(object sender, EventArgs e)
        {
            var param = new SyncProviderParameters()
            {
                ProviderInfo = new BasicSyncProviderInfo()
                {
                    ProviderId = Guid.Parse(@"fdf0b5bb-be08-4544-b6f6-fa954e869a87"),
                    CLSID = @"{DBD02E04-87CD-4E46-A230-FCE0B7B70FEB}",
                    ProviderName = "SXTestProvider",
                    ProviderVersion = "0.0.1"
                },
                LocalDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\VirtualTest3",
                ServerProvider = new ServerProvider(@"D:\TEMP")
            };

            SyncProvider = new SyncProvider(param);
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            await SyncProvider.Start();
        }
        private async void Button5_Click(object sender, EventArgs e)
        {
            this.button5.Enabled = false;
            await SyncProvider.Stop();
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;
            await SyncProvider.RevertAllPlaceholders();
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            this.button3.Enabled = false;
            await SyncProvider.Unregister();
        }

        private async void Button4_Click(object sender, EventArgs e)
        {
            await SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Local);
        }

        private async void Button6_Click(object sender, EventArgs e)
        {
            await SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Full);
        }
    }
}
