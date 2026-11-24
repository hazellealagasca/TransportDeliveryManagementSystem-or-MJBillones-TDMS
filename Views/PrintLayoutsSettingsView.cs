using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Settings → Print layouts: every printable form, with a button that opens its layout editor (the print window in layout mode):
    /// title, header, labels, columns, signatures, notes. The person printing can only preview and print; what a form shows is set here.
    /// </summary>
    public class PrintLayoutsSettingsView : UserControl, IRefreshable
    {
        private readonly List<(PrintLayoutEntry Entry, Label Status)> rows = new();
        private readonly FlowLayoutPanel page;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public PrintLayoutsSettingsView()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            BackColor = AppTheme.Canvas;
            Font = new Font("Segoe UI", 9.75F);
            page = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Location = new Point(D(24), D(24)), BackColor = Color.Transparent };
            Controls.Add(page);
            if (!SessionContext.IsAdmin)
                page.Controls.Add(new Label { Text = "Only the Owner / Administrator can change these settings.", AutoSize = true, ForeColor = AppTheme.TextMuted, Margin = new Padding(0, 0, 0, D(12)) });
            var entries = PrintLayoutRegistry.All();
            page.Controls.Add(Card("🖨️  Forms & statements", "Receipts, statements of account, trip reports, dispatch and crew forms. Pick a form to change its title, header, labels, details, columns, signature lines and notes. The person printing only previews and prints.", entries.Where(e => e.Group == PrintLayoutRegistry.GroupForms)));
            page.Controls.Add(Card("📋  Lists & registers", "Lists printed from a table (clients, trucks, receipts log…). Their columns follow the list on screen; the rest of the layout is set here.", entries.Where(e => e.Group == PrintLayoutRegistry.GroupLists)));
            void Fit()
            {
                int w = Math.Max(D(480), ClientSize.Width - D(48) - SystemInformation.VerticalScrollBarWidth);
                foreach (Control c in page.Controls) if (c is Panel) { c.MinimumSize = new Size(w, 0); c.MaximumSize = new Size(w, 0); }
                page.Location = new Point(D(24) + AutoScrollPosition.X, D(24) + AutoScrollPosition.Y);
            }
            Resize += (s, e) => Fit();
            HandleCreated += (s, e) => Fit();
            UIThemeHelper.ApplyTheme(this);
            BackColor = AppTheme.Canvas;
            RefreshData();
        }
        public void RefreshData()
        {
            foreach (var (entry, status) in rows)
            {
                bool custom = PrintStore.HasSavedLayout(entry.Key);
                status.Text = custom ? "Customised" : "Standard";
                status.ForeColor = custom ? AppTheme.AccentText : AppTheme.TextMuted;
            }
        }
        private Control Card(string title, string subtitle, IEnumerable<PrintLayoutEntry> items)
        {
            var card = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = AppTheme.Surface, Padding = new Padding(D(22), D(18), D(22), D(10)), Margin = new Padding(0, 0, 0, D(16)) };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(AppTheme.Border);
                using var gp = Gfx.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), D(12));
                e.Graphics.DrawPath(pen, gp);
            };
            card.Resize += (s, e) => card.Invalidate();
            var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            int r = 0;
            var head = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 12.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0), UseMnemonic = false };
            grid.Controls.Add(head, 0, r); grid.SetColumnSpan(head, 3); r++;
            var sub = new Label { Text = subtitle, AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), Margin = new Padding(0, D(4), 0, D(8)), UseMnemonic = false };
            grid.Controls.Add(sub, 0, r); grid.SetColumnSpan(sub, 3); r++;
            var list = items.ToList();
            foreach (var entry in list)
            {
                var name = new Label { Text = entry.Name, AutoSize = true, Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, D(13), D(16), D(8)), UseMnemonic = false, Anchor = AnchorStyles.Left };
                var status = new Label { AutoSize = true, Font = new Font("Segoe UI Semibold", 8.75F), Margin = new Padding(0, D(15), D(14), 0), Anchor = AnchorStyles.Right, UseMnemonic = false };
                var edit = new Button { Text = "Edit layout…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(D(120), D(34)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, D(6), 0, D(6)), Enabled = SessionContext.IsAdmin, UseMnemonic = false, Tag = entry.Key };
                UIThemeHelper.SetButtonRole(edit, "secondary");
                var e0 = entry;
                edit.Click += (s, ev) => Edit(e0);
                grid.Controls.Add(name, 0, r); grid.Controls.Add(status, 1, r); grid.Controls.Add(edit, 2, r); r++;
                rows.Add((entry, status));
                if (entry != list[^1])
                {
                    var line = new Panel { Height = 1, AutoSize = false, Anchor = AnchorStyles.Left | AnchorStyles.Right, BackColor = AppTheme.Border, Margin = new Padding(0) };
                    grid.Controls.Add(line, 0, r); grid.SetColumnSpan(line, 3); r++;
                }
            }
            void Wrap() { sub.MaximumSize = new Size(Math.Max(D(240), grid.ClientSize.Width), 0); }
            grid.Resize += (s, e) => Wrap();
            card.Controls.Add(grid);
            return card;
        }
        private void Edit(PrintLayoutEntry entry)
        {
            PrintFormDef def;
            try { def = entry.Build(); }
            catch (Exception ex)
            {
                ErrLog.Error("PrintLayouts", ex);
                MessageBox.Show(FindForm(), $"Couldn't prepare the layout of “{entry.Name}”: {ex.Message}", "Print layouts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PrintStudio.ShowLayout(def, FindForm());
            RefreshData();
        }
    }
}
