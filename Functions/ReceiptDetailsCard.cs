using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Step 1 of the collection receipt: a card with an accent bar, label / value rows (statement, client, service invoices, trip reports)
    /// and the statement breakdown (dotted leaders) ending in a maroon TOTAL AMOUNT DUE block. It paints itself and reports the height it needs.
    /// </summary>
    public sealed class ReceiptDetailsCard : Control
    {
        private string? message = "Pick a statement or invoice above";
        private readonly List<(string Label, string Value)> rows = new List<(string, string)>();
        private (string, string)[] breakdown = Array.Empty<(string, string)>();
        private string? total;
        private string? note;
        private readonly Font fLabel = new Font("Segoe UI", 10F);
        private readonly Font fValue = new Font("Segoe UI Semibold", 10F);
        private readonly Font fBig = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);

        public ReceiptDetailsCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { fLabel.Dispose(); fValue.Dispose(); fBig.Dispose(); }
            base.Dispose(disposing);
        }

        private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private static string Peso(decimal v) => "₱" + v.ToString("N2");

        /// <summary>A single plain line (nothing picked yet, or a saved receipt is open).</summary>
        public void ShowMessage(string text)
        {
            message = text; rows.Clear(); total = null; note = null; breakdown = Array.Empty<(string, string)>();
            Invalidate();
        }

        /// <summary>The picked statement / invoice with its breakdown rows (as the statement screen shows them); the big total is what is verified and paid.</summary>
        public void ShowDetails(bool isSoa, string number, string client, string invoices, string tripReports, (string, string)[] breakdownRows, decimal net)
        {
            message = null; note = null;
            rows.Clear();
            rows.Add((isSoa ? "Statement" : "Service invoice", number));
            rows.Add(("Client", client));
            if (!string.IsNullOrWhiteSpace(invoices)) rows.Add(("Service invoices", invoices));
            if (!string.IsNullOrWhiteSpace(tripReports)) rows.Add(("Trip reports", tripReports));
            breakdown = breakdownRows;
            total = Peso(net);
            Invalidate();
        }

        /// <summary>An extra line under the details (special trip reference).</summary>
        public void SetNote(string? text)
        {
            note = string.IsNullOrWhiteSpace(text) ? null : text;
            Invalidate();
        }

        /// <summary>The height the card needs at the given width, so nothing is clipped.</summary>
        public int MeasureHeight(int width) => Render(null, width);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(AppTheme.Surface);
            using (var pen = new Pen(AppTheme.Border)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            using (var br = new SolidBrush(AppTheme.Accent)) g.FillRectangle(br, 0, 0, S(5), Height);
            Render(g, Width);
        }

        private int Render(Graphics? g, int width)
        {
            const TextFormatFlags wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.Top;
            const TextFormatFlags one = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.Right | TextFormatFlags.Top;
            const TextFormatFlags oneL = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis;
            int pad = S(16), left = S(5) + pad, right = Math.Max(left + S(200), width - pad);

            if (message != null)
            {
                int w = right - left;
                var ms = TextRenderer.MeasureText(message, fValue, new Size(w, 0), wrap);
                int h = Math.Max(S(52), ms.Height + 2 * S(14));
                if (g != null) TextRenderer.DrawText(g, message, fValue, new Rectangle(left, (h - ms.Height) / 2, w, ms.Height), AppTheme.TextMuted, wrap);
                return h;
            }

            int y = S(14);
            int valX = left + S(130), valW = Math.Max(S(80), right - valX);
            var all = new List<(string Label, string Value)>(rows);
            if (note != null) all.Add(("Note", note));
            foreach (var (label, value) in all)
            {
                var vs = TextRenderer.MeasureText(value, fValue, new Size(valW, 0), wrap);
                var ls = TextRenderer.MeasureText(label, fLabel, new Size(S(120), 0), wrap);
                int rh = Math.Max(vs.Height, ls.Height);
                if (g != null)
                {
                    TextRenderer.DrawText(g, label, fLabel, new Rectangle(left, y, S(120), rh), AppTheme.TextMuted, wrap);
                    TextRenderer.DrawText(g, value, fValue, new Rectangle(valX, y, valW, vs.Height), AppTheme.TextPrimary, wrap);
                }
                y += rh + S(8);
            }

            y += S(4);
            if (g != null) using (var p = new Pen(AppTheme.Border)) g.DrawLine(p, left, y, right, y);
            y += S(12);

            int lineH = TextRenderer.MeasureText("Ag", fLabel, new Size(int.MaxValue, 0), one).Height;
            foreach (var (label, amount) in breakdown)
            {
                int vw = TextRenderer.MeasureText(amount, fValue, new Size(int.MaxValue, 0), one).Width;
                int lw = TextRenderer.MeasureText(label, fLabel, new Size(int.MaxValue, 0), one).Width;
                if (g != null)
                {
                    TextRenderer.DrawText(g, label, fLabel, new Rectangle(left, y, Math.Max(S(40), right - left - vw - S(8)), lineH), AppTheme.TextSecondary, oneL);
                    TextRenderer.DrawText(g, amount, fValue, new Rectangle(right - vw - S(2), y, vw + S(2), lineH), AppTheme.TextPrimary, one);
                    int x1 = left + lw + S(6), x2 = right - vw - S(10), ly = y + lineH - S(4);
                    if (x2 > x1) using (var dp = new Pen(AppTheme.BorderStrong) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot }) g.DrawLine(dp, x1, ly, x2, ly);
                }
                y += lineH + S(6);
            }
            y += S(6);
            int th = TextRenderer.MeasureText("Ag", fBig, new Size(int.MaxValue, 0), one).Height;
            int blockH = lineH + th + S(20);
            if (g != null)
            {
                var block = new Rectangle(S(5), y, Math.Max(1, width - S(5) - 1), blockH);
                using (var br = new SolidBrush(AppTheme.Accent)) g.FillRectangle(br, block);
                TextRenderer.DrawText(g, "TOTAL AMOUNT DUE", fLabel, new Rectangle(left, y + S(8), right - left, lineH), Color.White, oneL);
                TextRenderer.DrawText(g, total ?? "", fBig, new Rectangle(left, y + S(8) + lineH + S(2), right - left, th), Color.White, one);
            }
            return y + blockH + S(14);
        }
    }
}
