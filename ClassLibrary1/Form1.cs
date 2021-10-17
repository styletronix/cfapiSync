using Styletronix.CloudSyncProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClassLibrary1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private SyncProvider _SyncProvider;
        private System.Threading.CancellationTokenSource startCTX;

        private void Form1_Load(object sender, EventArgs e)
        {
            var param = new Styletronix.CloudSyncProvider.SyncProviderParameters()
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
            this._SyncProvider = new SyncProvider(param);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (startCTX != null) { startCTX.Cancel(); }
            startCTX = new System.Threading.CancellationTokenSource();

            await this._SyncProvider.Start(startCTX.Token);
        }
        private void button5_Click(object sender, EventArgs e)
        {
            if (startCTX != null) { startCTX.Cancel(); }
            this._SyncProvider.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (startCTX != null) { startCTX.Cancel(); }

            this._SyncProvider.RevertAllPlaceholders(new System.Threading.CancellationToken() );
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (startCTX != null) { startCTX.Cancel(); }
            this._SyncProvider.Unregister();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            using (var ctx = new System.Threading.CancellationTokenSource())
            {
                await this._SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Local,ctx.Token);
            }
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            using (var ctx = new System.Threading.CancellationTokenSource())
            {
                await this._SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Full, ctx.Token);
            }
        }
    }
}
