using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>Nov 17: "Automatic backups folder" row: shows where the automatic backups go, with Change folder… and Use default (admins).</summary>
    public partial class BackupRestoreView
    {
        private Label? lblAutoFolder;
        private Button? btnUseDefaultFolder;

        private void AddAutoFolderRow()
        {
            var box = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 14, 0, 0), BackColor = Color.Transparent };
            var caption = new Label
            {
                Dock = DockStyle.Top, AutoSize = true, Text = "Automatic backups folder", UseMnemonic = false,
                Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextPrimary, BackColor = Color.Transparent
            };
            lblAutoFolder = new Label
            {
                Dock = DockStyle.Top, AutoSize = true, UseMnemonic = false, Padding = new Padding(0, 4, 0, 6),
                Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, BackColor = Color.Transparent
            };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, BackColor = Color.Transparent, Margin = Padding.Empty };
            Button B(string text, string role, EventHandler click)
            {
                var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(0, 32), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, UseMnemonic = false,
                    Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8, 0, 8, 0), Font = new Font("Segoe UI Semibold", 9F) };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                b.Click += click;
                return b;
            }
            var btnChange = B("📁  Change folder…", "secondary", (s, e) => ChooseAutoFolder());
            btnUseDefaultFolder = B("Use default folder", "secondary", (s, e) => UseDefaultAutoFolder());
            buttons.Controls.Add(btnChange);
            buttons.Controls.Add(btnUseDefaultFolder);
            buttons.Visible = SessionContext.IsAdmin;   // only an administrator changes where backups go
            // Docked Top: the last one added sits highest, so add bottom-up.
            box.Controls.Add(buttons);
            box.Controls.Add(lblAutoFolder);
            box.Controls.Add(caption);
            pnlStatus.Controls.Add(box);
            box.BringToFront();
            void Wrap() { int w = pnlStatus.ClientSize.Width - pnlStatus.Padding.Horizontal; if (w > 50) lblAutoFolder.MaximumSize = new Size(w, 0); }
            pnlStatus.Resize += (s, e) => Wrap();
            Wrap();
            RefreshAutoFolderText();
        }

        private void RefreshAutoFolderText()
        {
            if (lblAutoFolder == null) return;
            string now = MJdatabase.AutoBackupFolder;
            string problem = MJdatabase.LastAutoBackupFolderProblem;
            if (MJdatabase.AutoBackupFolderIsDefault)
            {
                lblAutoFolder.Text = now + "  (default)\nThe copy made when the app closes (latest 20 kept) and the timed AutoSave snapshots are saved here.";
                lblAutoFolder.ForeColor = AppTheme.TextMuted;
            }
            else if (problem.Length > 0)
            {
                lblAutoFolder.Text = $"Chosen: {MJdatabase.ChosenAutoBackupFolder}\n⚠ It can't be used right now: {problem}\nUntil it is available again, automatic backups go to the default folder: {now}";
                lblAutoFolder.ForeColor = AppTheme.WarningText;
            }
            else
            {
                lblAutoFolder.Text = now + "\nThe copy made when the app closes (latest 20 kept) and the timed AutoSave snapshots are saved here.";
                lblAutoFolder.ForeColor = AppTheme.TextMuted;
            }
            if (btnUseDefaultFolder != null) btnUseDefaultFolder.Enabled = !MJdatabase.AutoBackupFolderIsDefault;
        }

        private void RefreshStatusText()
        {
            var last = MJdatabase.LastManualBackup();
            lblDbStatus.Text = $"Data folder: {AppPaths.DataRoot}\nDatabase: {MJdatabase.DbFilePath}\n" +
                $"Last manual backup: {(last.HasValue ? Fmt.DateTimeText(last.Value) : "never")} · Automatic copies: {MJdatabase.AutoBackupFolder}";
            RefreshAutoFolderText();
        }

        private void ChooseAutoFolder()
        {
            try
            {
                using var dlg = new FolderBrowserDialog
                {
                    Description = "Choose the folder for the automatic backups (for example another drive)",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true,
                    InitialDirectory = Directory.Exists(MJdatabase.AutoBackupFolder) ? MJdatabase.AutoBackupFolder : AppPaths.DataRoot
                };
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(dlg.SelectedPath)) return;
                string chosen = dlg.SelectedPath;
                string? why = MJdatabase.CheckAutoBackupFolder(chosen);
                if (why != null)
                {
                    MessageBox.Show(FindForm(), why, "Automatic backups folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.Equals(Path.GetFullPath(chosen).TrimEnd('\\'), Path.GetFullPath(AppPaths.Backups).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    UseDefaultAutoFolder(ask: false);
                    return;
                }
                if (MessageBox.Show(FindForm(),
                        $"Save the automatic backups in this folder from now on?\n\n{chosen}\n\nBackups already made stay where they are. A test backup is made there now.",
                        "Automatic backups folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                MJdatabase.SetAutoBackupFolder(chosen);
                string made = MJdatabase.PerformAutoBackup();
                RefreshStatusText();
                if (made.Length > 0 && made.StartsWith(Path.GetFullPath(chosen), StringComparison.OrdinalIgnoreCase))
                    MessageBox.Show(FindForm(), $"Done. Automatic backups now go to:\n\n{chosen}\n\nTest backup: {Path.GetFileName(made)}", "Automatic backups folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(FindForm(), "The folder was saved, but the test backup couldn't be made there. Automatic backups will use the default folder until it works.\n\n" + MJdatabase.LastAutoBackupFolderProblem,
                        "Automatic backups folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex) { AppLog.ShowFailure(FindForm(), "Automatic backups folder", "The folder could not be changed.", ex); }
        }

        private void UseDefaultAutoFolder(bool ask = true)
        {
            try
            {
                if (ask && MessageBox.Show(FindForm(), $"Save the automatic backups in the default folder again?\n\n{AppPaths.Backups}\n\nBackups already made in the other folder stay there.",
                        "Automatic backups folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                MJdatabase.SetAutoBackupFolder("");
                RefreshStatusText();
            }
            catch (Exception ex) { AppLog.ShowFailure(FindForm(), "Automatic backups folder", "The folder could not be changed.", ex); }
        }
    }
}
