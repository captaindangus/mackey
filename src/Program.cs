using System;
using System.Threading;
using System.Windows.Forms;

namespace MacKey
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool startHidden = false;
            bool replace = false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase))
                    startHidden = true;
                else if (string.Equals(arg, "--replace", StringComparison.OrdinalIgnoreCase))
                    replace = true;
            }

            const string mutexName = "MacKey.SingleInstance.v1";
            Mutex mutex = null;
            bool owned = false;

            try
            {
                mutex = new Mutex(true, mutexName, out owned);
                if (!owned)
                {
                    if (replace)
                    {
                        try
                        {
                            owned = mutex.WaitOne(5000);
                        }
                        catch (AbandonedMutexException)
                        {
                            owned = true;
                        }
                    }

                    if (!owned)
                    {
                        Native.PostMessage(
                            (IntPtr)Native.HWND_BROADCAST,
                            AppMessages.Show,
                            IntPtr.Zero,
                            IntPtr.Zero);
                        return;
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                AppController controller = new AppController(startHidden);
                Application.Run(controller);
            }
            finally
            {
                if (mutex != null)
                {
                    try
                    {
                        if (owned) mutex.ReleaseMutex();
                    }
                    catch
                    {
                    }
                    mutex.Dispose();
                }
            }
        }
    }
}
