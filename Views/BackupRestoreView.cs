using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class BackupRestoreView : UserControl
    {
        public BackupRestoreView()
        {
            InitializeComponent();
            Pal.RemapTree(this);
            AddFolderLinks();
            lblTitle.UseMnemonic = false;
            lblBDesc.Text = "Save a copy of all records (orders, dispatches, trucks, crew, billing and receipts) as a .db file, plus a _files.zip with every photo and document.";
            lblRDesc.Text = "Bring back records from a saved backup. Photos and documents saved with it are put back too. This replaces the current records; a safety copy is kept first.";
            foreach (var (lbl, pnl) in new[] { (lblBDesc, pnlBackup), (lblRDesc, pnlRestore) })
            {
                var l = lbl; var p = pnl;
                void Wrap() { int w = p.ClientSize.Width - p.Padding.Horizontal; if (w > 50) l.MaximumSize = new System.Drawing.Size(w, 0); }
                p.Resize += (s, e) => Wrap();
                Wrap();
            }
            var last = MJdatabase.LastManualBackup();
            lblDbStatus.Text = $"Data folder: {AppPaths.DataRoot}\nDatabase: {MJdatabase.DbFilePath}\n" +
                $"Last manual backup: {(last.HasValue ? last.Value.ToString("MMM d, yyyy h:mm tt") : "never")} · Automatic copies: {AppPaths.Backups}";
        }
        private void AddFolderLinks()
        {
            var links = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 10, 0, 0), BackColor = System.Drawing.Color.Transparent };
            LinkLabel L(string text, Func<string> path)
            {
                var l = new LinkLabel { Text = text, AutoSize = true, Margin = new Padding(0, 0, 18, 0), LinkBehavior = LinkBehavior.HoverUnderline,
                    LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, VisitedLinkColor = AppTheme.AccentText, BackColor = System.Drawing.Color.Transparent };
                l.LinkClicked += (s, e) =>
                {
                    try
                    {
                        string p = path();
                        if (File.Exists(p)) System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{p}\"");
                        else { Directory.CreateDirectory(p); System.Diagnostics.Process.Start("explorer.exe", $"\"{p}\""); }
                    }
                    catch (Exception ex) { AppLog.ShowFailure(FindForm(), "Open folder", "The folder could not be opened.", ex); }
                };
                return l;
            }
            var where = new LinkLabel { Text = "Where is my data?", AutoSize = true, Margin = new Padding(0, 0, 18, 0), LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = AppTheme.AccentText, ActiveLinkColor = AppTheme.AccentHover, VisitedLinkColor = AppTheme.AccentText, BackColor = System.Drawing.Color.Transparent };
            where.LinkClicked += (s, e) => AppDialog.Show(FindForm(), "Data folder", DialogTone.Info, "Where your data is kept",
                "The database, photos, documents, automatic backups and the error log are all in this folder. Rebuilding, updating or reinstalling the app won't touch it.",
                path: AppPaths.DataRoot, note: "Use Create Backup Snapshot at least once a week and keep the copy (the .db file and its _files.zip) on a USB drive or cloud storage.", okText: "Got it");
            links.Controls.Add(where);
            links.Controls.Add(L("Open data folder", () => AppPaths.DataRoot));
            links.Controls.Add(L("Open automatic backups", () => AppPaths.Backups));
            links.Controls.Add(L("Open error log", () => File.Exists(AppLog.ErrorLogFile) ? AppLog.ErrorLogFile : AppPaths.Logs));
            pnlStatus.Controls.Add(links);
            links.BringToFront();
        }

        private void BtnBackup_Click(object? sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "Database Backup (*.db)|*.db",
                    FileName = $"MJ_Billones_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    if (!SecurityHelper.IsValidFilePath(sfd.FileName, ".db"))
                    {
                        MessageBox.Show("Security Violation: Invalid file path or extension detected.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (MJdatabase.BackupDatabase(sfd.FileName))
                    {
                        Cursor = Cursors.WaitCursor;
                        int files;
                        string fileError;
                        try { files = MJdatabase.BackupAttachedFiles(sfd.FileName, out fileError); }
                        finally { Cursor = Cursors.Default; }
                        MJdatabase.MarkManualBackup();
                        string zip = System.IO.Path.GetFileName(MJdatabase.FilesArchiveFor(sfd.FileName));
                        string extra = files > 0 ? $"\n\n{files:N0} photo(s) and document(s) were saved with it in \"{zip}\". Keep both files together (for example on a USB drive)."
                                     : files == 0 ? "\n\nThere are no photos or documents to save yet."
                                     : $"\n\nThe data was backed up, but the photos and documents couldn't be saved ({fileError}).";
                        MessageBox.Show("Backup created and checked. It can be restored on this or another copy of the app." + extra, "Backup Success", MessageBoxButtons.OK, files < 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                        var lastNow = MJdatabase.LastManualBackup();
                        lblDbStatus.Text = $"Data folder: {AppPaths.DataRoot}\nDatabase: {MJdatabase.DbFilePath}\nLast manual backup: {lastNow:MMM d, yyyy h:mm tt} · Automatic copies: {AppPaths.Backups}";
                    }
                    else
                    {
                        MessageBox.Show("The backup wasn't created.\n\n" + MJdatabase.LastBackupError, "Backup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.ShowFailure(FindForm(), "Backup", "The backup could not be made.", ex);
            }
        }

        private void BtnRestore_Click(object? sender, EventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show("Are you sure you want to restore the database? Current unsaved records will be replaced.", "Confirm System Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "Database Backup (*.db)|*.db"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (!SecurityHelper.IsValidFilePath(ofd.FileName, ".db"))
                    {
                        MessageBox.Show("Security Violation: Invalid file path or extension detected.", "Security Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (MJdatabase.RestoreDatabase(ofd.FileName))
                    {
                        int files = MJdatabase.RestoreAttachedFiles(ofd.FileName, out string fileError);
                        string extra = files > 0 ? $"\n\n{files:N0} photo(s) and document(s) from the backup were put back."
                                     : files < 0 ? $"\n\nThe photos and documents saved with this backup couldn't be put back ({fileError})." : "";
                        MessageBox.Show("Database restored. A safety copy of the previous data was saved in the Backups folder." + extra + "\n\nPlease restart the application so every screen reloads the restored data.", "Restore Complete", MessageBoxButtons.OK, files < 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The database wasn't restored.\n\n" + MJdatabase.LastRestoreError, "Restore failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.ShowFailure(FindForm(), "Restore", "The backup could not be restored.", ex);
            }
        }
    }
}
