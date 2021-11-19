using System;
using Vanara.PInvoke;

namespace Styletronix
{
    public class Debug
    {
        public static System.Diagnostics.TraceSwitch generalSwitch = new("General", "Entire Application") { Level = System.Diagnostics.TraceLevel.Info };

        public static void LogResponse(HRESULT hResult)
        {
            if (hResult != HRESULT.S_OK)
            {
                Debug.WriteLine(hResult.GetException().Message, System.Diagnostics.TraceLevel.Error);
            }
        }
        public static void LogException(Exception ex)
        {
            Debug.WriteLine(ex.ToString(), System.Diagnostics.TraceLevel.Error);
        }

        public static void WriteLine(string value)
        {
            WriteLine(value, System.Diagnostics.TraceLevel.Verbose);
        }

        public static void WriteLine(string value, string category, System.Diagnostics.TraceLevel traceLevel)
        {
            if (generalSwitch.Level >= traceLevel)
            {
                System.Diagnostics.Debug.WriteLine(value, category);
                LogEvent?.Invoke(null, new LogEventArgs() { TraceLevel = traceLevel, Message = value, Category = category });
            }

        }
        public static void WriteLine(string value, System.Diagnostics.TraceLevel traceLevel)
        {
            WriteLine(value, null, traceLevel);
        }


        public static event EventHandler<LogEventArgs> LogEvent;

        public class LogEventArgs
        {
            public System.Diagnostics.TraceLevel TraceLevel;
            public string Message;
            public string Category;
        }

    }
}
