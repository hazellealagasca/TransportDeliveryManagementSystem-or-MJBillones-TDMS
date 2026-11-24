using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 25: read-only preview under the receipts list. Clicking an issued receipt shows it here (plain labels, nothing to type in);
    /// the only actions are printing the official receipt and opening its details.
    /// </summary>
    public partial class CollectionView
    {
        private Panel? pnlPreview;
        private Label? previewTitle;
        private Label? previewEmpty;
        private TableLayoutPanel? previewTable;
        private Panel? previewButtons;
        private readonly System.Collections.Generic.Dictionary<string, Label> previewValues = new System.Collections.Generic.Dictionary<string, Label>();
        private CollectionReceipt? previewShown;

        private void BuildReceiptPreview()
        {
            if (pnlPreview != null) return;
            pnlPreview = new Panel { Name = "pnlReceiptPreview", Dock = DockStyle.Bottom, Height = Dpi(190), BackColor = AppTheme.Surface, Padding = new Padding(Dpi(14), Dpi(10), Dpi(14), Dpi(8)), Margin = new Padding(0) };
            pnlPreview.Paint += (s, e) => { using var pen = new Pen(AppTheme.Border); e.Graphics.DrawLine(pen, 0, 0, pnlPreview.Width, 0); };
            previewTitle = new Label { Name = "previewTitle", Dock = DockStyle.Top, Height = Dpi(26), Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold), ForeColor = AppTheme.TextPrimary, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
            previewEmpty = new Label { Name = "previewEmpty", Dock = DockStyle.Top, Height = Dpi(60), Font = new Font("Segoe UI", 9.75F), ForeColor = AppTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, Text = "Click a receipt above to preview it." };

            previewButtons = new Panel { Dock = DockStyle.Right, Width = Dpi(150), Padding = new Padding(Dpi(10), Dpi(4), 0, 0) };
            var btnPrintOr = new Button { Name = "btnPreviewPrint", Text = "🖨 Print OR", Dock = DockStyle.Top, Height = Dpi(34), Cursor = Cursors.Hand };
            var btnOpen = new Button { Name = "btnPreviewOpen", Text = "Open details", Dock = DockStyle.Top, Height = Dpi(34), Cursor = Cursors.Hand };
            UIThemeHelper.SetButtonRole(btnPrintOr, "primary");
            UIThemeHelper.SetButtonRole(btnOpen, "secondary");
            btnPrintOr.Click += (s, e) => { if (previewShown != null) PrintHelper.PrintOfficialCollectionReceipt(previewShown); };
            btnOpen.Click += (s, e) => { if (previewShown != null) RecordDetailsDialog.ShowDetails(this, previewShown); };
            var gap = new Panel { Dock = DockStyle.Top, Height = Dpi(8) };
            previewButtons.Controls.Add(btnOpen);
            previewButtons.Controls.Add(gap);
            previewButtons.Controls.Add(btnPrintOr);

            previewTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Margin = new Padding(0) };
            previewTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(150)));
            previewTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            previewTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi(110)));
            previewTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 4; i++) previewTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            AddPreviewPair(0, 0, "Statement / invoices paid", "invoice");
            AddPreviewPair(0, 2, "Client", "client");
            AddPreviewPair(1, 0, "Payment date", "date");
            AddPreviewPair(1, 2, "Amount paid", "amount", bold: true);
            AddPreviewPair(2, 0, "Method", "method");
            AddPreviewPair(2, 2, "Reference #", "reference");
            AddPreviewPair(3, 0, "Notes", "notes", span: true);

            pnlPreview.Controls.Add(previewTable);
            pnlPreview.Controls.Add(previewEmpty);
            pnlPreview.Controls.Add(previewButtons);
            pnlPreview.Controls.Add(previewTitle);
            previewTable.BringToFront();
            gridCollections.Parent?.Controls.Add(pnlPreview);
            pnlPreview.SendToBack(); // docked after the summary bar (which goes to the very back when it is attached), so it sits between the table and the bar; the table keeps Fill
            gridCollections.BringToFront();
            pnlForm.VisibleChanged += (s, e) => UpdateReceiptPreview();
            UpdateReceiptPreview();
        }

        private void AddPreviewPair(int row, int col, string caption, string key, bool bold = false, bool span = false)
        {
            var cap = new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            var val = new Label { Name = "previewValue_" + key, Dock = DockStyle.Fill, ForeColor = AppTheme.TextPrimary, Font = new Font("Segoe UI", 9.75F, bold ? FontStyle.Bold : FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
            previewTable!.Controls.Add(cap, col, row);
            previewTable.Controls.Add(val, col + 1, row);
            if (span) previewTable.SetColumnSpan(val, 3);
            previewValues[key] = val;
        }

        /// <summary>Shows the clicked receipt (or the hint when none is clicked); hidden while the full-page form is open.</summary>
        private void UpdateReceiptPreview()
        {
            if (pnlPreview == null) return;
            pnlPreview.Visible = !pnlForm.Visible;
            var cr = gridCollections.SelectedRows.Count > 0 ? gridCollections.SelectedRows[0].DataBoundItem as CollectionReceipt : null;
            previewShown = cr;
            bool has = cr != null;
            previewTable!.Visible = has; previewButtons!.Visible = has; previewEmpty!.Visible = !has;
            previewTitle!.Text = has ? $"Receipt {cr!.ReceiptNumber} · Issued (read-only)" : "Receipt preview";
            if (!has) return;
            void Set(string k, string v) => previewValues[k].Text = string.IsNullOrWhiteSpace(v) ? "—" : v;
            Set("invoice", cr!.InvoiceNumber);
            Set("client", cr.ClientName);
            Set("date", Fmt.Date(cr.PaymentDate));
            Set("amount", "₱" + cr.AmountPaid.ToString("N2"));
            Set("method", cr.PaymentMethod);
            Set("reference", cr.ReferenceNumber);
            Set("notes", cr.Notes);
        }
    }
}
