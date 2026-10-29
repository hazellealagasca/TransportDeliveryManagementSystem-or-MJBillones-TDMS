using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// The app's error log (Logs\errors_yyyy-MM.log in the data folder). Problems that used to be ignored silently are
    /// written here with the screen, file and line, so a failed save or print can be traced afterwards.
    /// The same spot is written at most once a minute, and each monthly file is capped at 5 MB.
    /// </summary>
    public static class AppLog
    {
        private static readonly object gate = new();
        private static readonly Dictionary<string, DateTime> lastWrite = new();
        private const long MaxBytes = 5L * 1024 * 1024;
        public static string ErrorLogFile => Path.Combine(SafeLogs(), $"errors_{DateTime.Now:yyyy-MM}.log");
        public static string CrashLogFile => Path.Combine(SafeLogs(), "crash_log.txt");
        private static string SafeLogs()
        {
            try { string d = AppPaths.Logs; Directory.CreateDirectory(d); return d; }
            catch { return AppDomain.CurrentDomain.BaseDirectory; }
        }
        /// <summary>An error that the code recovered from (it used to be ignored). Logged quietly, no message shown.</summary>
        public static void Swallowed(Exception ex, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            // Expected on every start-up: the "add this column if it's missing" upgrades.
            if (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) return;
            Write("WARN", $"{Path.GetFileNameWithoutExtension(file)}.{member}:{line}", ex, null, throttle: true);
        }
        public static void Error(string where, Exception ex, string? note = null) => Write("ERROR", where, ex, note, throttle: false);
        public static void Info(string where, string message) => Write("INFO", where, null, message, throttle: false);
        public static void Crash(string where, Exception ex)
        {
            try
            {
                lock (gate)
                    File.AppendAllText(CrashLogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{where}] {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { }
            Write("CRASH", where, ex, null, throttle: false);
        }
        [ThreadStatic] private static bool writing;
        private static void Write(string level, string where, Exception? ex, string? note, bool throttle)
        {
            if (writing) return; // never log from inside the logger
            writing = true;
            try
            {
                lock (gate)
                {
                    if (throttle)
                    {
                        string key = where + "|" + ex?.GetType().Name;
                        if (lastWrite.TryGetValue(key, out var t) && (DateTime.Now - t).TotalSeconds < 60) return;
                        lastWrite[key] = DateTime.Now;
                    }
                    string path = ErrorLogFile;
                    try { if (File.Exists(path) && new FileInfo(path).Length > MaxBytes) File.Move(path, path.Replace(".log", $"_{DateTime.Now:ddHHmmss}.log")); } catch { }
                    var sb = new StringBuilder();
                    sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level} {where}");
                    try { string u = SessionContext.CurrentUsername; if (!string.IsNullOrEmpty(u)) sb.Append($" (user {u})"); } catch { }
                    if (!string.IsNullOrEmpty(note)) sb.Append(" — ").Append(note);
                    sb.AppendLine();
                    if (ex != null)
                    {
                        sb.AppendLine($"    {ex.GetType().Name}: {ex.Message}");
                        if (level != "WARN" && ex.StackTrace != null) sb.AppendLine(Indent(ex.StackTrace));
                        else if (ex.StackTrace != null) { var first = ex.StackTrace.Split('\n'); sb.AppendLine("    " + first[0].Trim()); }
                    }
                    File.AppendAllText(path, sb.ToString());
                }
            }
            catch { }
            finally { writing = false; }
            System.Diagnostics.Debug.WriteLine($"[{level}] {where}: {ex?.Message ?? note}");
        }
        private static string Indent(string s) => "    " + s.Replace("\n", "\n    ");
        /// <summary>
        /// Tells the user an action failed (for example a save or a print), records it in the error log, and offers
        /// "Copy error details" so the problem can be reported.
        /// </summary>
        public static void ShowFailure(IWin32Window? owner, string title, string whatFailed, Exception ex, [CallerMemberName] string member = "", [CallerFilePath] string file = "")
        {
            string where = $"{Path.GetFileNameWithoutExtension(file)}.{member}";
            Error(where, ex, whatFailed);
            string details = $"{title}{Environment.NewLine}{whatFailed}{Environment.NewLine}When: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}Where: {where}{Environment.NewLine}{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
            try
            {
                AppDialog.Show(owner, title, DialogTone.Error, whatFailed,
                    ex.Message + Environment.NewLine + Environment.NewLine + "Nothing else was changed. The details were saved in the error log.",
                    note: "If it happens again, click \"Copy error details\" and send them with a short note of what you were doing.", details: details);
            }
            catch
            {
                MessageBox.Show(whatFailed + Environment.NewLine + Environment.NewLine + ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
