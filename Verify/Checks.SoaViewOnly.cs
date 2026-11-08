using System.Reflection;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>Nov 8: a statement that is paid in full (or cancelled) opens read-only with its history instead of a "can't be edited" message.</summary>
public static partial class Checks
{
    [Check]
    public static string PaidSoa_Opens_ReadOnly_WithHistory()
    {
        Fresh(); RunBackfill();
        int d = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d);
        int paidSoa = SoaWithInvoice(d, "SI-9601", 1000m, out int invId);
        Exec("UPDATE StatementsOfAccount SET Status='Paid' WHERE Id=@i", ("@i", paidSoa));
        Exec("UPDATE Invoices SET Status='Paid' WHERE Id=@i", ("@i", invId));
        Seed.Receipt(invId, 1000m, "7001", paidSoa);
        int d2 = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(d2);
        int openSoa = SoaWithInvoice(d2, "SI-9602", 500m, out _);
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var view = new StatementOfAccountView();
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), Size = new Size(1500, 900), Opacity = 0 };
            view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show();
            Pump(800);
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            T Field<T>(string n) where T : class => (T)typeof(StatementOfAccountView).GetField(n, F)!.GetValue(view)!;
            bool Flag(string n) => (bool)typeof(StatementOfAccountView).GetField(n, F)!.GetValue(view)!;
            void Open(int id) => typeof(StatementOfAccountView).GetMethod("OpenEditStatement", F)!.Invoke(view, new object[] { MJdatabase.GetStatementOfAccountById(id)! });
            var bad = new List<string>();
            int before = boxes.Boxes.Count;
            Open(paidSoa);
            Pump(900);
            var form = Field<Panel>("pnlForm");
            var save = Field<Button>("btnSaveSoa");
            var items = Field<DataGridView>("gridItems");
            var history = Field<DataGridView>("gridSoaHistory");
            if (!form.Visible) bad.Add("a paid statement did not open");
            if (!Flag("soaViewOnly")) bad.Add("not in view-only mode");
            if (save.Visible) bad.Add("Save is still shown");
            if (!items.ReadOnly) bad.Add("the trip lines can still be edited");
            if (!Field<ComboBox>("cbClient").Enabled == false) bad.Add("the client box is still enabled");
            if (history.Rows.Count == 0 || !history.Rows.Cast<DataGridViewRow>().Any(r => (r.Cells[1].Value?.ToString() ?? "").StartsWith("Payment received"))) bad.Add("the history does not list the payment");
            if (boxes.Boxes.Count > before) bad.Add("a message box appeared instead of the view");
            // Leaving and opening one that can be edited puts everything back.
            typeof(StatementOfAccountView).GetMethod("SetFormVisibility", F)!.Invoke(view, new object[] { false });
            Open(openSoa);
            Pump(900);
            if (Flag("soaViewOnly")) bad.Add("the next statement is still view-only");
            if (!save.Visible) bad.Add("Save is hidden on a statement that can be edited");
            if (items.ReadOnly) bad.Add("the lines stayed read-only");
            if (!Field<ComboBox>("cbClient").Enabled) bad.Add("the client box stayed disabled");
            host.Close();
            return string.Join("; ", bad);
        });
    }
}
