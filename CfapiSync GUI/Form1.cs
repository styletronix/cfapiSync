using Styletronix;
using Styletronix.CloudSyncProvider;
using System;
using System.Windows.Forms;

namespace CfapiSync_GUI
{
    public partial class Form1 : Form
    {
        private SyncProvider SyncProvider;
        private System.Threading.Timer refreshUITimer;

        public Form1()
        {
            InitializeComponent();
        }

        private void RefreshUITimerCallback(object objectState)
        {
            refreshUITimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            try
            {
                Invoke(() =>
                          {
                              label_QueueCount.Text = QueueStatus;
                              progressBar1.Value = Progress;


                              //textBox1.SuspendLayout();
                              while (MessageQueue.TryDequeue(out string message))
                              {
                                  textBox1.AppendText(message + "\r\n");
                              }
                              //textBox1.ResumeLayout();
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

            var args = Environment.GetCommandLineArgs();
            int i = 1;
            bool anyCMD = false;
            bool hasLocalPath = false;

            while (i < args.Length - 1)
            {
                var command = args[i];
                var param = args[i + 1];

                switch (command.ToLower())
                {
                    case "/serverpath":
                        textBox_serverPath.Text = param;
                        anyCMD = true;
                        break;

                    case "/localpath":
                        textBox_localPath.Text = param;
                        anyCMD = true;
                        hasLocalPath = true;
                        break;

                    case "/caption":
                        textBox_Caption.Text = param;
                        anyCMD = true;
                        break;

                }
                i += 2;
            }

            refreshUITimer = new(RefreshUITimerCallback, null, 1000, 500);

            if (!anyCMD)
            {
                textBox_serverPath.Text = SyncProviderUtils.GetUserSetting("ServerPath", "Provider\\1", @"\\privatserver01.ama.local\Dokumente$").ToString();
                textBox_localPath.Text = SyncProviderUtils.GetUserSetting("LocalPath", "Provider\\1", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\VirtualTest").ToString();
                textBox_Caption.Text = SyncProviderUtils.GetUserSetting("Caption", "Provider\\1", @"CfapiSync").ToString();
            }
            else
            {
                if (!hasLocalPath)
                {
                    textBox_localPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\" + textBox_Caption.Text;
                }

                button1.Enabled = false;

                InitProvider();
                SyncProvider.Start();
            }
        }

        private void WriteEventToLog(object sender, Debug.LogEventArgs e)
        {
            MessageQueue.Enqueue(e.Message);
        }
        private string GetAssemblyGUID()
        {
            string id = "";
            foreach (object attr in System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(true))
            {
                if (attr is System.Runtime.InteropServices.GuidAttribute)
                    id = ((System.Runtime.InteropServices.GuidAttribute)attr).Value;
            }
            return id;
        }
        private void InitProvider()
        {
            textBox_localPath.Enabled = false;
            textBox_serverPath.Enabled = false;
            textBox_Caption.Enabled = false;

            if (SyncProvider == null)
            {
                SyncProviderParameters param = new()
                {
                    ProviderInfo = new BasicSyncProviderInfo()
                    {
                        ProviderId = Guid.Parse(GetAssemblyGUID()),  // ProviderID must be unique for each Application
                        ProviderName = textBox_Caption.Text,
                        ProviderVersion = Application.ProductVersion
                    },
                    LocalDataPath = textBox_localPath.Text,
                    ServerProvider = new LocalNetworkServerProvider(textBox_serverPath.Text)
                };

                SyncProvider = new SyncProvider(param);
                SyncProvider.FileProgressEvent += SyncProvider_FileProgressEvent;
                SyncProvider.QueuedItemsCountChanged += SyncProvider_QueuedItemsCountChanged;
            }
        }

        private string QueueStatus = "";
        private short Progress;
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> MessageQueue = new();

        private void SyncProvider_QueuedItemsCountChanged(object sender, int e)
        {
            QueueStatus = e.ToString();
        }
        private void SyncProvider_FileProgressEvent(object sender, FileProgressEventArgs e)
        {
            Progress = e.Progress;
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            InitProvider();
            await SyncProvider.Start();
        }
        private async void Button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;

            InitProvider();
            await SyncProvider.Stop();

            //SyncProvider.Dispose();
            //SyncProvider = null;
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;

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
