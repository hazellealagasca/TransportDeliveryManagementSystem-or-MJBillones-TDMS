using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 18: the New/Edit truck form has ONE "Vehicle &amp; capacity" section (plate, category, cargo, status, capacity) and then Remarks; no separate "Capacity" section.</summary>
public static partial class Checks
{
    [Check]
    public static string TruckForm_VehicleAndCapacityIsOneSection()
    {
        Fresh();
        return RunOnSta(() =>
        {
            var problems = new List<string>();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ClientSize = new Size(1500, 900), Opacity = 0 };
            var view = new TruckManagementView { Dock = DockStyle.Fill };
            host.Controls.Add(view); host.Show(); Pump(900);
            var body = FieldOf<Panel>(view, "pnlFormBody");
            if (body == null) return "pnlFormBody not found";

            Control? CardOf(string field)
            {
                var c = FieldOf<Control>(view, field);
                if (c == null) { problems.Add(field + " not found"); return null; }
                while (c != null && c.Parent != body) c = c.Parent;
                if (c == null) problems.Add(field + " is not inside a section card");
                return c;
            }
            var plate = CardOf("txtPlate");
            foreach (var f in new[] { "cbType", "cbCargoType", "cbStatus", "numCapacity" })
            {
                var c = CardOf(f);
                if (c != null && plate != null && c != plate) problems.Add(f + " is in a different section than the plate box");
            }
            // the card title/subtitle are painted (not Labels), so count the cards and scan the source for the titles
            int cards = body.Controls.Cast<Control>().Count(c => c is Panel p && p.AutoSize);
            if (cards != 2) problems.Add($"expected 2 section cards (Vehicle & capacity, Remarks), found {cards}");
            var src = new[] { @"..\Views\TruckManagementView.cs", @"..\..\Views\TruckManagementView.cs", @"..\..\..\Views\TruckManagementView.cs", @"..\..\..\..\Views\TruckManagementView.cs", @"C:\TDMS\Views\TruckManagementView.cs" }
                .Select(r => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, r))).FirstOrDefault(File.Exists);
            if (src != null)
            {
                var code = File.ReadAllText(src);
                if (!code.Contains("Section(\"Vehicle & capacity\"")) problems.Add("source has no 'Vehicle & capacity' section");
                if (code.Contains("Section(\"Capacity\"")) problems.Add("source still has a separate 'Capacity' section");
                if (code.Contains("Section(\"Vehicle\"")) problems.Add("source still has the old 'Vehicle' section");
            }
            host.Close();
            return string.Join(" | ", problems);
        });
    }
}

