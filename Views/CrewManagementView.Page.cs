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
    /// <summary>List page for crew: header and summary tiles that filter the table.</summary>
    public partial class CrewManagementView
    {
        private PageHeader? pageHeader;
        private TileStrip? crewTiles;
        private Panel? tilesGap, headerGap;
        private StatTile? cAll, cDrivers, cAides, cBusy, cInactive;
        private string? crewTileFilter;
        private HashSet<int> busyCrewIds = new HashSet<int>();
        private void InitListPage()
        {
            pageHeader = PageKit.Header("Crew", "Drivers and aides — contact details, license, pay per trip and who is free for the next dispatch.");
            pageHeader.AddActions(PageKit.Action("📋  Trip history", "secondary", (s, e) => OnViewCrewHistoryRequested?.Invoke(selectedCrew?.Id ?? 0)));
            crewTiles = new TileStrip();
            cAll = crewTiles.Add(new StatTile("All crew", AppTheme.Accent, "") { Clickable = true, Key = "all" });
            cDrivers = crewTiles.Add(new StatTile("Drivers", AppTheme.Info, "") { Clickable = true, Key = "driver" });
            cAides = crewTiles.Add(new StatTile("Aides / helpers", AppTheme.Success, "") { Clickable = true, Key = "aide" });
            cBusy = crewTiles.Add(new StatTile("On a dispatch", AppTheme.Warning, "assigned to an open order") { Clickable = true, Key = "busy" });
            cInactive = crewTiles.Add(new StatTile("Inactive", AppTheme.TextMuted, "not available for trips") { Clickable = true, Key = "inactive" });
            crewTiles.FilterChanged += t => { crewTileFilter = t?.Key as string == "all" ? null : t?.Key as string; FilterGridData(); };
            headerGap = PageKit.Gap(this, 12);
            tilesGap = PageKit.Gap(this, 14);
            PageKit.InsertAtTop(this, tilesGap);
            PageKit.InsertAtTop(this, crewTiles);
            PageKit.InsertAtTop(this, headerGap);
            PageKit.InsertAtTop(this, pageHeader);
            HandleCreated += (s, e) => crewTiles.Height = PageKit.D(this, 96);
        }
        private void ShowListChrome(bool show)
        {
            foreach (var c in new Control?[] { pageHeader, crewTiles, tilesGap, headerGap }) if (c != null) c.Visible = show;
        }
        private void RefreshBusyCrew()
        {
            try
            {
                var set = new HashSet<int>();
                foreach (var d in MJdatabase.GetDeliveries().Where(d => RowMenu.Is(d.Status, "Scheduled", "Pending", "In Transit", "Rescheduled", "Dispatched", "Delayed")))
                    foreach (var (id, _) in MJdatabase.GetDeliveryCrewIds(d)) set.Add(id);
                busyCrewIds = set;
            }
            catch { busyCrewIds = new HashSet<int>(); }
        }
        private static bool IsDriver(Crew c) => (c.Role ?? "").IndexOf("Driver", StringComparison.OrdinalIgnoreCase) >= 0;
        private bool CrewPassesTile(Crew c) => crewTileFilter switch
        {
            "driver" => IsDriver(c),
            "aide" => !IsDriver(c),
            "busy" => busyCrewIds.Contains(c.Id),
            "inactive" => !c.IsActive,
            _ => true
        };
        private void UpdateCrewTiles()
        {
            if (crewTiles == null) return;
            cAll!.Value = allCrews.Count.ToString("N0");
            cAll.Note = $"{allCrews.Count(c => c.IsActive):N0} active";
            var drivers = allCrews.Where(IsDriver).ToList();
            cDrivers!.Value = drivers.Count.ToString("N0");
            cDrivers.Note = $"{drivers.Count(c => c.IsActive && !busyCrewIds.Contains(c.Id)):N0} free right now";
            var aides = allCrews.Where(c => !IsDriver(c)).ToList();
            cAides!.Value = aides.Count.ToString("N0");
            cAides.Note = $"{aides.Count(c => c.IsActive && !busyCrewIds.Contains(c.Id)):N0} free right now";
            cBusy!.Value = allCrews.Count(c => busyCrewIds.Contains(c.Id)).ToString("N0");
            cInactive!.Value = allCrews.Count(c => !c.IsActive).ToString("N0");
        }
        public void FilterByText(string text)
        {
            if (pnlForm.Visible) SetFormVisibility(false);
            RowMenu.Search(txtGridSearch, text);
        }
    }
}
