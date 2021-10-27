using Styletronix;
using Styletronix.CloudSyncProvider;
using System;
using System.Windows.Forms;

namespace ClassLibrary1
{
    public partial class Form1 : Form
    {
        private SyncProvider SyncProvider;

        public Form1()
        {
            InitializeComponent();

            this.textBox_serverPath.Text = @"\\privatserver01.ama.local\Dokumente$";
            //this.textBox_serverPath.Text = @"D:\TEMP";
            this.textBox_localPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\VirtualTest3";
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            Styletronix.Debug.LogEvent += WriteEventToLog;
        }

        private void WriteEventToLog(object sender, Debug.LogEventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                this.textBox1.AppendText(e.Message + "\r\n");
            }));
        }

        private void InitProvider()
        {
            this.textBox_localPath.Enabled = false;
            this.textBox_serverPath.Enabled = false;

            if (SyncProvider == null)
            {
                var param = new SyncProviderParameters()
                {
                    ProviderInfo = new BasicSyncProviderInfo()
                    {
                        ProviderId = Guid.Parse(@"fdf0b5bb-be08-4544-b6f6-fa954e869a87"),
                        ProviderName = "SXTestProvider",
                        ProviderVersion = "0.0.1"
                    },
                    LocalDataPath = this.textBox_localPath.Text,
                    ServerProvider = new ServerProvider(this.textBox_serverPath.Text)
                };

                SyncProvider = new SyncProvider(param);
            }
        }
        private async void Button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;

            InitProvider();
            await SyncProvider.Start();
        }
        private async void Button5_Click(object sender, EventArgs e)
        {
            this.button5.Enabled = false;

            InitProvider();
            await SyncProvider.Stop();

            //SyncProvider.Dispose();
            //SyncProvider = null;
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;

            InitProvider();
            await SyncProvider.RevertAllPlaceholders();
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            this.button3.Enabled = false;

            InitProvider();
            await SyncProvider.Unregister();
        }

        private async void Button4_Click(object sender, EventArgs e)
        {
            InitProvider();
            await SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Local);
        }

        private async void Button6_Click(object sender, EventArgs e)
        {
            InitProvider();
            await SyncProvider.SyncDataAsync(SyncProvider.SyncMode.Full);
        }
    }
}
