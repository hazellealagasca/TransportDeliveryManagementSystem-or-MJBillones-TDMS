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
            lblDbStatus.Text = $"Database Path: {MJdatabase.DbFilePath}\nStatus: Online & Connected (SQLite Embedded)";
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
                        MessageBox.Show("Database backup created successfully!", "Backup Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to create database backup.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show("System database restored successfully! Please restart the application.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to restore database from selected file.", "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore operation error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
