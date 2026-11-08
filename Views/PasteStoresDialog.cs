using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Paste many stores at once (Code | Name, tab-separated, as copied from Excel). The grid only shows what
    /// <see cref="StorePasteModel"/> decided; the stores are added as the client's stores, not placed on any route.
    /// Every message is shown inside the dialog.
    /// </summary>
    public sealed class PasteStoresDialog : Form
    {
        private readonly string client;
        private readonly TextBox txtPaste;
        private readonly DataGridView grid;
        private readonly Label lblMsg;
        private readonly Button btnAdd, btnClose;
        private List<PasteRow> rows = new List<PasteRow>();

        /// <summary>How many stores this dialog has added so far (the panel reloads its lists when this is above 0).</summary>
        public int TotalAdded { get; private set; }

        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        public PasteStoresDialog(string clientName)
        {
            client = clientName;
            Text = "Paste stores";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
            BackColor = AppTheme.Surface;
            ClientSize = new Size(D(LargePrint.Px(640, 560)), D(LargePrint.Px(520, 460)));
            MinimumSize = Size;

            var lblHelp = new Label { Dock = DockStyle.Top, Height = D(LargePrint.Px(44, 38)), Font = FontCache.Get("Segoe UI", LargePrint.Pt(10F, 8.75F)), ForeColor = AppTheme.TextSecondary, UseMnemonic = false, Padding = new Padding(D(8), D(4), D(8), 0), Text = $"Paste two columns from Excel (store code, then store name) for {client}. These are added to the client's stores only; routes are not changed." };
            txtPaste = new TextBox { Dock = DockStyle.Top, Multiline = true, AcceptsTab = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, Height = D(LargePrint.Px(90, 76)), Font = FontCache.Get("Segoe UI", LargePrint.Pt(10F, 9F)), PlaceholderText = "Paste here (Ctrl+V): Code, Tab, Name, one store per line" };
            txtPaste.TextChanged += (s, e) => Recheck();

            grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.None, BackgroundColor = AppTheme.Surface, GridColor = AppTheme.BorderStrong, EnableHeadersVisualStyles = false };
            grid.RowTemplate.Height = D(LargePrint.Px(34, 30));
            grid.DefaultCellStyle.Font = FontCache.Get("Segoe UI", LargePrint.Pt(10F, 9F));
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.GridHeaderBack;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.GridHeaderText;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = D(LargePrint.Px(32, 28));
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Code", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", FillWeight = 40 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 60 });
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= rows.Count || grid.Columns[e.ColumnIndex].Name != "colStatus") return;
                bool ok = rows[e.RowIndex].Status == PasteStatus.Ok;
                e.CellStyle.BackColor = ok ? AppTheme.SuccessSoft : AppTheme.DangerSoft;
                e.CellStyle.ForeColor = ok ? AppTheme.SuccessText : AppTheme.DangerText;
            };

            lblMsg = new Label { Dock = DockStyle.Bottom, Height = D(LargePrint.Px(58, 50)), Font = FontCache.Get("Segoe UI", LargePrint.Pt(10F, 8.75F)), ForeColor = AppTheme.TextSecondary, UseMnemonic = false, Padding = new Padding(D(8), D(4), D(8), 0) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(LargePrint.Px(50, 44)), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(8), D(6), D(8), D(6)), BackColor = AppTheme.SurfaceAlt };
            btnClose = new Button { Text = "Close", AutoSize = true, MinimumSize = new Size(D(90), D(LargePrint.Px(36, 30))), UseMnemonic = false, DialogResult = DialogResult.Cancel };
            UIThemeHelper.SetButtonRole(btnClose, "secondary");
            btnAdd = new Button { AutoSize = true, MinimumSize = new Size(D(180), D(LargePrint.Px(36, 30))), UseMnemonic = false };
            UIThemeHelper.SetButtonRole(btnAdd, "primary");
            btnAdd.Click += (s, e) => AddValid();
            bar.Controls.Add(btnClose); bar.Controls.Add(btnAdd);
            CancelButton = btnClose;

            Controls.Add(grid);
            Controls.Add(lblMsg);
            Controls.Add(bar);
            Controls.Add(txtPaste);
            Controls.Add(lblHelp);

            Shown += (s, e) => PrefillFromClipboard();
            Recheck();
        }

        /// <summary>If Excel text is already on the clipboard, start with it. A failure here only means starting empty.</summary>
        private void PrefillFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText().Contains('\t')) txtPaste.Text = Clipboard.GetText();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); }
            txtPaste.Focus();
        }

        private void Recheck()
        {
            try
            {
                rows = StorePasteModel.Classify(client, StorePasteModel.ParseRows(txtPaste.Text));
                Render();
                int ok = rows.Count(r => r.Status == PasteStatus.Ok), skip = rows.Count - ok;
                Say(rows.Count == 0 ? "Nothing pasted yet." : skip == 0 ? $"{ok} stores ready to add." : $"{ok} ready; {skip} row{(skip == 1 ? "" : "s")} will be skipped (see Status).", false);
            }
            catch (Exception ex)
            {
                ErrLog.Error("PasteStoresDialog.Recheck", ex);
                rows = new List<PasteRow>(); grid.Rows.Clear();
                btnAdd.Text = "Add 0 valid stores"; btnAdd.Enabled = false;
                Say("✕ Couldn't read the pasted list: " + ex.Message, true);
            }
        }

        private void Render()
        {
            grid.Rows.Clear();
            foreach (var r in rows) grid.Rows.Add(r.Code, r.Name, r.Message);
            int ok = rows.Count(r => r.Status == PasteStatus.Ok);
            btnAdd.Text = $"Add {ok} valid store{(ok == 1 ? "" : "s")}";
            btnAdd.Enabled = ok > 0;
        }

        private void AddValid()
        {
            try
            {
                var res = StorePasteModel.AddValid(client, rows);
                TotalAdded += res.Added;
                var failed = res.Problems.Count;
                // keep only the rows that were skipped, so what is left on screen is exactly what needs attention
                var skippedText = rows.Where(r => r.Status != PasteStatus.Ok).Select(r => r.Code + "\t" + r.Name).ToList();
                txtPaste.Text = string.Join("\r\n", skippedText);
                string msg = $"Added {res.Added} store{(res.Added == 1 ? "" : "s")}. Skipped {res.Skipped} row{(res.Skipped == 1 ? "" : "s")}" + (res.Skipped > 0 ? " (still listed below)." : ".");
                if (failed > 0) msg += " Not added: " + string.Join(" ", res.Problems);
                Say((failed > 0 ? "✕ " : "✔ ") + msg, failed > 0);
            }
            catch (Exception ex)
            {
                ErrLog.Error("PasteStoresDialog.AddValid", ex);
                Say("✕ Couldn't add the stores: " + ex.Message, true);
            }
        }

        private void Say(string text, bool bad)
        {
            lblMsg.Text = text;
            lblMsg.ForeColor = bad ? AppTheme.DangerText : AppTheme.TextSecondary;
        }
    }
}
