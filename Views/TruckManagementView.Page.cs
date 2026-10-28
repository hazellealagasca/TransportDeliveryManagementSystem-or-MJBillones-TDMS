using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>List page for trucks: header, summary tiles that filter the table, and a segmented Trucks / Categories switch.</summary>
    public partial class TruckManagementView
    {
        private PageHeader? pageHeader;
        private TileStrip? truckTiles;
        private Panel? tilesGap, headerGap;
        private StatTile? tAll, tReady, tBusy, tMaint, tInactive;
        private string? truckTileFilter;
        private HashSet<int> busyTruckIds = new HashSet<int>();
        private int Dp(int v) => PageKit.D(this, v);
        private void InitListPage()
        {
            pageHeader = PageKit.Header("Trucks", "Your fleet — plate numbers, capacity, cargo type and whether each truck is free to dispatch.");
            truckTiles = new TileStrip();
            tAll = truckTiles.Add(new StatTile("All trucks", AppTheme.Accent, "") { Clickable = true, Key = "all" });
            tReady = truckTiles.Add(new StatTile("Ready to dispatch", AppTheme.Success, "active and not on a trip") { Clickable = true, Key = "ready" });
            tBusy = truckTiles.Add(new StatTile("On a dispatch", AppTheme.Info, "assigned to an open order") { Clickable = true, Key = "busy" });
            tMaint = truckTiles.Add(new StatTile("Under maintenance", AppTheme.Warning, "can't be dispatched") { Clickable = true, Key = "maint" });
            tInactive = truckTiles.Add(new StatTile("Inactive", AppTheme.TextMuted, "retired or parked") { Clickable = true, Key = "inactive" });
            truckTiles.FilterChanged += t => { truckTileFilter = t?.Key as string == "all" ? null : t?.Key as string; FilterGridData(); };
            headerGap = PageKit.Gap(this, 12);
            tilesGap = PageKit.Gap(this, 14);
            PageKit.InsertBelow(pnlSubNavTabs, pageHeader);
            PageKit.InsertBelow(pageHeader, headerGap);
            PageKit.InsertBelow(headerGap, truckTiles);
            PageKit.InsertBelow(truckTiles, tilesGap);
            // Segmented Trucks / Categories switch
            pnlSubNavTabs.BackColor = AppTheme.Canvas;
            pnlSubNavTabs.Padding = new Padding(0, Dp(2), 0, Dp(6));
            pnlSubNavTabs.Height = Dp(48);
            btnSubTabFleet.Text = "🚚  Trucks";
            btnSubTabCategories.Text = "🏷️  Categories & capacities";
            foreach (var b in new[] { btnSubTabFleet, btnSubTabCategories })
            {
                b.MinimumSize = new Size(Dp(120), Dp(36));
                b.Font = new Font("Segoe UI Semibold", 9.5F);
                b.Margin = new Padding(0, 0, Dp(6), 0);
            }
            StyleSegments(true);
            btnSubTabFleet.Click += (s, e) => { StyleSegments(true); ShowListChrome(true); };
            btnSubTabCategories.Click += (s, e) => { StyleSegments(false); ShowListChrome(false); };
            HandleCreated += (s, e) => { truckTiles.Height = Dp(96); pnlSubNavTabs.Height = Dp(48); };
        }
        private void StyleSegments(bool fleet)
        {
            void Seg(Button b, bool on)
            {
                b.BackColor = on ? AppTheme.Accent : AppTheme.Surface;
                b.ForeColor = on ? AppTheme.OnAccent : AppTheme.TextSecondary;
                b.FlatAppearance.BorderColor = on ? AppTheme.Accent : AppTheme.Border;
                b.FlatAppearance.MouseOverBackColor = on ? AppTheme.AccentHover : AppTheme.SurfaceHover;
            }
            Seg(btnSubTabFleet, fleet);
            Seg(btnSubTabCategories, !fleet);
        }
        /// <summary>Header and tiles belong to the list; hide them while the full-page form or the categories page is showing.</summary>
        private void ShowListChrome(bool show)
        {
            bool onFleet = pnlCategoriesContainer == null || !pnlCategoriesContainer.Visible;
            bool vis = show && onFleet && !(pnlForm?.Visible ?? false);
            foreach (var c in new Control?[] { pageHeader, truckTiles, tilesGap, headerGap }) if (c != null) c.Visible = vis;
            if (pnlSubNavTabs != null) pnlSubNavTabs.Visible = false;   // categories now live in the truck form
        }
        private void RefreshBusyTrucks()
        {
            try
            {
                busyTruckIds = MJdatabase.GetDeliveries()
                    .Where(d => RowMenu.Is(d.Status, "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched", "Delayed"))
                    .Select(d => d.TruckId).Where(id => id > 0).ToHashSet();
            }
            catch { busyTruckIds = new HashSet<int>(); }
        }
        private bool TruckPassesTile(Truck t) => truckTileFilter switch
        {
            "ready" => MJdatabase.IsTruckReadyStatus(t.Status) && !busyTruckIds.Contains(t.Id),
            "busy" => busyTruckIds.Contains(t.Id),
            "maint" => MJdatabase.IsTruckBlockedStatus(t.Status) && !RowMenu.Is(t.Status, "Inactive"),
            "inactive" => RowMenu.Is(t.Status, "Inactive"),
            _ => true
        };
        private void UpdateTruckTiles()
        {
            if (truckTiles == null) return;
            tAll!.Value = allTrucks.Count.ToString("N0");
            tAll.Note = $"{allTrucks.Sum(t => t.CapacityCrates):N0} crates total capacity";
            tReady!.Value = allTrucks.Count(t => MJdatabase.IsTruckReadyStatus(t.Status) && !busyTruckIds.Contains(t.Id)).ToString("N0");
            tBusy!.Value = allTrucks.Count(t => busyTruckIds.Contains(t.Id)).ToString("N0");
            int maint = allTrucks.Count(t => MJdatabase.IsTruckBlockedStatus(t.Status) && !RowMenu.Is(t.Status, "Inactive"));
            tMaint!.Value = maint.ToString("N0");
            tMaint.ValueColor = maint > 0 ? AppTheme.WarningText : null;
            tInactive!.Value = allTrucks.Count(t => RowMenu.Is(t.Status, "Inactive")).ToString("N0");
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
    }
}
