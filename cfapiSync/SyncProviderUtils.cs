using Microsoft.Win32;
using Styletronix.CloudSyncProvider;
using System;
using System.Threading;
using static Vanara.PInvoke.CldApi;

public class SyncProviderUtils
{
    public class DataActions
    {
        public long FileOffset;
        public long Length;
        public string NormalizedPath;
        public CF_TRANSFER_KEY TransferKey;
        public CF_REQUEST_KEY RequestKey;
        public byte PriorityHint;
        public CancellationTokenSource CancellationTokenSource;
        public Guid guid = Guid.NewGuid();

        public bool isCompleted;

        public string Id;
    }
    public class FetchRange
    {
        public FetchRange() { }
        public FetchRange(DataActions data)
        {
            NormalizedPath = data.NormalizedPath;
            PriorityHint = data.PriorityHint;
            RangeStart = data.FileOffset;
            RangeEnd = data.FileOffset + data.Length;
            TransferKey = data.TransferKey;
        }

        public long RangeStart;
        public long RangeEnd;
        public string NormalizedPath;
        public CF_TRANSFER_KEY TransferKey;
        public byte PriorityHint;
    }

    public static void SetUserSetting(string name, string categorie, object value, RegistryValueKind valueKind)
    {
        if (string.IsNullOrEmpty(categorie))
        {
            categorie = "";
        }
        else
        {
            categorie = "\\" + categorie;
        }

        using var myKey = Registry.CurrentUser.CreateSubKey("Software\\Styletronix.net\\CfapiSync{0}" + categorie);

        if (value == null)
        {
            myKey.DeleteValue(name);
        }
        else
        {
            myKey.SetValue(name, value, valueKind);
        }

        myKey.Close();
    }
    public static object GetUserSetting(string name, string categorie, object defaultValue)
    {
        if (string.IsNullOrEmpty(categorie))
        {
            categorie = "";
        }
        else
        {
            categorie = "\\" + categorie;
        }

        using var myKey = Registry.CurrentUser.OpenSubKey("Software\\Styletronix.net\\CfapiSync{0}" + categorie);
       
        if (myKey == null)
            return defaultValue;    

        return myKey.GetValue(name, defaultValue);
    }
}