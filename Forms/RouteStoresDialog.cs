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
    /// Picks a route's stores from the client's Stores &amp; branches list and sets the delivery order.
    /// Only stores that exist in Master Data can be added; new stores are added on the client first.
    /// </summary>
    public class RouteStoresDialog : Form
    {
        public List<string> Places { get; private set; }
        private readonly string client;
        private readonly List<ClientStore> stores;
        private readonly List<DeliveryRoute> otherRoutes;
        private TextBox txtFilter = null!;
        private ListBox lstAvailable = null!;
        private ListBox lstChosen = null!;
        private Label lblCount = null!;
        private Button openClient = null!;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);





        private sealed class StoreItem
        {
            public ClientStore Store { get; init; } = new ClientStore();
            public string Note { get; init; } = "";
            public override string ToString()
            {
                string label = string.IsNullOrWhiteSpace(Store.StoreCode) ? Store.StoreName : $"{Store.StoreCode} - {Store.StoreName}";
                return Note.Length == 0 ? label : $"{label}   ({Note})";
            }
        }





        public RouteStoresDialog(string clientName, string routeLabel, List<string> currentPlaces, List<DeliveryRoute> otherRoutesOfClient)
        {
            client = clientName;
            Places = new List<string>(currentPlaces);
            otherRoutes = otherRoutesOfClient ?? new List<DeliveryRoute>();
            try { stores = MJdatabase.GetClientStores(clientName); } catch { stores = new List<ClientStore>(); }
            Build(routeLabel);
            UIThemeHelper.ApplyTheme(this);
            UIThemeHelper.SetButtonRole(openClient, "secondary");
            FillChosen();
            FillAvailable();
        }










        private void Build(string routeLabel)
        {
            Text = "Route stores";
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(D(820), D(560));
            MinimumSize = new Size(D(720), D(480));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = AppTheme.Canvas;





            var header = new Panel { Dock = DockStyle.Top, Height = D(66), BackColor = AppTheme.Surface, Padding = new Padding(D(20), D(10), D(20), D(8)) };
            header.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(22), Text = $"Pick the stores the truck delivers to, in order. Only {client}'s stores in Master Data can be picked.", Font = new Font("Segoe UI", 9F), ForeColor = AppTheme.TextMuted, AutoEllipsis = true, UseMnemonic = false });
            header.Controls.Add(new Label { Dock = DockStyle.Top, Height = D(28), Text = $"Stores on {routeLabel}", Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.TextPrimary, AutoEllipsis = true, UseMnemonic = false });





            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, Padding = new Padding(D(20), D(12), D(20), D(8)), BackColor = AppTheme.Canvas };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));





            Label Cap(string t) => new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(0, 0, 0, D(6)), UseMnemonic = false };
            body.Controls.Add(Cap($"{client}'s stores"), 0, 0);
            lblCount = Cap("Delivery order");
            body.Controls.Add(lblCount, 2, 0);
            txtFilter = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "🔍 Search store code or name…", Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 0, 0, D(6)) };
            txtFilter.TextChanged += (s, e) => FillAvailable();
            body.Controls.Add(txtFilter, 0, 1);
            var hint = new Label { Text = "Use ▲ ▼ to set the order the truck delivers in.", AutoSize = true, Font = new Font("Segoe UI", 8.75F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, D(4), 0, D(6)) };
            body.Controls.Add(hint, 2, 1);





            lstAvailable = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), IntegralHeight = false, SelectionMode = SelectionMode.MultiExtended, BorderStyle = BorderStyle.FixedSingle };
            lstAvailable.DoubleClick += (s, e) => AddSelected();
            lstChosen = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), IntegralHeight = false, BorderStyle = BorderStyle.FixedSingle };
            lstChosen.DoubleClick += (s, e) => RemoveSelected();
            body.Controls.Add(lstAvailable, 0, 2);
            body.Controls.Add(lstChosen, 2, 2);





            var mid = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Padding = new Padding(D(10), D(30), D(10), 0), BackColor = AppTheme.Canvas };
            Button Btn(string t, string role, Action a)
            {
                var b = new Button { Text = t, Width = D(120), Height = D(34), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, D(8)), Font = new Font("Segoe UI Semibold", 9F), UseMnemonic = false };
                b.FlatAppearance.BorderSize = 0;
                UIThemeHelper.SetButtonRole(b, role);
                b.Click += (s, e) => a();
                return b;
            }
            mid.Controls.Add(Btn("Add  ➜", "primary", AddSelected));
            mid.Controls.Add(Btn("⬅  Remove", "secondary", RemoveSelected));
            mid.Controls.Add(Btn("▲  Up", "secondary", () => MoveItem(-1)));
            mid.Controls.Add(Btn("▼  Down", "secondary", () => MoveItem(1)));
            body.Controls.Add(mid, 1, 2);
            body.SetRowSpan(mid, 1);





            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = D(58), FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(D(20), D(10), D(20), D(10)), BackColor = AppTheme.Surface };
            var ok = new Button { Text = "Use these stores", AutoSize = true, MinimumSize = new Size(D(150), D(36)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 9.5F) };
            ok.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(ok, "primary");
            ok.Click += (s, e) =>
            {
                Places = lstChosen.Items.Cast<object>().Select(o => o.ToString() ?? "").Where(x => x.Length > 0).ToList();
                DialogResult = DialogResult.OK;
            };
            var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(D(100), D(36)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel, Margin = new Padding(0, 0, D(8), 0) };
            cancel.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(cancel, "secondary");
            openClient = new Button { Text = "Open client profile", AutoSize = true, MinimumSize = new Size(0, D(36)), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, D(24), 0) };
            openClient.FlatAppearance.BorderSize = 0;
            UIThemeHelper.SetButtonRole(openClient, "secondary");
            new ToolTip().SetToolTip(openClient, "New stores are created on the client's profile (Stores & branches), then picked here.");
            openClient.Click += (s, e) => { DialogResult = DialogResult.Cancel; BeginInvoke(new Action(() => MainForm.Instance?.OpenClientProfile(client))); };
            footer.Controls.Add(ok);
            footer.Controls.Add(cancel);
            footer.Controls.Add(openClient);
            AcceptButton = ok;
            CancelButton = cancel;





            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
        }





        private void FillAvailable()
        {
            string q = (txtFilter.Text ?? "").Trim();
            var chosen = lstChosen?.Items.Cast<object>().Select(o => o.ToString() ?? "").ToList() ?? Places;
            lstAvailable.BeginUpdate();
            lstAvailable.Items.Clear();
            foreach (var st in stores.OrderBy(x => x.StoreName, StringComparer.OrdinalIgnoreCase))
            {
                if (chosen.Any(p => MJdatabase.PlaceIsStore(p, st))) continue;
                if (q.Length > 0 && !(st.StoreName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) && !(st.StoreCode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                var on = otherRoutes.FirstOrDefault(r => r.PlacesList.Any(p => MJdatabase.PlaceIsStore(p, st)));
                lstAvailable.Items.Add(new StoreItem { Store = st, Note = on == null ? "" : $"on {on.RouteCode}" });
            }
            lstAvailable.EndUpdate();
            if (stores.Count == 0)
                lstAvailable.Items.Add("(This client has no stores yet — click Open client to add them)");
        }





        private void FillChosen()
        {
            lstChosen.BeginUpdate();
            lstChosen.Items.Clear();
            foreach (var p in Places) lstChosen.Items.Add(p);
            lstChosen.EndUpdate();
            UpdateCount();
        }





        private void UpdateCount() => lblCount.Text = lstChosen.Items.Count == 0 ? "Delivery order (none yet)" : $"Delivery order ({lstChosen.Items.Count} store{(lstChosen.Items.Count == 1 ? "" : "s")})";





        private void AddSelected()
        {
            var picks = lstAvailable.SelectedItems.OfType<StoreItem>().ToList();
            if (picks.Count == 0) return;
            foreach (var p in picks) lstChosen.Items.Add(MJdatabase.PlaceFor(p.Store));
            lstChosen.SelectedIndex = lstChosen.Items.Count - 1;
            FillAvailable();
            UpdateCount();
        }





        private void RemoveSelected()
        {
            int i = lstChosen.SelectedIndex;
            if (i < 0) return;
            lstChosen.Items.RemoveAt(i);
            if (lstChosen.Items.Count > 0) lstChosen.SelectedIndex = Math.Min(i, lstChosen.Items.Count - 1);
            FillAvailable();
            UpdateCount();
        }





        private void MoveItem(int dir)
        {
            int i = lstChosen.SelectedIndex, j = i + dir;
            if (i < 0 || j < 0 || j >= lstChosen.Items.Count) return;
            var item = lstChosen.Items[i];
            lstChosen.Items.RemoveAt(i);
            lstChosen.Items.Insert(j, item);
            lstChosen.SelectedIndex = j;
        }





        protected override void Dispose(bool disposing)
        {
            DarkModeKit.RunWithoutFalseCrossThreadCheck(() => base.Dispose(disposing), nameof(RouteStoresDialog));
        }
    }
}
