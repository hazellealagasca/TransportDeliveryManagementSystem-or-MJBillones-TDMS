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
    /// Registers a store for the selected client on one of its routes (store code, store name, route), with the rules
    /// checked before saving: all three required, code upper case (letters, digits, hyphens), code not already used by
    /// any client, the same name not already on that route. Store and route are saved together.
    /// </summary>
    public sealed class RegisterStoreDialog : Form
    {
        public ClientStore? RegisteredStore { get; private set; }
        private readonly string client;
        private readonly ComboBox cbRoute = new ComboBox();
        private readonly TextBox txtCode = new TextBox(), txtName = new TextBox(), txtAddress = new TextBox();
        private readonly Label lblProblem = new Label();
        private readonly Button btnSave = new Button(), btnCancel = new Button(), btnNewRoute = new Button();
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        public RegisterStoreDialog(string clientName, string? code, string? name, string? defaultRouteLabel)
        {
            client = MJdatabase.CleanStoreText(clientName);
            Text = "Add store details";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 10F);
            BackColor = AppTheme.Surface;
            ForeColor = AppTheme.TextPrimary;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(D(22), D(18), D(22), D(16));
            KeyPreview = true;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = AppTheme.Surface };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(480)));
            Controls.Add(root);
            Label Cap(string t) => new Label { Text = t, AutoSize = true, ForeColor = AppTheme.TextSecondary, Font = new Font("Segoe UI Semibold", 9.5F), Margin = new Padding(0, D(10), 0, D(4)), UseMnemonic = false };
            void Field(Control c) { c.Dock = DockStyle.Fill; c.Margin = new Padding(0, 0, 0, D(2)); c.Font = new Font("Segoe UI", 10.5F); c.BackColor = AppTheme.InputBack; c.ForeColor = AppTheme.TextPrimary; }

            root.Controls.Add(new Label { Text = "Register a new store", AutoSize = true, Font = new Font("Segoe UI Semibold", 14F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(2)), UseMnemonic = false });
            root.Controls.Add(new Label { Text = "It is added to this client's stores and to the route's store list, then filled into your table.", AutoSize = true, MaximumSize = new Size(D(480), 0), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, 0, 0, D(4)), UseMnemonic = false });

            root.Controls.Add(Cap("Client company"));
            var lblClient = new Label { Text = client.Length > 0 ? client : "(no client selected)", AutoSize = true, Font = new Font("Segoe UI Semibold", 10.5F), ForeColor = AppTheme.TextPrimary, BackColor = AppTheme.SurfaceAlt, Padding = new Padding(D(8), D(6), D(8), D(6)), MinimumSize = new Size(D(480), 0), UseMnemonic = false, Margin = new Padding(0) };
            root.Controls.Add(lblClient);

            root.Controls.Add(Cap("Route *"));
            var routeRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0), BackColor = AppTheme.Surface };
            routeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            routeRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cbRoute.DropDownStyle = ComboBoxStyle.DropDownList;
            Field(cbRoute);
            cbRoute.Margin = new Padding(0, 0, D(8), 0);
            StyleButton(btnNewRoute, "+ New route", "secondary");
            btnNewRoute.Margin = new Padding(0);
            btnNewRoute.Click += (s, e) => AddRoute();
            routeRow.Controls.Add(cbRoute, 0, 0);
            routeRow.Controls.Add(btnNewRoute, 1, 0);
            root.Controls.Add(routeRow);

            root.Controls.Add(Cap($"Store code (optional: left blank, one is made and shows as {MJdatabase.NoCodeText}; letters, numbers, hyphens; up to {MJdatabase.StoreCodeMaxLength})"));
            Field(txtCode);
            txtCode.CharacterCasing = CharacterCasing.Upper;
            txtCode.MaxLength = MJdatabase.StoreCodeMaxLength;
            txtCode.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != '-') e.Handled = true; };
            root.Controls.Add(txtCode);

            root.Controls.Add(Cap("Store name *"));
            Field(txtName);
            txtName.MaxLength = MJdatabase.StoreNameMaxLength;
            root.Controls.Add(txtName);

            root.Controls.Add(Cap("Store address (optional)"));
            Field(txtAddress);
            txtAddress.MaxLength = 200;
            root.Controls.Add(txtAddress);

            lblProblem.AutoSize = true;
            lblProblem.MaximumSize = new Size(D(480), 0);
            lblProblem.Font = new Font("Segoe UI Semibold", 9.5F);
            lblProblem.ForeColor = AppTheme.DangerText;
            lblProblem.Margin = new Padding(0, D(10), 0, 0);
            lblProblem.UseMnemonic = false;
            root.Controls.Add(lblProblem);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, D(14), 0, 0), BackColor = AppTheme.Surface };
            StyleButton(btnSave, "Save store", "primary");
            StyleButton(btnCancel, "Cancel", "secondary");
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            root.Controls.Add(buttons);
            CancelButton = btnCancel;
            AcceptButton = btnSave;

            // What was typed in the table goes into the matching box: a value that looks like a code goes to the code.
            string c = MJdatabase.CleanStoreCode(code), n = MJdatabase.CleanStoreText(name);
            txtCode.Text = c;
            txtName.Text = n;
            LoadRoutes(defaultRouteLabel);
            txtCode.TextChanged += (s, e) => lblProblem.Text = "";
            txtName.TextChanged += (s, e) => lblProblem.Text = "";
            txtAddress.TextChanged += (s, e) => lblProblem.Text = "";
            cbRoute.SelectedIndexChanged += (s, e) => lblProblem.Text = "";
            Shown += (s, e) =>
            {
                Control first = cbRoute.SelectedIndex < 0 ? cbRoute : txtCode.Text.Length == 0 ? txtCode : txtName.Text.Length == 0 ? txtName : (Control)btnSave;
                first.Focus();
            };
            UIThemeHelper.ApplyTheme(this);
        }

        private void StyleButton(Button b, string text, string role)
        {
            b.Text = text;
            b.AutoSize = true;
            b.MinimumSize = new Size(D(104), D(36));
            b.Padding = new Padding(D(10), 0, D(10), 0);
            b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI Semibold", 10F);
            b.Cursor = Cursors.Hand;
            b.Margin = new Padding(D(6), 0, 0, 0);
            b.UseMnemonic = false;
            UIThemeHelper.SetButtonRole(b, role);
        }

        private void LoadRoutes(string? preferLabel)
        {
            cbRoute.Items.Clear();
            List<DeliveryRoute> routes;
            try
            {
                routes = MJdatabase.GetDeliveryRoutesByClient(client, activeOnly: true)
                    .Where(r => StoreDirectory.Same(r.ClientCompanyName, client))
                    .OrderBy(r => r.RouteCode, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception ex) { ErrLog.Swallowed(ex); routes = new List<DeliveryRoute>(); }
            foreach (var r in routes) cbRoute.Items.Add(MJdatabase.RouteLabel(r));
            string want = MJdatabase.CleanStoreText(preferLabel);
            int idx = want.Length == 0 ? -1 : cbRoute.Items.Cast<object>().ToList().FindIndex(o => StoreDirectory.Same(o?.ToString(), want));
            if (idx < 0 && cbRoute.Items.Count == 1) idx = 0;
            cbRoute.SelectedIndex = idx;
            if (cbRoute.Items.Count == 0)
                lblProblem.Text = $"{(client.Length > 0 ? client : "This client")} has no routes yet. Click \"+ New route\" to add one first.";
        }

        private void AddRoute()
        {
            using var dlg = new QuickAddRouteDialog(client);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.CreatedRoute == null) return;
            LoadRoutes(MJdatabase.RouteLabel(dlg.CreatedRoute));
        }

        private void Save()
        {
            txtCode.Text = MJdatabase.CleanStoreCode(txtCode.Text);
            txtName.Text = MJdatabase.CleanStoreText(txtName.Text);
            string route = cbRoute.SelectedItem?.ToString() ?? "";
            string? problem;
            try { problem = MJdatabase.CheckNewStore(client, txtCode.Text, txtName.Text, route, out _); }
            catch (Exception ex) { ErrLog.Error("RegisterStoreDialog", ex); problem = "Couldn't check the store: " + ex.Message; }
            if (problem != null) { ShowProblem(problem); return; }
            try
            {
                RegisteredStore = MJdatabase.RegisterClientStore(client, txtCode.Text, txtName.Text, route, txtAddress.Text);
            }
            catch (InvalidOperationException ex) { ShowProblem(ex.Message); return; }
            catch (Exception ex) { ErrLog.Error("RegisterStoreDialog", ex); ShowProblem("The store was not saved: " + ex.Message); return; }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowProblem(string problem)
        {
            lblProblem.Text = "⚠ " + problem;
            if (problem.IndexOf("route", StringComparison.OrdinalIgnoreCase) >= 0 && cbRoute.SelectedIndex < 0) cbRoute.Focus();
            else if (problem.IndexOf("code", StringComparison.OrdinalIgnoreCase) >= 0) { txtCode.Focus(); txtCode.SelectAll(); }
            else if (problem.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0) { txtName.Focus(); txtName.SelectAll(); }
        }
    }

    /// <summary>
    /// The warning shown when a store typed into a store table is not registered under the selected client:
    /// "Add Store Details First" (opens <see cref="RegisterStoreDialog"/>) or "Cancel".
    /// </summary>
    public sealed class UnregisteredStorePrompt : Form
    {
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        private UnregisteredStorePrompt(string message, bool canRegister)
        {
            Text = "Store not registered";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Segoe UI", 10F);
            BackColor = AppTheme.Surface;
            ForeColor = AppTheme.TextPrimary;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(D(22), D(18), D(22), D(16));
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, BackColor = AppTheme.Surface };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, D(460)));
            Controls.Add(root);
            root.Controls.Add(new Label { Text = "⚠  Store not registered", AutoSize = true, Font = new Font("Segoe UI Semibold", 13F), ForeColor = AppTheme.WarningText, Margin = new Padding(0, 0, 0, D(8)), UseMnemonic = false });
            root.Controls.Add(new Label { Text = message, AutoSize = true, MaximumSize = new Size(D(460), 0), Font = new Font("Segoe UI", 10.5F), ForeColor = AppTheme.TextPrimary, Margin = new Padding(0, 0, 0, D(6)), UseMnemonic = false });
            root.Controls.Add(new Label
            {
                Text = canRegister ? "It can't be added to the table until it is registered. Add its details now?" : "It can't be added to the table. Use one of this client's stores.",
                AutoSize = true, MaximumSize = new Size(D(460), 0), Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted, Margin = new Padding(0, 0, 0, D(4)), UseMnemonic = false
            });
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, D(14), 0, 0), BackColor = AppTheme.Surface };
            Button B(string t, string role, DialogResult r)
            {
                var b = new Button { Text = t, AutoSize = true, MinimumSize = new Size(D(104), D(36)), Padding = new Padding(D(10), 0, D(10), 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 10F), Cursor = Cursors.Hand, Margin = new Padding(D(6), 0, 0, 0), UseMnemonic = false, DialogResult = r };
                UIThemeHelper.SetButtonRole(b, role);
                return b;
            }
            var cancel = B(canRegister ? "Cancel" : "OK", canRegister ? "secondary" : "primary", DialogResult.Cancel);
            if (canRegister)
            {
                var add = B("Add Store Details First", "primary", DialogResult.Yes);
                buttons.Controls.Add(add);
                AcceptButton = add;
            }
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons);
            CancelButton = cancel;
            UIThemeHelper.ApplyTheme(this);
        }

        /// <summary>Shows the warning. Returns true when "Add Store Details First" was chosen.</summary>
        public static bool Ask(IWin32Window? owner, string message, bool canRegister)
        {
            using var dlg = new UnregisteredStorePrompt(message, canRegister);
            return dlg.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
