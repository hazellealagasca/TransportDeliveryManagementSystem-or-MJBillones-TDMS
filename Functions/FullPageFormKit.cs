using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Turns a list screen's side drawer form into a full-page form in the same tab (the Dispatch layout):
    /// a "← Back" button at the top left, the fields arranged in numbered section cards in a centred column,
    /// and the action buttons in a bar along the bottom with the main action at the far right.
    /// </summary>
    public sealed class FullPageFormKit
    {
        public sealed class Section
        {
            public string Title = "";
            public string Subtitle = "";
            public Control?[] Controls = Array.Empty<Control?>();
            public Section(string title, string subtitle, params Control?[] controls) { Title = title; Subtitle = subtitle; Controls = controls; }
        }
        private readonly Control host;
        private readonly TableLayoutPanel tblMaster;
        private readonly Panel pnlForm;
        private readonly Control listPanel;
        private readonly Panel header;
        private readonly Label title;
        private readonly Button back;
        private readonly Panel body;
        private readonly Panel actionsBar;
        private readonly int maxWidth;
        private FlowLayoutPanel? flowActions;
        private Panel? headerSpacer;
        private readonly Padding originalMasterPadding;
        private int D(int v) => (int)Math.Round(v * host.DeviceDpi / 96f);
        public FullPageFormKit(Control host, TableLayoutPanel tblMaster, Panel pnlForm, Control listPanel, Panel header, Label title, Button back,
            Panel body, Panel actionsBar, string backText, int maxContentWidth = 900)
        {
            this.host = host; this.tblMaster = tblMaster; this.pnlForm = pnlForm; this.listPanel = listPanel; this.header = header;
            this.title = title; this.back = back; this.body = body; this.actionsBar = actionsBar; this.maxWidth = maxContentWidth;
            originalMasterPadding = tblMaster.Padding;
            back.Text = "←  " + backText;
            pnlForm.Resize += (s, e) => Center();
        }
        /// <summary>
        /// Moves the existing fields into section cards. Controls left out of every section go into the last one.
        /// <paramref name="actionContainers"/> are the panels/tables holding the form's buttons (default: everything in the action bar).
        /// </summary>
        public void Build(IEnumerable<Section> sections, IEnumerable<Control>? actionContainers = null)
        {
            var secs = sections.ToList();
            // A field may have been wrapped in a panel by the screen's own code; move the wrapper that sits in the form body.
            Control? TopLevel(Control? c)
            {
                if (c == null) return null;
                var cur = c;
                while (cur.Parent != null && cur.Parent != body) cur = cur.Parent;
                return cur.Parent == body ? cur : c;
            }
            foreach (var sec in secs) sec.Controls = sec.Controls.Select(TopLevel).Distinct().ToArray();
            body.SuspendLayout();
            actionsBar.SuspendLayout();
            // Buttons → bottom bar
            var containers = (actionContainers ?? actionsBar.Controls.Cast<Control>().ToList()).Where(c => c != null).ToList();
            var buttons = new List<Control>();
            foreach (var c in containers) CollectActionControls(c, buttons);
            foreach (var c in containers)
            {
                c.Parent?.Controls.Remove(c);
            }
            foreach (Control c in actionsBar.Controls.Cast<Control>().ToList()) actionsBar.Controls.Remove(c);
            flowActions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = AppTheme.Surface, Margin = new Padding(0) };
            foreach (var b in buttons.OrderBy(Rank))
            {
                b.Parent?.Controls.Remove(b);
                b.Dock = DockStyle.None;
                b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                if (b is Button btn)
                {
                    btn.AutoSize = true;
                    btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    btn.MinimumSize = new Size(D(130), D(40));
                    btn.Padding = new Padding(D(14), 0, D(14), 0);
                    btn.Font = new Font("Segoe UI Semibold", 9.5F);
                }
                else
                {
                    b.Height = D(40);
                }
                b.Margin = new Padding(D(10), 0, 0, 0);
                flowActions.Controls.Add(b);
            }
            actionsBar.Controls.Add(flowActions);
            actionsBar.Height = D(66);
            actionsBar.BackColor = AppTheme.Surface;
            actionsBar.BorderStyle = BorderStyle.None;
            actionsBar.Paint -= ActionsBarPaint;
            actionsBar.Paint += ActionsBarPaint;
            // Fields → section cards (in on-screen order: the last control in a docked panel sits at the top)
            var remaining = body.Controls.Cast<Control>().Reverse().ToList();
            foreach (var c in remaining) body.Controls.Remove(c);
            var placed = new HashSet<Control>(secs.SelectMany(s => s.Controls).Where(c => c != null)!);
            var leftovers = remaining.Where(c => !placed.Contains(c) && !containers.Contains(c)).ToList();
            if (secs.Count == 0) secs.Add(new Section("Details", "", Array.Empty<Control>()));
            if (leftovers.Count > 0)
            {
                var last = secs[^1];
                last.Controls = last.Controls.Concat(leftovers).ToArray();
            }
            var cards = new List<Control>();
            for (int i = 0; i < secs.Count; i++)
            {
                var visual = secs[i].Controls.Where(c => c != null).Select(c => c!).ToList();
                foreach (var c in visual) PolishFieldControl(c);
                cards.Add(CreateSectionCard(i + 1, secs[i].Title, secs[i].Subtitle, visual));
            }
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                body.Controls.Add(cards[i]);
                if (i > 0) body.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(16), BackColor = AppTheme.Canvas });
            }
            body.AutoScroll = true;
            body.ResumeLayout(true);
            actionsBar.ResumeLayout(true);
            Restyle();
        }
        private void ActionsBarPaint(object? sender, PaintEventArgs e)
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, 0, actionsBar.Width, 0);
        }
        private static void CollectActionControls(Control c, List<Control> into)
        {
            if (c is Button) { into.Add(c); return; }
            foreach (Control child in c.Controls) CollectActionControls(child, into);
        }
        /// <summary>Right-to-left order: save/update/approve at the far right, delete and clear further left.</summary>
        private static int Rank(Control c)
        {
            string n = (c.Name + " " + c.Text).ToLowerInvariant();
            if (n.Contains("save") || n.Contains("record") || n.Contains("generate") || n.Contains("create")) return 0;
            if (n.Contains("update")) return 1;
            if (n.Contains("approve")) return 2;
            if (n.Contains("print")) return 4;
            if (n.Contains("delete") || n.Contains("void") || n.Contains("cancel")) return 6;
            if (n.Contains("clear") || n.Contains("reset")) return 8;
            return 5;
        }
        /// <summary>Header, colours and spacing for the full-page look (call again after the theme is applied).</summary>
        public void Restyle()
        {
            foreach (Control c in pnlForm.Controls)
                if (c.Name == "pnlFormResizeGrip") c.Visible = false;
            pnlForm.BackColor = AppTheme.Canvas;
            pnlForm.BorderStyle = BorderStyle.None;
            body.BackColor = AppTheme.Canvas;
            header.BackColor = AppTheme.Canvas;
            header.Height = D(62);
            back.AutoSize = false;
            back.MinimumSize = Size.Empty;
            back.Dock = DockStyle.Left;
            back.Width = Math.Max(D(170), TextRenderer.MeasureText(back.Text, new Font("Segoe UI Semibold", 9.5F)).Width + D(36));
            back.Font = new Font("Segoe UI Semibold", 9.5F);
            back.Padding = new Padding(0);
            UIThemeHelper.SetButtonRole(back, "secondary");
            back.ForeColor = AppTheme.TextPrimary;
            headerSpacer ??= new Panel { Dock = DockStyle.Left, Width = D(16), BackColor = AppTheme.Canvas };
            if (!header.Controls.Contains(headerSpacer)) header.Controls.Add(headerSpacer);
            if (!header.Controls.Contains(title)) header.Controls.Add(title);
            title.AutoSize = false;
            title.AutoEllipsis = true;
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font("Segoe UI Semibold", 15F);
            title.ForeColor = AppTheme.TextPrimary;
            title.BackColor = AppTheme.Canvas;
            title.UseMnemonic = false;
            header.Controls.SetChildIndex(title, 0);
            header.Controls.SetChildIndex(headerSpacer, 1);
            header.Controls.SetChildIndex(back, 2);
            Center();
        }
        private void Center()
        {
            if (pnlForm.Width <= 0) return;
            int side = Math.Max(D(24), (pnlForm.ClientSize.Width - D(maxWidth)) / 2);
            header.Padding = new Padding(side, D(10), side, D(14));
            body.Padding = new Padding(side, 0, side + SystemInformation.VerticalScrollBarWidth / 2, D(16));
            actionsBar.Padding = new Padding(side, D(12), side, D(12));
        }
        /// <summary>Shows the form as a full page in place of the list, or goes back to the list.</summary>
        public void Show(bool visible)
        {
            tblMaster.SuspendLayout();
            pnlForm.Visible = visible;
            listPanel.Visible = !visible;
            if (tblMaster.ColumnStyles.Count > 1)
            {
                tblMaster.ColumnStyles[0].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
                tblMaster.ColumnStyles[0].Width = visible ? 100F : 0F;
                tblMaster.ColumnStyles[1].SizeType = visible ? SizeType.Absolute : SizeType.Percent;
                tblMaster.ColumnStyles[1].Width = visible ? 0F : 100F;
            }
            pnlForm.Margin = new Padding(0);
            tblMaster.Padding = visible ? new Padding(0) : originalMasterPadding;
            tblMaster.ResumeLayout(true);
            if (visible)
            {
                Center();
                body.AutoScrollPosition = new Point(0, 0);
            }
            foreach (var g in Descendants(listPanel).OfType<DataGridView>())
            {
                if (g.Columns.Count == 0) continue;
                var mode = g.AutoSizeColumnsMode;
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                g.AutoSizeColumnsMode = mode;
            }
        }
        private static IEnumerable<Control> Descendants(Control c)
        {
            foreach (Control child in c.Controls)
            {
                yield return child;
                foreach (var d in Descendants(child)) yield return d;
            }
        }
        // ------------------------------------------------------------------ Cards and field styling
        private void PolishFieldControl(Control c)
        {
            if (c is Label l && !(c is LinkLabel))
            {
                if (l.Font.Size <= 9.5f)
                {
                    l.Font = new Font("Segoe UI Semibold", 9F);
                    if (l.ForeColor.GetBrightness() < 0.5f || l.ForeColor == SystemColors.ControlText) l.ForeColor = AppTheme.TextSecondary;
                }
                if (l.Dock == DockStyle.Top && l.AutoSize) { l.AutoSize = false; l.Height = Math.Max(l.Height, D(28)); l.TextAlign = ContentAlignment.BottomLeft; }
            }
            foreach (Control child in c.Controls) PolishInput(child);
            PolishInput(c);
        }
        private static void PolishInput(Control c)
        {
            if (c is TextBox || c is ComboBox || c is NumericUpDown || c is DateTimePicker)
            {
                if (c.Font.Size < 10f) c.Font = new Font("Segoe UI", 10F, c.Font.Bold ? FontStyle.Bold : FontStyle.Regular);
            }
            foreach (Control child in c.Controls) if (!(child is TextBox && c is NumericUpDown)) PolishInput(child);
        }
        private Panel CreateSectionCard(int step, string titleText, string subtitle, List<Control> contentTopToBottom)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface,
                Padding = new Padding(D(24), D(18), D(24), D(22)),
                Margin = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(AppTheme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Resize += (s, e) => card.Invalidate();
            for (int i = contentTopToBottom.Count - 1; i >= 0; i--)
            {
                var c = contentTopToBottom[i];
                c.Parent?.Controls.Remove(c);
                c.Dock = DockStyle.Top;
                if (c.BackColor == Color.White || c.BackColor == SystemColors.Control) c.BackColor = AppTheme.Surface;
                card.Controls.Add(c);
            }
            var hdr = new Panel { Dock = DockStyle.Top, Height = D(56), BackColor = AppTheme.Surface };
            hdr.Paint += (s, e) =>
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                int d = D(30);
                var circle = new Rectangle(0, D(2), d, d);
                using (var b = new SolidBrush(AppTheme.AccentSoft)) g.FillEllipse(b, circle);
                const TextFormatFlags f = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
                using (var nf = new Font("Segoe UI Semibold", 10F))
                    TextRenderer.DrawText(g, step.ToString(), nf, circle, AppTheme.AccentText, f | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                using (var tf = new Font("Segoe UI Semibold", 12F))
                    TextRenderer.DrawText(g, titleText, tf, new Point(d + D(12), 0), AppTheme.TextPrimary, f);
                using (var sf = new Font("Segoe UI", 9F))
                    TextRenderer.DrawText(g, subtitle, sf, new Rectangle(d + D(12), D(24), hdr.Width - d - D(12), D(22)), AppTheme.TextMuted, f | TextFormatFlags.EndEllipsis);
                using var pen = new Pen(AppTheme.Border);
                g.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            };
            hdr.Resize += (s, e) => hdr.Invalidate();
            card.Controls.Add(new Panel { Dock = DockStyle.Top, Height = D(8), BackColor = AppTheme.Surface });
            card.Controls.Add(hdr);
            return card;
        }
    }
}
