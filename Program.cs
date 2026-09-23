using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    internal static class Program
    {
        private const string AppMutexName = "MJBillones_TDMS_SingleInstance_Mutex";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(true, AppMutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    bool activated = ActivateExistingInstance();
                    if (activated)
                    {
                        return;
                    }
                    try
                    {
                        if (!mutex.WaitOne(1500, false))
                        {
                            return;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                    }
                    catch
                    {
                        return;
                    }
                }

                ApplicationConfiguration.Initialize();

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => HandleException(e.Exception, "UI Thread");
                AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleException(e.ExceptionObject as Exception, "AppDomain");
                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    HandleException(e.Exception, "TaskScheduler");
                    e.SetObserved();
                };

                try
                {
                    Application.Run(new LoginForm());
                }
                catch (Exception ex)
                {
                    HandleException(ex, "Application.Run");
                }
            }
        }

        private static IntPtr FindProcessVisibleWindow(uint targetPid)
        {
            IntPtr found = IntPtr.Zero;
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == targetPid && IsWindowVisible(hWnd))
                    {
                        found = hWnd;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        private static bool ActivateExistingInstance()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                {
                    if (p.Id != current.Id)
                    {
                        IntPtr targetWnd = p.MainWindowHandle;
                        if (targetWnd == IntPtr.Zero)
                        {
                            targetWnd = FindProcessVisibleWindow((uint)p.Id);
                        }

                        if (targetWnd != IntPtr.Zero)
                        {
                            ShowWindowAsync(targetWnd, SW_RESTORE);
                            SetForegroundWindow(targetWnd);
                            return true;
                        }
                        else
                        {
                            try
                            {
                                p.Kill();
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static void HandleException(Exception? ex, string source)
        {
            if (ex == null) return;

            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                string errorEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(logPath, errorEntry);
            }
            catch { }

            MessageBox.Show(
                $"An unexpected system error occurred:{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details have been recorded in the local log.",
                "System Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
