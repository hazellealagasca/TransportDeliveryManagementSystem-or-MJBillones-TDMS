using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// A one-line summary of the client picked on an order form (TIN, contact, routes, stores) with a link to the
    /// client's profile in Master Data, or a one-click "add to clients" when the name isn't registered yet.
    /// </summary>
    public sealed class ClientInfoStrip : Panel
    {
        private readonly Label lbl;
        private readonly LinkLabel link;
        private readonly System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer { Interval = 250 };
        private string pendingName = "";
        private string shownName = "";
        private bool registered;
        /// <summary>Raised after the user adds the client to Master Data from the strip.</summary>
        public event Action<string>? ClientRegistered;
        private int D(int v) => (int)Math.Round(v * DeviceDpi / 96f);
        public ClientInfoStrip()
        {
            Dock = DockStyle.Top;
            Height = D(30);
            Padding = new Padding(D(10), 0, D(10), 0);
            Margin = new Padding(0, 0, 0, D(6));
            BackColor = AppTheme.SurfaceAlt;
            Visible = false;
            lbl = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F),
                ForeColor = AppTheme.TextSecondary,
                UseMnemonic = false,
                BackColor = Color.Transparent
            };
            link = new LinkLabel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI Semibold", 9F),
                LinkColor = AppTheme.Accent,
                ActiveLinkColor = AppTheme.AccentHover,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Padding = new Padding(0, D(6), 0, 0),
                BackColor = Color.Transparent
            };
            link.LinkClicked += (s, e) => OnLink();
            Controls.Add(lbl);
            Controls.Add(link);
            debounce.Tick += (s, e) => { debounce.Stop(); Render(pendingName); };
            Disposed += (s, e) => debounce.Dispose();
        }
        /// <summary>Shows the summary for a client name (updates are batched while the user types).</summary>
        public void ShowFor(string? clientName)
        {
            pendingName = (clientName ?? "").Trim();
            debounce.Stop();
            debounce.Start();
        }
        public void ShowNow(string? clientName)
        {
            debounce.Stop();
            Render((clientName ?? "").Trim());
        }
        private void Render(string name)
        {
            if (IsDisposed) return;
            shownName = name;
            if (name.Length == 0)
            {
                Visible = false;
                return;
            }
            try
            {
                var cl = MJdatabase.GetClientByCompanyName(name);
                registered = cl != null;
                if (cl == null)
                {
                    lbl.Text = $"⚠  \"{name}\" isn't in Master Data → Clients yet, so it has no routes or stores.";
                    lbl.ForeColor = AppTheme.WarningText;
                    BackColor = AppTheme.WarningSoft;
                    link.Text = "＋ Add to clients";
                }
                else
                {
                    var routes = MJdatabase.GetDeliveryRoutesByClient(cl.CompanyName, activeOnly: true)
                        .Count(r => string.Equals((r.ClientCompanyName ?? "").Trim(), cl.CompanyName.Trim(), StringComparison.OrdinalIgnoreCase));
                    int stores = MJdatabase.GetClientStores(cl.CompanyName).Count;
                    var parts = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrWhiteSpace(cl.TIN)) parts.Add($"TIN {cl.TIN}");
                    string contact = string.Join(" ", new[] { cl.ContactPerson, cl.ContactNumber }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (contact.Length > 0) parts.Add($"📞 {contact}");
                    parts.Add($"{routes} route{(routes == 1 ? "" : "s")}");
                    parts.Add($"{stores} store{(stores == 1 ? "" : "s")}");
                    if (!string.IsNullOrWhiteSpace(cl.Address)) parts.Add(cl.Address.Replace("\r", " ").Replace("\n", " "));
                    lbl.Text = "🏢  " + string.Join("   ·   ", parts);
                    lbl.ForeColor = AppTheme.TextSecondary;
                    BackColor = AppTheme.SurfaceAlt;
                    link.Text = "Open client ›";
                }
                Visible = true;
            }
            catch
            {
                Visible = false;
            }
        }
        private void OnLink()
        {
            if (shownName.Length == 0) return;
            if (!registered)
            {
                try
                {
                    MJdatabase.EnsureClientExists(shownName);
                    ToastHelper.ShowToast(FindForm() ?? (Control)this, $"\"{shownName}\" added to clients. Open it to add TIN, contact and stores.", ToastType.Success);
                    ClientRegistered?.Invoke(shownName);
                    Render(shownName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(FindForm(), "Couldn't add the client: " + ex.Message, "Clients", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }
            MainForm.Instance?.OpenClientProfile(shownName);
        }
    }
}
