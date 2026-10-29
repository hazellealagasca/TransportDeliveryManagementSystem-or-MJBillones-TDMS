using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Where the app keeps its data. Everything that must survive a rebuild or a reinstall (the database, attachments,
    /// crew photos, backups and logs) lives in one data folder, by default C:\ProgramData\MJ Billones Transport\TDMS,
    /// not in the program's own folder (bin\Debug or the install folder), which Visual Studio and installers may wipe.
    /// A file named data_folder.txt next to the program can point to another folder (for example a USB drive).
    /// </summary>
    public static class AppPaths
    {
        public const string DatabaseFileName = "mj_billones.db";
        public static string ProgramFolder => AppDomain.CurrentDomain.BaseDirectory;
        private static string? dataRoot;
        public static string DataRoot => dataRoot ??= ChooseDataRoot();
        public static string DatabaseFile => Path.Combine(DataRoot, DatabaseFileName);
        public static string Attachments => Path.Combine(DataRoot, "Attachments");
        public static string Uploads => Path.Combine(DataRoot, "Uploads");
        public static string Backups => Path.Combine(DataRoot, "Backups");
        public static string Logs => Path.Combine(DataRoot, "Logs");
        /// <summary>Set when this start-up copied the data out of the program folder (shown once to the user).</summary>
        public static string MigrationMessage { get; private set; } = "";
        private static string ChooseDataRoot()
        {
            // 1) An explicit choice next to the program.
            try
            {
                string pointer = Path.Combine(ProgramFolder, "data_folder.txt");
                if (File.Exists(pointer))
                {
                    string p = File.ReadAllLines(pointer).Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0 && !l.StartsWith("#")) ?? "";
                    p = Environment.ExpandEnvironmentVariables(p);
                    if (p.Length > 0 && TryUse(p)) return Path.GetFullPath(p);
                }
            }
            catch { }
            // 2) The shared data folder for every Windows account on this computer.
            try
            {
                string shared = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MJ Billones Transport", "TDMS");
                if (TryUse(shared)) { try { GrantUsersModify(shared); } catch { } return shared; }
            }
            catch { }
            // 3) This Windows account only.
            try
            {
                string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MJ Billones Transport", "TDMS");
                if (TryUse(local)) return local;
            }
            catch { }
            // 4) Last resort: the program folder (the old behaviour).
            return ProgramFolder;
        }
        private static bool TryUse(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                string probe = Path.Combine(folder, ".write_test_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }
        /// <summary>Lets every Windows account on the computer read and write the shared data folder.</summary>
        private static void GrantUsersModify(string folder)
        {
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                var di = new DirectoryInfo(folder);
                var sec = di.GetAccessControl();
                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                sec.AddAccessRule(new FileSystemAccessRule(users, FileSystemRights.Modify,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                di.SetAccessControl(sec);
            }
            catch { /* not fatal: the account that created the folder can always use it */ }
        }
        /// <summary>
        /// Run once at start-up, before the database is opened: creates the data folders and, the first time, copies the
        /// database, attachments, crew photos and backups out of the program folder. Nothing in the old folder is deleted.
        /// </summary>
        public static void Prepare()
        {
            try
            {
                foreach (var d in new[] { DataRoot, Attachments, Uploads, Backups, Logs }) Directory.CreateDirectory(d);
            }
            catch { }
            if (SamePath(DataRoot, ProgramFolder)) return;
            string oldDb = Path.Combine(ProgramFolder, DatabaseFileName);
            string newDb = DatabaseFile;
            if (File.Exists(newDb) || !File.Exists(oldDb)) return;
            try
            {
                // Copy with SQLite's own backup, so recent changes still in the -wal file come along.
                string tmp = newDb + ".copying";
                if (File.Exists(tmp)) File.Delete(tmp);
                using (var src = new SqliteConnection($"Data Source={oldDb};Pooling=False;"))
                using (var dst = new SqliteConnection($"Data Source={tmp};Pooling=False;"))
                {
                    src.Open(); dst.Open();
                    src.BackupDatabase(dst);
                }
                File.Move(tmp, newDb);
                foreach (var name in new[] { "Attachments", "Uploads", "Backups" })
                    CopyFolder(Path.Combine(ProgramFolder, name), Path.Combine(DataRoot, name));
                RewriteStoredPaths(newDb, ProgramFolder, DataRoot);
                try
                {
                    File.WriteAllText(Path.Combine(ProgramFolder, "DATA_MOVED_README.txt"),
                        $"On {DateTime.Now:MMMM d, yyyy h:mm tt} the system's data was copied to:{Environment.NewLine}{DataRoot}{Environment.NewLine}{Environment.NewLine}" +
                        "The app now reads and saves only there. The old database and files in this folder were left untouched as an extra copy; " +
                        "they are no longer updated.");
                }
                catch { }
                MigrationMessage = $"Your data is now kept in a safe folder that rebuilding or reinstalling the app won't touch:\n\n{DataRoot}\n\n" +
                                   "The old copy in the program folder was left as it was, as an extra backup.";
                AppLog.Info("AppPaths", "Data copied from " + ProgramFolder + " to " + DataRoot);
            }
            catch (Exception ex)
            {
                // Could not copy: keep using the old database so nothing is lost or split.
                try { if (File.Exists(newDb + ".copying")) File.Delete(newDb + ".copying"); } catch { }
                dataRoot = ProgramFolder;
                AppLog.Error("AppPaths.Prepare", ex, "Data could not be copied to the data folder; still using the program folder.");
            }
        }
        private static bool SamePath(string a, string b) =>
            string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        private static void CopyFolder(string from, string to)
        {
            if (!Directory.Exists(from)) return;
            foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, dir)));
            Directory.CreateDirectory(to);
            foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string dest = Path.Combine(to, Path.GetRelativePath(from, file));
                try { if (!File.Exists(dest)) File.Copy(file, dest); } catch (Exception ex) { AppLog.Error("AppPaths.CopyFolder", ex, file); }
            }
        }
        /// <summary>Photos and attachments are saved with full paths; point the ones inside the old folder at the new one.</summary>
        private static void RewriteStoredPaths(string db, string oldRoot, string newRoot)
        {
            string o = oldRoot.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            string n = newRoot.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            using var conn = new SqliteConnection($"Data Source={db};Pooling=False;");
            conn.Open();
            var targets = new System.Collections.Generic.List<(string Table, string Column)>();
            using (var t = conn.CreateCommand())
            {
                t.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
                var tables = new System.Collections.Generic.List<string>();
                using (var r = t.ExecuteReader()) while (r.Read()) tables.Add(r.GetString(0));
                foreach (var table in tables)
                {
                    using var c = conn.CreateCommand();
                    c.CommandText = $"PRAGMA table_info(\"{table.Replace("\"", "\"\"")}\")";
                    using var r = c.ExecuteReader();
                    while (r.Read())
                    {
                        string col = r.GetString(1);
                        if (col is "PhotoPath" or "StoredPath") targets.Add((table, col));
                    }
                }
            }
            foreach (var (table, col) in targets)
            {
                using var u = conn.CreateCommand();
                u.CommandText = $"UPDATE \"{table}\" SET \"{col}\" = @n || substr(\"{col}\", length(@o) + 1) WHERE \"{col}\" LIKE @like ESCAPE '!'";
                u.Parameters.AddWithValue("@o", o);
                u.Parameters.AddWithValue("@n", n);
                u.Parameters.AddWithValue("@like", o.Replace("!", "!!").Replace("%", "!%").Replace("_", "!_") + "%");
                u.ExecuteNonQuery();
            }
        }
        /// <summary>
        /// Finds a saved file: as stored, then (for paths into the old program folder or relative paths) inside the data folder.
        /// Returns the stored value unchanged when nothing better is found.
        /// </summary>
        public static string Resolve(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return stored ?? "";
            try
            {
                if (Path.IsPathRooted(stored))
                {
                    if (File.Exists(stored)) return stored;
                    string o = ProgramFolder.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
                    if (stored.StartsWith(o, StringComparison.OrdinalIgnoreCase))
                    {
                        string moved = Path.Combine(DataRoot, stored.Substring(o.Length));
                        if (File.Exists(moved)) return moved;
                    }
                    // Fall back on the folder names, for paths saved on another computer or an older location.
                    foreach (var marker in new[] { "Attachments", "Uploads" })
                    {
                        int i = stored.IndexOf(Path.DirectorySeparatorChar + marker + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                        if (i >= 0)
                        {
                            string guess = Path.Combine(DataRoot, stored.Substring(i + 1));
                            if (File.Exists(guess)) return guess;
                        }
                    }
                    return stored;
                }
                string inData = Path.Combine(DataRoot, stored);
                if (File.Exists(inData)) return inData;
                string inProgram = Path.Combine(ProgramFolder, stored);
                if (File.Exists(inProgram)) return inProgram;
            }
            catch { }
            return stored;
        }
    }
}
