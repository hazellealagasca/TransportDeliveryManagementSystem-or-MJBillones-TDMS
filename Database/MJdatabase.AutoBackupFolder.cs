using System;
using System.IO;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Nov 17: the folder for the AUTOMATIC backups (the copy made when the app closes and the timed AutoSave snapshots)
    /// can be chosen in Settings → Backup &amp; Restore, e.g. another drive. Blank = the default Backups folder in the data folder.
    /// When the chosen folder can't be used (a USB drive unplugged, no permission) the backup still runs, in the default folder,
    /// and the problem is logged and shown on the Backup &amp; Restore screen. Safety copies made before a restore or a repair
    /// always stay in the data folder's Backups folder.
    /// </summary>
    public static partial class MJdatabase
    {
        public const string AutoBackupFolderKey = "Backup.AutoFolder";

        /// <summary>Why the chosen folder could not be used the last time it was asked for ("" = no problem).</summary>
        public static string LastAutoBackupFolderProblem { get; private set; } = "";

        /// <summary>The folder chosen in Settings ("" = the default).</summary>
        public static string ChosenAutoBackupFolder => GetSystemSetting(AutoBackupFolderKey, "").Trim();

        /// <summary>True when no folder was chosen (the default Backups folder is used).</summary>
        public static bool AutoBackupFolderIsDefault => ChosenAutoBackupFolder.Length == 0;

        /// <summary>The folder the automatic backups go to now: the chosen one, or the default when none is chosen or it can't be used.</summary>
        public static string AutoBackupFolder
        {
            get
            {
                string chosen = ChosenAutoBackupFolder;
                if (chosen.Length == 0) { LastAutoBackupFolderProblem = ""; return AppPaths.Backups; }
                string? why = CheckAutoBackupFolder(chosen);
                if (why == null) { LastAutoBackupFolderProblem = ""; return chosen; }
                LastAutoBackupFolderProblem = why;
                try { AppLog.Info("AutoBackupFolder", $"Chosen folder \"{chosen}\" can't be used ({why}); the default folder {AppPaths.Backups} is used instead."); } catch { }
                return AppPaths.Backups;
            }
        }

        /// <summary>Null when the folder can hold automatic backups (it is created if missing and a test file is written), otherwise the problem in plain words.</summary>
        public static string? CheckAutoBackupFolder(string? path)
        {
            string p = (path ?? "").Trim();
            if (p.Length == 0) return "No folder was chosen.";
            if (p.Length > 200) return "The folder path is too long (at most 200 characters).";
            string full;
            try
            {
                if (!Path.IsPathRooted(p)) return "Choose a full folder path (for example D:\\TDMS Backups).";
                full = Path.GetFullPath(p);
            }
            catch { return "That is not a valid folder path."; }
            // The program folder is emptied when the app is rebuilt or reinstalled.
            if (IsInside(full, AppPaths.ProgramFolder)) return "Backups can't be kept in the program folder: rebuilding or reinstalling the app would delete them.";
            try
            {
                Directory.CreateDirectory(full);
                string test = Path.Combine(full, $".tdms_write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(test, "ok");
                File.Delete(test);
            }
            catch (Exception ex) { return "The app can't save files in that folder (" + ex.Message.TrimEnd('.') + ")."; }
            return null;
        }

        /// <summary>Saves the chosen folder ("" or null = back to the default) and writes it to the audit trail.</summary>
        public static void SetAutoBackupFolder(string? path)
        {
            string old = ChosenAutoBackupFolder;
            string now = string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path.Trim());
            SetSystemSetting(AutoBackupFolderKey, now, "Folder for the automatic backups (blank = Backups in the data folder)");
            LastAutoBackupFolderProblem = "";
            try
            {
                LogAuditTrail("UPDATE", "SystemSettings", 0, "Automatic backups folder", "AutoBackupFolder",
                    old.Length == 0 ? "(default) " + AppPaths.Backups : old, now.Length == 0 ? "(default) " + AppPaths.Backups : now,
                    "Automatic backups folder changed");
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
        }

        private static bool IsInside(string path, string folder)
        {
            try
            {
                string a = Path.GetFullPath(path).TrimEnd('\\') + "\\";
                string b = Path.GetFullPath(folder).TrimEnd('\\') + "\\";
                return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }
}
