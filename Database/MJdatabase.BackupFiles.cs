using System;
using System.IO;
using System.IO.Compression;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Photos and documents (Attachments and crew photos) are saved next to a database backup as "&lt;name&gt;_files.zip",
    /// and put back when that backup is restored. Restoring only adds missing files; it never deletes or overwrites any.
    /// </summary>
    public static partial class MJdatabase
    {
        public const string LastManualBackupKey = "LastManualBackupAt";
        public static string FilesArchiveFor(string dbBackupPath) => Path.Combine(Path.GetDirectoryName(dbBackupPath) ?? "", Path.GetFileNameWithoutExtension(dbBackupPath) + "_files.zip");
        /// <summary>Zips the Attachments and Uploads folders next to the backup. Returns the number of files, or -1 on failure.</summary>
        public static int BackupAttachedFiles(string dbBackupPath, out string error)
        {
            error = "";
            string zipPath = FilesArchiveFor(dbBackupPath);
            string tmp = zipPath + ".tmp";
            int count = 0;
            try
            {
                if (File.Exists(tmp)) File.Delete(tmp);
                using (var zip = ZipFile.Open(tmp, ZipArchiveMode.Create))
                {
                    foreach (var folder in new[] { AppPaths.Attachments, AppPaths.Uploads })
                    {
                        if (!Directory.Exists(folder)) continue;
                        string top = Path.GetFileName(folder.TrimEnd('\\', '/'));
                        foreach (var file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                        {
                            string entry = (top + "/" + Path.GetRelativePath(folder, file)).Replace('\\', '/');
                            zip.CreateEntryFromFile(file, entry, CompressionLevel.Fastest);
                            count++;
                        }
                    }
                }
                if (count == 0) { File.Delete(tmp); return 0; }
                File.Move(tmp, zipPath, true);
                return count;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                error = ex.Message;
                AppLog.Error("BackupAttachedFiles", ex, zipPath);
                return -1;
            }
        }
        /// <summary>Puts back the photos and documents saved with a backup (only files that are missing). Returns the number added, or -1.</summary>
        public static int RestoreAttachedFiles(string dbBackupPath, out string error)
        {
            error = "";
            string zipPath = FilesArchiveFor(dbBackupPath);
            if (!File.Exists(zipPath)) return 0;
            int added = 0;
            try
            {
                string root = Path.GetFullPath(AppPaths.DataRoot).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                using var zip = ZipFile.OpenRead(zipPath);
                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue;
                    string dest = Path.GetFullPath(Path.Combine(root, e.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // ignore entries that try to leave the folder
                    string top = e.FullName.Split('/')[0];
                    if (!top.Equals("Attachments", StringComparison.OrdinalIgnoreCase) && !top.Equals("Uploads", StringComparison.OrdinalIgnoreCase)) continue;
                    if (File.Exists(dest)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    e.ExtractToFile(dest, false);
                    added++;
                }
                return added;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLog.Error("RestoreAttachedFiles", ex, zipPath);
                return -1;
            }
        }
        /// <summary>When the last manual backup was made, or null if never.</summary>
        public static DateTime? LastManualBackup()
        {
            string v = GetSystemSetting(LastManualBackupKey, "");
            return DateTime.TryParse(v, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
        }
        public static void MarkManualBackup() => SetSystemSetting(LastManualBackupKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), "When the last manual backup was made");
    }
}
