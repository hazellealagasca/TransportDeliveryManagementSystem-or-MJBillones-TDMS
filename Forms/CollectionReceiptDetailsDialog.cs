using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    /// <summary>
    /// Nov 25: the window that opens when an issued collection receipt is double-clicked in Payments → Collection Receipts.
    /// Shows everything about the receipt (payment, client, statement, the service invoices it paid) and prints the Official Receipt.
    /// Read-only: plain labels and a read-only table, nothing to type in.
    /// </summary>
    public sealed class CollectionReceiptDetailsDialog : Form
    {
        // ------------------------------------------------------------------ Opening
        private static CollectionReceiptDetailsDialog? active;
        public static void ShowFor(Control parent, CollectionReceipt cr)
        {
            if (cr == null) return;
            var siblings = FindSiblingReceipts(parent);
            if (active != null && !active.IsDisposed)
            {
                active.siblings = siblings;
                active.LoadReceipt(cr);
                if (active.WindowState == FormWindowState.Minimized) active.WindowState = FormWindowState.Normal;
                active.Activate();
                return;
            }
            var dlg = Create(parent, cr);
            active = dlg;
            var owner = parent.FindForm();
            if (owner != null && !owner.IsDisposed) dlg.Show(owner); else dlg.Show();
        }
        /// <summary>Builds the window for a receipt without showing it (used by ShowFor and the verify checks).</summary>
        public static CollectionReceiptDetailsDialog Create(Control parent, CollectionReceipt cr)
        {
            var dlg = new CollectionReceiptDetailsDialog(parent, FindSiblingReceipts(parent));
            dlg.LoadReceipt(cr);
            return dlg;
        }
        /// <summary>The receipts listed in the table the user double-clicked, in on-screen order, for Previous / Next.</summary>
        private static List<CollectionReceipt> FindSiblingReceipts(Control parent)
        {
            var queue = new Queue<Control>();
            queue.Enqueue(parent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c is DataGridView g && g.Visible)
                {
                    var list = new List<CollectionReceipt>();
                    foreach (DataGridViewRow r in g.Rows) if (r.DataBoundItem is CollectionReceipt x) list.Add(x);
                    if (list.Count > 0) return list;
                }
                foreach (Control child in c.Controls) queue.Enqueue(child);
            }
            return new List<CollectionReceipt>();
        }
        // ------------------------------------------------------------------ State
        private List<CollectionReceipt> siblings;
        private CollectionReceipt receipt = new CollectionReceipt();
        private List<CollectionReceipt> rows = new List<CollectionReceipt>();
        private StatementOfAccount? soa;
        private Invoice? firstInvoice;
        private Dictionary<int, Invoice> invoices = new Dictionary<int, Invoice>();
        private Form? ownerForm;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private int RowH => D(30);
        private CollectionReceiptDetailsDialog(Control parent, List<CollectionReceipt> siblings)
        {
            this.siblings = siblings;
            Text = "Collection Receipt";
            Font = new Font("Segoe UI", 9.5F);
            BackColor = AppTheme.Canvas;
            ForeColor = AppTheme.TextPrimary;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(D(980), D(600)); // Nov 25: two columns of boxes need the width
            Size = new Size(D(1090), D(780));
            var wa = Screen.FromControl(parent).WorkingArea;
            if (Width > wa.Width) Width = wa.Width;
            if (Height > wa.Height) Height = wa.Height;
            if (parent.FindForm() is Form pf)
            {
                ownerForm = pf;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(Math.Max(wa.X, pf.Left + (pf.Width - Width) / 2), Math.Max(wa.Y, pf.Top + (pf.Height - Height) / 2));
                if (pf.Icon != null) Icon = pf.Icon;
            }
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
            FormClosed += (s, e) => { if (active == this) active = null; };
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape: Close(); return true;
                case Keys.Alt | Keys.Left: Navigate(-1); return true;
                case Keys.Alt | Keys.Right: Navigate(1); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ------------------------------------------------------------------ Data
        private static T? SafeGet<T>(Func<T> f, T? fallback = default)
        {
            try { return f(); } catch (System.Exception ex) { ErrLog.Swallowed(ex); return fallback; }
        }
        private void LoadReceipt(CollectionReceipt cr)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                receipt = cr;
                rows = SafeGet(() => MJdatabase.GetReceiptRows(cr.ReceiptNumber), new List<CollectionReceipt>()) ?? new List<CollectionReceipt>();
                if (rows.Count == 0) rows = new List<CollectionReceipt> { cr };
                soa = cr.SoaId > 0 ? SafeGet(() => MJdatabase.GetStatementsOfAccount().FirstOrDefault(s => s.Id == cr.SoaId), null) : null;
                var all = SafeGet(() => MJdatabase.GetInvoices(), new List<Invoice>()) ?? new List<Invoice>();
                invoices = all.GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
                firstInvoice = rows.Select(r => invoices.TryGetValue(r.InvoiceId, out var i) ? i : null).FirstOrDefault(i => i != null);
                BuildUi();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        private void Navigate(int step)
        {
            int idx = siblings.FindIndex(x => string.Equals(x.ReceiptNumber, receipt.ReceiptNumber, StringComparison.OrdinalIgnoreCase));
            int to = idx + step;
            if (idx < 0 || to < 0 || to >= siblings.Count) return;
            var next = siblings[to];
            if (IsHandleCreated) BeginInvoke(new Action(() => { if (!IsDisposed) LoadReceipt(next); }));
            else LoadReceipt(next);
        }
        private static string Or(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();
        // ------------------------------------------------------------------ Layout
        private void BuildUi()
        {
            SuspendLayout();
            foreach (Control c in Controls.Cast<Control>().ToList()) { Controls.Remove(c); c.Dispose(); }
            Text = $"Collection Receipt {receipt.ReceiptNumber}";
            var body = new NoJumpScrollPanel { Dock = DockStyle.Fill, BackColor = AppTheme.Canvas, AutoScroll = true, Padding = new Padding(D(24), D(16), D(24), D(12)) };
            body.Controls.Add(BuildStack());
            body.MouseEnter += (s, e) => { if (!body.Focused) body.Focus(); };
            Controls.Add(body);
            Controls.Add(BuildHeader());
            Controls.Add(BuildFooter());
            ResumeLayout(true);
            body.AutoScrollPosition = Point.Empty; // Nov 25: a receipt always opens at the top (Client box first)
            if (!sizedToContent) { sizedToContent = true; FitHeightToContent(body); }
        }
        private bool sizedToContent;
        /// <summary>Nov 25: the window opens just tall enough for all its boxes (it scrolls only when the screen is too small), centred on the app.</summary>
        private void FitHeightToContent(Panel body)
        {
            try
            {
                var stack = body.Controls.Count > 0 ? body.Controls[0] : null;
                if (stack == null) return;
                int contentH = stack.GetPreferredSize(new Size(body.ClientSize.Width - body.Padding.Horizontal, 0)).Height;
                int chrome = Height - ClientSize.Height;
                int otherH = Controls.Cast<Control>().Where(c => c != body).Sum(c => c.Height); // header + footer
                int want = chrome + otherH + body.Padding.Vertical + contentH + D(4);
                var wa = Screen.FromControl(this).WorkingArea;
                int h = Math.Max(MinimumSize.Height, Math.Min(want, wa.Height));
                if (h == Height) return;
                Height = h;
                int y = ownerForm != null ? ownerForm.Top + (ownerForm.Height - h) / 2 : wa.Y + (wa.Height - h) / 2;
                Top = Math.Max(wa.Y, Math.Min(y, wa.Bottom - h));
            }
            catch (System.Exception ex) { ErrLog.Swallowed(ex); }
        }
        /// <summary>Nov 25: the scroll panel doesn't jump to the invoice table when it takes focus, so the window opens at the top.</summary>
        private sealed class NoJumpScrollPanel : Panel
        {
            protected override Point ScrollToControl(Control activeControl) => DisplayRectangle.Location;
        }
        private Panel BuildHeader()
        {
            var p = new Panel { Dock = DockStyle.Top, Height = D(68), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(14), D(18), D(10)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1); };
            var titleRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(42), WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            titleRow.Controls.Add(new Label
            {
                Name = "lblReceiptTitle",
                Text = $"Collection Receipt {receipt.ReceiptNumber}",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 17F),
                ForeColor = AppTheme.TextPrimary,
                Margin = new Padding(0, 0, D(10), 0),
                UseMnemonic = false
            });
            titleRow.Controls.Add(new DispatchDetailsDialog.Pill("Issued · Paid", AppTheme.SuccessSoft, AppTheme.SuccessText) { Margin = new Padding(D(4), D(10), 0, 0) });
            p.Controls.Add(titleRow); // Nov 25: no read-only subtitle under the title
            return p;
        }
        /// <summary>One column of equal-width boxes with auto-sized rows; the scroll panel around it scrolls when the window is short.</summary>
        private Control BuildStack()
        {
            string clientName = Or(!string.IsNullOrWhiteSpace(soa?.ClientName) ? soa!.ClientName : !string.IsNullOrWhiteSpace(receipt.ClientName) ? receipt.ClientName : firstInvoice?.ClientName);
            // Nov 25: two equal columns, two rows; the boxes in a row are the same height:
            //   Client | Statement
            //   Service Invoice | Collection Receipt (receipt + payment in one box)
            var client = InfoCard("Client", new List<(string, string, bool)>
            {
                ("Client", clientName, false),
                ("Address", Or(soa?.ClientAddress), false),
                ("TIN", Or(soa?.ClientTin), false)
            });
            Control? statement = receipt.SoaId > 0
                ? InfoCard("Statement", new List<(string, string, bool)>
                {
                    ("Statement no.", Or(soa?.SoaNumber), false),
                    ("Issued", soa != null ? Fmt.Date(soa.IssueDate) : "—", false),
                    ("Period covered", Or(soa?.PeriodCovered), false),
                    ("Statement total", soa != null ? "₱" + soa.TotalAmount.ToString("N2") : "—", false)
                })
                : null;
            var receiptCard = BuildReceiptCard(); // Nov 25: one full-width box: the receipt, its payment and the full breakdown by service invoice
            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = AppTheme.Canvas,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            int gap = D(14);
            void Place(Control c, int col, int row, int span = 1)
            {
                c.Dock = DockStyle.Fill;
                c.Margin = new Padding(col == 0 ? 0 : gap / 2, 0, col == 0 && span == 1 ? gap / 2 : 0, row == 0 ? gap : 0);
                stack.Controls.Add(c, col, row);
                if (span > 1) stack.SetColumnSpan(c, span);
            }
            // equal heights in a row: both boxes take the taller one's height
            if (statement != null) { int h = Math.Max(client.Height, statement.Height); client.Height = h; statement.Height = h; }
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (statement != null) { Place(client, 0, 0); Place(statement, 1, 0); }
            else Place(client, 0, 0, span: 2); // a lone invoice has no statement: the client box takes the whole row
            Place(receiptCard, 0, 1, span: 2);
            return stack;
        }
        private Control InfoCard(string title, List<(string Label, string Value, bool Big)> items)
        {
            int pad = D(16);
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(pad, D(12), pad, D(12)) };
            card.Height = D(12) + D(32) + items.Count * RowH + D(14);
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var lblTitle = new Label { Dock = DockStyle.Top, Height = D(32), Text = title, Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.TopLeft };
            var tbl = new TableLayoutPanel { Dock = DockStyle.Top, Height = items.Count * RowH, ColumnCount = 2, RowCount = items.Count, BackColor = AppTheme.Surface };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(120)));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < items.Count; i++)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, RowH));
                var (label, value, big) = items[i];
                var l = new Label { Text = label, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) };
                var v = new Label
                {
                    Text = value,
                    Dock = DockStyle.Fill,
                    ForeColor = big ? AppTheme.SuccessText : AppTheme.TextPrimary,
                    Font = big ? new Font("Segoe UI Semibold", 13F, FontStyle.Bold) : new Font("Segoe UI Semibold", 9.75F),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Margin = new Padding(0),
                    UseMnemonic = false
                };
                tbl.Controls.Add(l, 0, i);
                tbl.Controls.Add(v, 1, i);
            }
            card.Controls.Add(tbl);
            card.Controls.Add(lblTitle);
            return card;
        }
        /// <summary>Nov 25: the one full-width Collection Receipt box: receipt and payment details in two columns, then the full breakdown by service invoice and the total paid.</summary>
        private Control BuildReceiptCard()
        {
            int pad = D(16);
            var card = new Panel { BackColor = AppTheme.Surface, Padding = new Padding(pad, D(12), pad, D(12)) };
            card.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            card.Resize += (s, e) => card.Invalidate();
            var lblTitle = new Label { Dock = DockStyle.Top, Height = D(32), Text = "Collection Receipt", Font = new Font("Segoe UI Semibold", 11.5F), ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.TopLeft };

            // receipt and payment details: label | value | label | value (the label column matches the Client / Statement boxes)
            var pairs = new List<(string, string)>
            {
                ("Receipt no.", Or(receipt.ReceiptNumber)), ("Payment date", Fmt.Date(receipt.PaymentDate)),
                ("Status", "Issued · Paid"), ("Method", Or(receipt.PaymentMethod)),
                ("Reference #", Or(receipt.ReferenceNumber)), ("Notes", Or(receipt.Notes))
            };
            int fieldRows = (pairs.Count + 1) / 2;
            var fields = new TableLayoutPanel { Dock = DockStyle.Top, Height = fieldRows * RowH, ColumnCount = 4, RowCount = fieldRows, BackColor = AppTheme.Surface };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(120)));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(120)));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < fieldRows; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, RowH));
            for (int i = 0; i < pairs.Count; i++)
            {
                var (label, value) = pairs[i];
                int row = i / 2, col = (i % 2) * 2;
                fields.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) }, col, row);
                fields.Controls.Add(new Label { Text = value, Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI Semibold", 9.75F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0), UseMnemonic = false }, col + 1, row);
            }

            var lblBreakdown = new Label { Dock = DockStyle.Top, Height = D(34), Text = "Breakdown", Font = new Font("Segoe UI Semibold", 10F), ForeColor = AppTheme.TextSecondary, TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, D(4)) };
            var g = new DataGridView
            {
                Name = "gridReceiptInvoices",
                Dock = DockStyle.Top,
                BackgroundColor = AppTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5F),
                MultiSelect = true
            };
            UIThemeHelper.ApplyGridTheme(g);
            g.RowTemplate.Height = D(32);
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = D(36);
            g.Columns.Add("c1", "Service invoice #");
            g.Columns.Add("c2", "Issue date");
            g.Columns.Add("c3", "Status");
            g.Columns.Add("c4", "Amount paid");
            g.Columns[0].FillWeight = 30; g.Columns[1].FillWeight = 25; g.Columns[2].FillWeight = 20; g.Columns[3].FillWeight = 25;
            g.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            g.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            foreach (var r in rows.OrderBy(x => x.InvoiceNumber))
            {
                invoices.TryGetValue(r.InvoiceId, out var inv);
                string status = inv == null ? "—" : SafeGet(() => StatusText.Billing(inv.Status, inv.NetAmount), "—") ?? "—";
                g.Rows.Add(Or(inv?.InvoiceNumber ?? r.InvoiceNumber),
                    inv != null ? Fmt.Date(inv.IssueDate) : "—",
                    status,
                    "₱" + r.AmountPaid.ToString("N2"));
            }
            g.ClearSelection();
            int shown = Math.Min(Math.Max(rows.Count, 1), 12);
            g.Height = D(36) + shown * D(32) + D(3);
            // no scrollbar of its own (12 rows or fewer): hand the wheel to the scrolling panel
            if (rows.Count <= 12)
                g.MouseWheel += (s, e) =>
                {
                    Control? c = g.Parent;
                    while (c != null && !(c is Panel pn && pn.AutoScroll)) c = c.Parent;
                    if (c is Panel sp) sp.AutoScrollPosition = new Point(-sp.AutoScrollPosition.X, -sp.AutoScrollPosition.Y - e.Delta);
                    if (e is HandledMouseEventArgs h) h.Handled = true;
                };

            // total paid: the whole receipt (all its invoice rows)
            var total = new Panel { Dock = DockStyle.Top, Height = D(46), BackColor = AppTheme.Surface };
            total.Controls.Add(new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), ForeColor = AppTheme.SuccessText, Text = "₱" + rows.Sum(x => x.AmountPaid).ToString("N2"), UseMnemonic = false });
            total.Controls.Add(new Label { Dock = DockStyle.Right, Width = D(16) });
            total.Controls.Add(new Label { Dock = DockStyle.Left, AutoSize = false, Width = D(320), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextSecondary, Text = $"Total paid  ·  {rows.Count} invoice{(rows.Count == 1 ? "" : "s")}" });

            // docked Top: added last = shown first
            card.Controls.Add(total);
            card.Controls.Add(g);
            card.Controls.Add(lblBreakdown);
            card.Controls.Add(fields);
            card.Controls.Add(lblTitle);
            card.Height = D(12) + D(32) + fields.Height + lblBreakdown.Height + g.Height + total.Height + D(12);
            return card;
        }
        private Panel BuildFooter()
        {
            var p = new Panel { Dock = DockStyle.Bottom, Height = D(60), BackColor = AppTheme.Surface, Padding = new Padding(D(24), D(10), D(24), D(10)) };
            p.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); };
            var right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = AppTheme.Surface };
            var btnClose = MakeButton("Close", "secondary", D(100));
            btnClose.Click += (s, e) => Close();
            var btnPrint = MakeButton("🖨 Print Official Receipt", "primary", D(210));
            btnPrint.Name = "btnPrintOfficialReceipt";
            btnPrint.Click += (s, e) =>
            {
                try { PrintHelper.PrintOfficialCollectionReceipt(receipt); }
                catch (System.Exception ex) { ErrLog.Swallowed(ex); }
            };
            right.Controls.Add(btnClose);
            right.Controls.Add(btnPrint);
            int idx = siblings.FindIndex(x => string.Equals(x.ReceiptNumber, receipt.ReceiptNumber, StringComparison.OrdinalIgnoreCase));
            var left = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, WrapContents = false, BackColor = AppTheme.Surface };
            var btnPrev = MakeButton("‹  Previous", "secondary", D(110));
            var btnNext = MakeButton("Next  ›", "secondary", D(96));
            btnPrev.Enabled = idx > 0;
            btnNext.Enabled = idx >= 0 && idx < siblings.Count - 1;
            btnPrev.Click += (s, e) => Navigate(-1);
            btnNext.Click += (s, e) => Navigate(1);
            left.Controls.Add(btnPrev);
            left.Controls.Add(btnNext);
            if (idx >= 0)
                left.Controls.Add(new Label { Text = $"{idx + 1} of {siblings.Count}", AutoSize = true, ForeColor = AppTheme.TextMuted, Margin = new Padding(D(8), D(8), 0, 0) });
            p.Controls.Add(right);
            p.Controls.Add(left);
            return p;
        }
        private Button MakeButton(string text, string role, int width)
        {
            var b = new Button { Text = text, Width = width, Height = D(36), Cursor = Cursors.Hand, UseMnemonic = false, Margin = new Padding(D(8), 0, 0, 0) };
            UIThemeHelper.SetButtonRole(b, role);
            return b;
        }
    }
}
