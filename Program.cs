using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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
