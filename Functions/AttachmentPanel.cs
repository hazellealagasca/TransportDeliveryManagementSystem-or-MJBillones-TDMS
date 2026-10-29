using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Attachments for one record, shown as thumbnails. Add several files at once (button or drag and drop),
    /// double-click to view, right-click to re-categorise, open the folder, or void.
    /// </summary>
    public sealed class AttachmentPanel : Panel
    {
        private string ownerType = "";
        private int ownerId;
        private string ownerRef = "";
        private readonly ComboBox cbCategory;
        private readonly Button btnAdd;
        private readonly Label lblInfo;
        private readonly FlowLayoutPanel flow;
        private readonly Label lblEmpty;
        private List<Attachment> items = new List<Attachment>();
        private static readonly Dictionary<string, Image> thumbCache = new Dictionary<string, Image>();
        public event EventHandler? AttachmentsChanged;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<Attachment> Items => items;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public AttachmentPanel(string defaultCategory = "Proof of delivery (POD)")
        {
            BackColor = AppTheme.Surface;
            Padding = new Padding(D(10));
            AllowDrop = true;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(40), WrapContents = false, BackColor = AppTheme.Surface };
            cbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = D(200), Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, D(4), D(8), 0) };
            cbCategory.Items.AddRange(MJdatabase.AttachmentCategories.Cast<object>().ToArray());
            cbCategory.SelectedItem = MJdatabase.AttachmentCategories.Contains(defaultCategory) ? defaultCategory : MJdatabase.AttachmentCategories[0];
            btnAdd = new Button { Text = "📎  Add files…", AutoSize = true, MinimumSize = new Size(0, D(30)), Cursor = Cursors.Hand, Margin = new Padding(0, D(2), D(10), 0) };
            UIThemeHelper.SetButtonRole(btnAdd, "secondary");
            btnAdd.Click += (s, e) => PickFiles();
            lblInfo = new Label { AutoSize = true, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 8.75F), Margin = new Padding(0, D(9), 0, 0) };
            bar.Controls.Add(cbCategory);
            bar.Controls.Add(btnAdd);
            bar.Controls.Add(lblInfo);
            flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = AppTheme.SurfaceAlt, Padding = new Padding(D(6)), AllowDrop = true };
            lblEmpty = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = AppTheme.TextMuted, Font = new Font("Segoe UI", 9.5F, FontStyle.Italic), BackColor = AppTheme.SurfaceAlt, AllowDrop = true };
            Controls.Add(flow);
            Controls.Add(lblEmpty);
            Controls.Add(bar);
            foreach (Control c in new Control[] { this, flow, lblEmpty })
            {
                c.DragEnter += (s, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true && ownerId > 0) e.Effect = DragDropEffects.Copy; };
                c.DragDrop += (s, e) => { if (e.Data?.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files); };
            }
            Render();
        }
        /// <summary>Points the panel at a record. An id of 0 means "not saved yet": adding is disabled.</summary>
        public void SetOwner(string type, int id, string reference)
        {
            ownerType = type; ownerId = id; ownerRef = reference ?? "";
            Reload();
        }
        public void Reload()
        {
            try { items = ownerId > 0 ? MJdatabase.GetAttachments(ownerType, ownerId) : new List<Attachment>(); } catch { items = new List<Attachment>(); }
            Render();
        }
        private void Render()
        {
            btnAdd.Enabled = ownerId > 0;
            cbCategory.Enabled = ownerId > 0;
            lblInfo.Text = ownerId <= 0 ? "Pick / save the record first" : items.Count == 0 ? "Drop files here or click Add · max 15 MB each" : $"{items.Count} file(s) · {items.Count(i => i.Category.StartsWith("Proof"))} POD · double-click to view";
            flow.SuspendLayout();
            foreach (Control c in flow.Controls) c.Dispose();
            flow.Controls.Clear();
            foreach (var a in items) flow.Controls.Add(new Tile(this, a));
            flow.ResumeLayout();
            lblEmpty.Text = ownerId <= 0 ? "Attachments appear here once the record is chosen." : "No files yet. Drag PODs, receipts or trip sheets here, or click \"Add files…\".";
            lblEmpty.Visible = items.Count == 0;
            flow.Visible = items.Count > 0;
        }
        private void PickFiles()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Attach files",
                Multiselect = true,
                Filter = "Documents and images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.csv;*.txt|Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|PDF|*.pdf|All files|*.*"
            };
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK) AddFiles(dlg.FileNames);
        }
        private void AddFiles(IEnumerable<string> paths)
        {
            if (ownerId <= 0) { MessageBox.Show(FindForm(), "Choose or save the record first, then attach files.", "Attachments", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var list = paths.Where(p => !Directory.Exists(p)).ToList();
            if (list.Count == 0) return;
            string category = cbCategory.Text;
            var rejected = list.Select(p => (Path: p, Why: MJdatabase.ValidateAttachmentFile(p))).Where(x => x.Why != null).ToList();
            var ok = list.Except(rejected.Select(r => r.Path)).ToList();
            if (ok.Count > 0 && MessageBox.Show(FindForm(), $"Are you sure you want to attach {ok.Count} file(s) as \"{category}\" to {ownerRef}?", "Add attachments", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int added = 0;
            var problems = rejected.Select(r => r.Why!).ToList();
            Cursor = Cursors.WaitCursor;
            try
            {
                foreach (var p in ok)
                {
                    try { MJdatabase.AddAttachment(ownerType, ownerId, ownerRef, p, category); added++; }
                    catch (Exception ex) { problems.Add($"{Path.GetFileName(p)}: {ex.Message}"); }
                }
            }
            finally { Cursor = Cursors.Default; }
            Reload();
            AttachmentsChanged?.Invoke(this, EventArgs.Empty);
            if (problems.Count > 0)
                MessageBox.Show(FindForm(), (added > 0 ? $"{added} file(s) attached.\n\n" : "") + "Not attached:\n• " + string.Join("\n• ", problems), "Attachments", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else if (added > 0) ToastHelper.ShowToast(this, $"{added} file(s) attached", ToastType.Success);
        }
        internal void Open(Attachment a)
        {
            if (!File.Exists(a.StoredPath)) { MessageBox.Show(FindForm(), $"The stored file is missing:\n{a.StoredPath}", "Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (a.IsImage) AttachmentViewer.Show(FindForm(), items.Where(i => i.IsImage).ToList(), a);
            else OpenExternally(a.StoredPath);
        }
        internal static void OpenExternally(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { ErrLog.Error("AttachmentPanel", ex);  MessageBox.Show("Couldn't open the file: " + ex.Message, "Attachment", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        internal ContextMenuStrip MenuFor(Attachment a)
        {
            var m = new ContextMenuStrip();
            m.Items.Add("Open", null, (s, e) => Open(a));
            m.Items.Add("Show in folder", null, (s, e) => { try { Process.Start("explorer.exe", $"/select,\"{a.StoredPath}\""); } catch (System.Exception swx1) { ErrLog.Swallowed(swx1); } });
            var cat = new ToolStripMenuItem("Change category");
            foreach (var c in MJdatabase.AttachmentCategories)
            {
                var it = new ToolStripMenuItem(c) { Checked = c == a.Category };
                it.Click += (s, e) =>
                {
                    if (c == a.Category) return;
                    if (MessageBox.Show(FindForm(), $"Are you sure you want to change \"{a.FileName}\" to {c}?", "Change category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    MJdatabase.SetAttachmentCategory(a.Id, c);
                    Reload();
                    AttachmentsChanged?.Invoke(this, EventArgs.Empty);
                };
                cat.DropDownItems.Add(it);
            }
            m.Items.Add(cat);
            m.Items.Add("Copy file path", null, (s, e) => { try { Clipboard.SetText(a.StoredPath); } catch (System.Exception swx2) { ErrLog.Swallowed(swx2); } });
            m.Items.Add(new ToolStripSeparator());
            var v = new ToolStripMenuItem("Void attachment…") { ForeColor = AppTheme.DangerText };
            v.Click += (s, e) =>
            {
                if (MessageBox.Show(FindForm(), $"Are you sure you want to void \"{a.FileName}\"?\n\nIt disappears from this list but the file and its record are kept for the audit trail.", "Void attachment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                MJdatabase.VoidAttachment(a.Id);
                Reload();
                AttachmentsChanged?.Invoke(this, EventArgs.Empty);
            };
            m.Items.Add(v);
            return m;
        }
        /// <summary>Small cached thumbnail; non-images get a document badge with the file extension.</summary>
        internal static Image? Thumb(Attachment a, int size)
        {
            string key = a.StoredPath + "|" + size;
            if (thumbCache.TryGetValue(key, out var img)) return img;
            if (!a.IsImage || !File.Exists(a.StoredPath)) return null;
            try
            {
                using var src = SecurityHelper.LoadDecoupledBitmap(a.StoredPath);
                if (src == null) return null;
                double k = Math.Min((double)size / src.Width, (double)size / src.Height);
                var bmp = new Bitmap(Math.Max(1, (int)(src.Width * k)), Math.Max(1, (int)(src.Height * k)));
                using (var g = Graphics.FromImage(bmp)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(src, 0, 0, bmp.Width, bmp.Height); }
                if (thumbCache.Count > 300) thumbCache.Clear();
                thumbCache[key] = bmp;
                return bmp;
            }
            catch { return null; }
        }
        private sealed class Tile : Control
        {
            private readonly AttachmentPanel owner;
            private readonly Attachment a;
            private bool hover;
            private readonly ToolTip tip = new ToolTip();
            private int S(int v) => (int)Math.Round(v * DeviceDpi / 96f);
            public Tile(AttachmentPanel owner, Attachment a)
            {
                this.owner = owner; this.a = a;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Size = new Size(S(132), S(150));
                Margin = new Padding(S(4));
                Cursor = Cursors.Hand;
                tip.SetToolTip(this, $"{a.FileName}\n{a.Category} · {a.SizeDisplay}\nAdded {a.AddedAt:MMM d, yyyy h:mm tt} by {a.AddedBy}");
                MouseEnter += (s, e) => { hover = true; Invalidate(); };
                MouseLeave += (s, e) => { hover = false; Invalidate(); };
                DoubleClick += (s, e) => owner.Open(a);
                MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) owner.MenuFor(a).Show(this, e.Location); };
                KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) owner.Open(a); };
            }
            protected override void Dispose(bool disposing) { if (disposing) tip.Dispose(); base.Dispose(disposing); }
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                Gfx.HighQuality(g);
                g.Clear(AppTheme.SurfaceAlt);
                var card = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Gfx.RoundedRect(card, S(8)))
                {
                    using (var b = new SolidBrush(hover ? AppTheme.SurfaceHover : AppTheme.Surface)) g.FillPath(b, p);
                    using (var pen = new Pen(hover ? AppTheme.Accent : AppTheme.Border)) g.DrawPath(pen, p);
                }
                var thumbArea = new Rectangle(S(8), S(8), Width - S(16), S(92));
                var img = Thumb(a, S(116));
                if (img != null)
                {
                    var r = new Rectangle(thumbArea.X + (thumbArea.Width - img.Width) / 2, thumbArea.Y + (thumbArea.Height - img.Height) / 2, img.Width, img.Height);
                    g.DrawImage(img, r);
                }
                else
                {
                    string ext = Path.GetExtension(a.FileName).TrimStart('.').ToUpperInvariant();
                    var badge = new Rectangle(thumbArea.X + thumbArea.Width / 2 - S(28), thumbArea.Y + S(10), S(56), S(70));
                    Color c = ext == "PDF" ? AppTheme.Danger : ext.StartsWith("XLS") || ext == "CSV" ? AppTheme.Success : ext.StartsWith("DOC") ? AppTheme.Info : AppTheme.TextMuted;
                    using (var p = Gfx.RoundedRect(badge, S(6))) using (var b = new SolidBrush(ThemeManager.Mix(AppTheme.Surface, c, 0.15))) g.FillPath(b, p);
                    using var f = new Font("Segoe UI Semibold", 10F);
                    TextRenderer.DrawText(g, ext, f, badge, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                using var nf = new Font("Segoe UI", 8.5F);
                using var cf = new Font("Segoe UI Semibold", 7.75F);
                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
                TextRenderer.DrawText(g, a.FileName, nf, new Rectangle(S(6), S(104), Width - S(12), S(18)), AppTheme.TextPrimary, flags);
                string cat = a.Category.Contains('(') ? a.Category.Substring(a.Category.IndexOf('(') + 1).TrimEnd(')') : a.Category;
                TextRenderer.DrawText(g, $"{cat} · {a.SizeDisplay}", cf, new Rectangle(S(6), S(124), Width - S(12), S(18)), a.Category.StartsWith("Proof") ? AppTheme.SuccessText : AppTheme.TextMuted, flags);
            }
        }
    }
    /// <summary>Image viewer for attachments: fit / actual size, zoom with the mouse wheel, previous / next, open externally.</summary>
    public sealed class AttachmentViewer : Form
    {
        private readonly List<Attachment> images;
        private int index;
        private Image? current;
        private float zoom = 1f;
        private bool fit = true;
        private readonly Panel canvas;
        private readonly Label lblInfo;
        public static void Show(IWin32Window? owner, List<Attachment> images, Attachment start)
        {
            using var v = new AttachmentViewer(images, Math.Max(0, images.FindIndex(i => i.Id == start.Id)));
            v.ShowDialog(owner);
        }
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private AttachmentViewer(List<Attachment> images, int start)
        {
            this.images = images;
            index = start;
            Text = "Attachment viewer";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(D(1000), D(720));
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = AppTheme.Canvas;
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = D(48), BackColor = AppTheme.Surface, Padding = new Padding(D(10), D(8), D(10), 0), WrapContents = false };
            Button B(string t, Action a) { var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(0, D(30)), Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(6), 0) }; UIThemeHelper.SetButtonRole(b, "secondary"); b.Click += (s, e) => a(); return b; }
            bar.Controls.Add(B("◀ Prev", () => Step(-1)));
            bar.Controls.Add(B("Next ▶", () => Step(1)));
            bar.Controls.Add(B("Fit", () => { fit = true; canvas!.Invalidate(); }));
            bar.Controls.Add(B("100%", () => { fit = false; zoom = 1f; canvas!.Invalidate(); }));
            bar.Controls.Add(B("＋", () => Zoom(1.25f)));
            bar.Controls.Add(B("－", () => Zoom(0.8f)));
            bar.Controls.Add(B("Open externally", () => { if (images.Count > 0) AttachmentPanel.OpenExternally(images[index].StoredPath); }));
            lblInfo = new Label { AutoSize = true, ForeColor = AppTheme.TextSecondary, Margin = new Padding(D(10), D(7), 0, 0) };
            bar.Controls.Add(lblInfo);
            canvas = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = AppTheme.IsDark ? Color.FromArgb(18, 18, 20) : Color.FromArgb(40, 40, 44), AutoScroll = false };
            canvas.Paint += PaintImage;
            canvas.MouseWheel += (s, e) => Zoom(e.Delta > 0 ? 1.15f : 0.87f);
            canvas.Resize += (s, e) => canvas.Invalidate();
            Controls.Add(canvas);
            Controls.Add(bar);
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Left) Step(-1);
                else if (e.KeyCode == Keys.Right) Step(1);
                else if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) Zoom(1.25f);
                else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) Zoom(0.8f);
            };
            LoadCurrent();
            try { ThemeManager.StyleWindowChrome(this); } catch (System.Exception swx3) { ErrLog.Swallowed(swx3); }
        }
        private void Zoom(float f)
        {
            if (current == null) return;
            if (fit) zoom = FitScale();
            fit = false;
            zoom = Math.Clamp(zoom * f, 0.05f, 8f);
            canvas.Invalidate();
            UpdateInfo();
        }
        private float FitScale() => current == null ? 1f : Math.Min(1f, Math.Min((float)canvas.ClientSize.Width / current.Width, (float)canvas.ClientSize.Height / current.Height));
        private void Step(int d)
        {
            if (images.Count == 0) return;
            index = (index + d + images.Count) % images.Count;
            LoadCurrent();
        }
        private void LoadCurrent()
        {
            current?.Dispose();
            current = images.Count == 0 ? null : SecurityHelper.LoadDecoupledBitmap(images[index].StoredPath);
            fit = true;
            canvas.Invalidate();
            UpdateInfo();
        }
        private void UpdateInfo()
        {
            if (images.Count == 0) { lblInfo.Text = "No images"; return; }
            var a = images[index];
            lblInfo.Text = $"{index + 1} of {images.Count} · {a.FileName} · {a.Category} · {a.SizeDisplay}{(current != null ? $" · {current.Width}×{current.Height}" : "")} · {(fit ? "fit" : $"{zoom * 100:0}%")}";
            Text = $"{a.FileName} — {a.OwnerRef}";
        }
        private void PaintImage(object? sender, PaintEventArgs e)
        {
            if (current == null)
            {
                TextRenderer.DrawText(e.Graphics, "The image couldn't be loaded.", Font, canvas.ClientRectangle, Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            float k = fit ? FitScale() : zoom;
            int w = (int)(current.Width * k), h = (int)(current.Height * k);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(current, new Rectangle((canvas.ClientSize.Width - w) / 2, (canvas.ClientSize.Height - h) / 2, w, h));
        }
        protected override void Dispose(bool disposing) { if (disposing) current?.Dispose(); base.Dispose(disposing); }
        private sealed class DoubleBufferedPanel : Panel { public DoubleBufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; } }
    }
}
