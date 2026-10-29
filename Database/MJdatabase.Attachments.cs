using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database
{
    /// <summary>
    /// Files attached to records (proof of delivery, receipts, trip sheets, photos). The file is copied into the
    /// app's Attachments folder and the database keeps its path, size and SHA-256 hash. Attachments are never deleted,
    /// only voided, and the same file can't be attached twice to the same record.
    /// </summary>
    public static partial class MJdatabase
    {
        public const long MaxAttachmentBytes = 15L * 1024 * 1024;
        public static readonly string[] AttachmentExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt" };
        public static readonly string[] AttachmentCategories = { "Proof of delivery (POD)", "Receipt", "Trip sheet", "Delivery receipt (DR)", "Photo", "Registration / OR-CR", "Insurance", "Other" };
        private static bool attachmentsReady;
        public static string AttachmentsRoot => AppPaths.Attachments;
        private static void EnsureAttachmentsTable()
        {
            if (attachmentsReady) return;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Attachments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OwnerType TEXT NOT NULL,
                    OwnerId INTEGER NOT NULL,
                    OwnerRef TEXT DEFAULT '',
                    Category TEXT DEFAULT 'Other',
                    FileName TEXT NOT NULL,
                    StoredPath TEXT NOT NULL,
                    ContentType TEXT DEFAULT '',
                    SizeBytes INTEGER DEFAULT 0,
                    Sha256 TEXT NOT NULL,
                    Caption TEXT DEFAULT '',
                    AddedBy TEXT DEFAULT '',
                    AddedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    IsVoid INTEGER DEFAULT 0,
                    VoidedBy TEXT DEFAULT '',
                    VoidedAt TEXT DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS idx_attachments_owner ON Attachments(OwnerType, OwnerId, IsVoid);
                CREATE UNIQUE INDEX IF NOT EXISTS ux_attachments_file_per_owner ON Attachments(OwnerType, OwnerId, Sha256) WHERE IsVoid = 0;
                CREATE TRIGGER IF NOT EXISTS trg_nodelete_Attachments BEFORE DELETE ON Attachments BEGIN SELECT RAISE(ABORT, 'Attachments are kept for history. Void them instead.'); END;";
            cmd.ExecuteNonQuery();
            attachmentsReady = true;
        }
        public static List<Attachment> GetAttachments(string ownerType, int ownerId)
        {
            EnsureAttachmentsTable();
            var list = new List<Attachment>();
            if (ownerId <= 0) return list;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, OwnerType, OwnerId, OwnerRef, Category, FileName, StoredPath, ContentType, SizeBytes, Sha256, Caption, AddedBy, AddedAt FROM Attachments WHERE OwnerType = @t AND OwnerId = @o AND IsVoid = 0 ORDER BY AddedAt DESC, Id DESC";
            cmd.Parameters.AddWithValue("@t", ownerType);
            cmd.Parameters.AddWithValue("@o", ownerId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Attachment
                {
                    Id = r.GetInt32(0), OwnerType = r.GetString(1), OwnerId = r.GetInt32(2), OwnerRef = r.IsDBNull(3) ? "" : r.GetString(3),
                    Category = r.IsDBNull(4) ? "" : r.GetString(4), FileName = r.GetString(5), StoredPath = AppPaths.Resolve(r.GetString(6)), ContentType = r.IsDBNull(7) ? "" : r.GetString(7),
                    SizeBytes = r.IsDBNull(8) ? 0 : r.GetInt64(8), Sha256 = r.GetString(9), Caption = r.IsDBNull(10) ? "" : r.GetString(10),
                    AddedBy = r.IsDBNull(11) ? "" : r.GetString(11), AddedAt = DateTime.TryParse(r.IsDBNull(12) ? "" : r.GetString(12), out var at) ? at : DateTime.Now
                });
            }
            return list;
        }
        /// <summary>Attachment counts per owner id (for "has POD" filters and badges).</summary>
        public static Dictionary<int, (int Total, int Pods)> GetAttachmentCounts(string ownerType)
        {
            var map = new Dictionary<int, (int, int)>();
            try
            {
                EnsureAttachmentsTable();
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT OwnerId, COUNT(*), SUM(CASE WHEN Category LIKE 'Proof of delivery%' OR Category LIKE 'Delivery receipt%' THEN 1 ELSE 0 END) FROM Attachments WHERE OwnerType = @t AND IsVoid = 0 GROUP BY OwnerId";
                cmd.Parameters.AddWithValue("@t", ownerType);
                using var r = cmd.ExecuteReader();
                while (r.Read()) map[r.GetInt32(0)] = (r.GetInt32(1), r.IsDBNull(2) ? 0 : r.GetInt32(2));
            }
            catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"[DB] GetAttachmentCounts: {ex.Message}"); ErrLog.Swallowed(ex); }
            return map;
        }
        /// <summary>Checks a file before it is attached. Returns a reason when it can't be accepted.</summary>
        public static string? ValidateAttachmentFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return "The file no longer exists.";
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!AttachmentExtensions.Contains(ext)) return $"\"{Path.GetFileName(path)}\" is a {ext} file. Allowed: images (JPG, PNG, BMP, GIF, TIFF), PDF, Word, Excel, CSV and text.";
            var info = new FileInfo(path);
            if (info.Length == 0) return $"\"{info.Name}\" is empty.";
            if (info.Length > MaxAttachmentBytes) return $"\"{info.Name}\" is {info.Length / 1024.0 / 1024.0:0.#} MB. The limit is {MaxAttachmentBytes / 1024 / 1024} MB per file.";
            return null;
        }
        private static string Sha256Of(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return Convert.ToHexString(sha.ComputeHash(fs));
        }
        private static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Replace("..", "_").Trim();
            if (name.Length > 80) name = Path.GetFileNameWithoutExtension(name).Substring(0, 70) + Path.GetExtension(name);
            return name.Length == 0 ? "file" : name;
        }
        /// <summary>Copies the file into the attachments folder and records it. Throws with a readable message when refused.</summary>
        public static Attachment AddAttachment(string ownerType, int ownerId, string ownerRef, string sourcePath, string category, string caption = "")
        {
            EnsureAttachmentsTable();
            if (ownerId <= 0) throw new InvalidOperationException("Save the record first, then attach files to it.");
            string? problem = ValidateAttachmentFile(sourcePath);
            if (problem != null) throw new InvalidOperationException(problem);
            string hash = Sha256Of(sourcePath);
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var dup = conn.CreateCommand();
                dup.CommandText = "SELECT FileName FROM Attachments WHERE OwnerType = @t AND OwnerId = @o AND Sha256 = @h AND IsVoid = 0 LIMIT 1";
                dup.Parameters.AddWithValue("@t", ownerType); dup.Parameters.AddWithValue("@o", ownerId); dup.Parameters.AddWithValue("@h", hash);
                var existing = dup.ExecuteScalar()?.ToString();
                if (existing != null) throw new InvalidOperationException($"This file is already attached here (as \"{existing}\").");
            }
            string folder = Path.Combine(AttachmentsRoot, SafeFileName(ownerType), ownerId.ToString());
            Directory.CreateDirectory(folder);
            string fileName = SafeFileName(Path.GetFileName(sourcePath));
            string stored = Path.Combine(folder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{hash.Substring(0, 8)}_{fileName}");
            File.Copy(sourcePath, stored, overwrite: false);
            var a = new Attachment
            {
                OwnerType = ownerType, OwnerId = ownerId, OwnerRef = ownerRef ?? "", Category = string.IsNullOrWhiteSpace(category) ? "Other" : category,
                FileName = fileName, StoredPath = stored, ContentType = Path.GetExtension(fileName).ToLowerInvariant(), SizeBytes = new FileInfo(stored).Length,
                Sha256 = hash, Caption = caption ?? "", AddedBy = SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername, AddedAt = DateTime.Now
            };
            try
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO Attachments (OwnerType, OwnerId, OwnerRef, Category, FileName, StoredPath, ContentType, SizeBytes, Sha256, Caption, AddedBy, AddedAt)
                                    VALUES (@t, @o, @ref, @cat, @fn, @p, @ct, @sz, @h, @cap, @by, @at); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@t", a.OwnerType); cmd.Parameters.AddWithValue("@o", a.OwnerId); cmd.Parameters.AddWithValue("@ref", a.OwnerRef);
                cmd.Parameters.AddWithValue("@cat", a.Category); cmd.Parameters.AddWithValue("@fn", a.FileName); cmd.Parameters.AddWithValue("@p", a.StoredPath);
                cmd.Parameters.AddWithValue("@ct", a.ContentType); cmd.Parameters.AddWithValue("@sz", a.SizeBytes); cmd.Parameters.AddWithValue("@h", a.Sha256);
                cmd.Parameters.AddWithValue("@cap", a.Caption); cmd.Parameters.AddWithValue("@by", a.AddedBy); cmd.Parameters.AddWithValue("@at", a.AddedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                a.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch
            {
                try { File.Delete(stored); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                throw;
            }
            LogAuditTrail("ADD", "Attachments", a.Id, $"{ownerType} {ownerRef}".Trim(), "File", "", a.FileName, $"Attached {a.Category}: {a.FileName} ({a.SizeBytes / 1024} KB)");
            return a;
        }
        public static void SetAttachmentCategory(int id, string category)
        {
            EnsureAttachmentsTable();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Attachments SET Category = @c WHERE Id = @id";
            cmd.Parameters.AddWithValue("@c", category);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogAuditTrail("UPDATE", "Attachments", id, $"Attachment #{id}", "Category", "", category, "Attachment re-categorised");
        }
        /// <summary>Soft delete: the row and the stored file stay; the attachment just stops showing.</summary>
        public static void VoidAttachment(int id)
        {
            EnsureAttachmentsTable();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Attachments SET IsVoid = 1, VoidedBy = @by, VoidedAt = @at WHERE Id = @id";
            cmd.Parameters.AddWithValue("@by", SessionContext.ActiveUser?.Username ?? SessionContext.CurrentUsername);
            cmd.Parameters.AddWithValue("@at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            LogAuditTrail("VOID", "Attachments", id, $"Attachment #{id}", "", "", "", "Attachment voided (file kept)");
        }
    }
}
