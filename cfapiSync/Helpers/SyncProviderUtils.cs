using Microsoft.Win32;

public class SyncProviderUtils
{
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

        using RegistryKey myKey = Registry.CurrentUser.CreateSubKey("Software\\Styletronix.net\\CfapiSync{0}" + categorie);

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

        using RegistryKey myKey = Registry.CurrentUser.OpenSubKey("Software\\Styletronix.net\\CfapiSync{0}" + categorie);

        if (myKey == null)
        {
            return defaultValue;
        }

        return myKey.GetValue(name, defaultValue);
    }
}