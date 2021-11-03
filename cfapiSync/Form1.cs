using Styletronix;
using Styletronix.CloudSyncProvider;
using System;
using System.Windows.Forms;

namespace ClassLibrary1
{
    public partial class Form1 : Form
    {
        private SyncProvider SyncProvider;
        private readonly System.Threading.Timer refreshUITimer;

        public Form1()
        {
            InitializeComponent();
            var key = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Default);

            this.textBox_serverPath.Text = key.CreateSubKey(@"Software\Styletronix.net\cfapiSync", true).GetValue("serverPath", @"\\privatserver01.ama.local\Dokumente$").ToString();
            this.textBox_localPath.Text = key.CreateSubKey(@"Software\Styletronix.net\cfapiSync", true).GetValue("localPath", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\VirtualTest3").ToString();

            refreshUITimer = new(RefreshUITimerCallback, null, 1000, 200);
        }

        private void RefreshUITimerCallback(object objectState)
        {
            refreshUITimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            try
            {
                this.Invoke(() =>
                          {
                              this.label_QueueCount.Text = QueueStatus;
                              this.progressBar1.Value = Progress;

                              this.textBox1.SuspendLayout();
                              while (MessageQueue.TryDequeue(out string message))
                              {
                                  this.textBox1.AppendText(message + "\r\n");
                              }
                              this.textBox1.ResumeLayout();
                          });
            }
            finally
            {
                refreshUITimer.Change(200, System.Threading.Timeout.Infinite);
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            Styletronix.Debug.LogEvent += WriteEventToLog;
        }

        private void WriteEventToLog(object sender, Debug.LogEventArgs e)
        {
            MessageQueue.Enqueue(e.Message);
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
                SyncProvider.FileProgressEvent += SyncProvider_FileProgressEvent;
                SyncProvider.QueuedItemsCountChanged += SyncProvider_QueuedItemsCountChanged;
            }
        }

        private string QueueStatus = "";
        private short Progress;
        private System.Collections.Concurrent.ConcurrentQueue<string> MessageQueue = new();

        private void SyncProvider_QueuedItemsCountChanged(object sender, int e)
        {
            this.QueueStatus = e.ToString();
        }
        private void SyncProvider_FileProgressEvent(object sender, FileProgress e)
        {
            Progress = e.Progress;
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
            await SyncProvider.SyncDataAsync(SyncMode.Local);
        }

        private async void Button6_Click(object sender, EventArgs e)
        {
            InitProvider();
            await SyncProvider.SyncDataAsync(SyncMode.Full);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshUITimer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            refreshUITimer?.Dispose();  
        }
    }
}
