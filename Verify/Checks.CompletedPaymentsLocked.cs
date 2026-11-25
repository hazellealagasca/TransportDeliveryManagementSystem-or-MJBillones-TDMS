using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 24: completed work is locked and the working tabs show open work only.
/// Service Invoice No. has no suggestion list (a used number is only refused); "Completed Delivery Trip" is hidden; a Paid invoice and an issued receipt
/// can't be edited; Service Invoices lists open work only and Collection Receipts lists issued receipts read-only (Nov 25); Finance History lists the completed ones newest first and the cancelled /
/// voided ones under Voided.
/// </summary>
public static partial class Checks
{
    private const BindingFlags CpF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static (BillingView view, Form host) CpBilling()
    {
        var view = (BillingView)Activator.CreateInstance(typeof(BillingView), nonPublic: true)!;
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900), FormBorderStyle = FormBorderStyle.None };
        view.Dock = DockStyle.Fill; host.Controls.Add(view); host.Show(); Pump(800);
        return (view, host);
    }
    /// <summary>Handles the app's own themed message boxes (forms with a title): clicks Yes on a confirmation when <c>yes</c> is set, otherwise closes them.</summary>
    private sealed class CpDialogCloser : IDisposable
    {
        private readonly System.Threading.Timer timer;
        private readonly bool yes;
        private static IEnumerable<Control> AllControls(Control c) => c.Controls.Cast<Control>().SelectMany(x => new[] { x }.Concat(AllControls(x)));
        public CpDialogCloser(bool yes = false)
        {
            this.yes = yes;
            timer = new System.Threading.Timer(_ =>
            {
                try
                {
                    foreach (var f in Application.OpenForms.Cast<Form>().ToList())
                    {
                        if (!f.Visible || string.IsNullOrEmpty(f.Text) || !f.IsHandleCreated) continue;
                        f.BeginInvoke(new Action(() =>
                        {
                            var y = AllControls(f).OfType<Button>().FirstOrDefault(b => b.Text.Replace("&", "").Trim().StartsWith("Yes", StringComparison.OrdinalIgnoreCase));
                            if (this.yes && y != null) y.PerformClick(); else f.Close();
                        }));
                    }
                }
                catch { }
            }, null, 500, 500);
        }
        public void Dispose() => timer.Dispose();
    }
    private static T CpField<T>(object view, string name) where T : class => (T)view.GetType().GetField(name, CpF)!.GetValue(view)!;
    private static List<Invoice> CpListed(BillingView view) => (CpField<DataGridView>(view, "gridInvoices").DataSource as List<Invoice>) ?? new List<Invoice>();

    [Check]
    public static string CompletedPayments_InvoiceNumberBox_HasNoLookup_AndUsedNumberIsRefused()
    {
        Fresh(); RunBackfill();
        int inv = Seed.Invoice(Seed.Delivery(), 500m, PayStatus.Unpaid);
        string used = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", inv));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser();
            var (view, host) = CpBilling();
            var bad = new List<string>();
            if (typeof(BillingView).GetFields(CpF).Any(f => f.FieldType == typeof(RichLookup))) bad.Add("the invoice form still holds a RichLookup (suggestion list)");
            if (typeof(BillingView).GetMethod("InvoiceSuggestions", CpF) != null) bad.Add("InvoiceSuggestions still exists");
            if (typeof(BillingView).GetMethod("HandleInvoiceNumberInput", CpF) != null) bad.Add("typing a number still opens an invoice (HandleInvoiceNumberInput)");
            typeof(BillingView).GetMethod("OpenNewRecordForm", CpF)!.Invoke(view, null); Pump(300);
            var tb = CpField<TextBox>(view, "txtInvoiceNumber");
            string header = CpField<Label>(view, "lblFormHeader").Text;
            tb.Focus(); tb.Text = used.Substring(0, Math.Max(1, used.Length - 1)); Pump(300);
            tb.Text = used; Pump(500);
            if (Application.OpenForms.OfType<RichLookup>().Any(f => f.Visible)) bad.Add("a suggestion list opened while typing the number");
            if (typeof(BillingView).GetField("selectedInvoice", CpF)!.GetValue(view) != null) bad.Add("typing an existing number opened that invoice");
            if (CpField<Label>(view, "lblFormHeader").Text != header) bad.Add("the form header changed to '" + CpField<Label>(view, "lblFormHeader").Text + "' when the number was typed");
            if (CpField<Button>(view, "btnUpdate").Visible) bad.Add("Update appeared for a typed number");
            typeof(BillingView).GetMethod("UpdateBillGuide", CpF)!.Invoke(view, null);
            var checklist = CpField<ValidationChecklist>(view, "billChecklist");
            if (!checklist.Checks.Any(c => !c.Passed && c.Text.Contains(used))) bad.Add("the checklist does not flag the used number: " + string.Join(" | ", checklist.Checks.Where(c => !c.Passed).Select(c => c.Text)));
            if (MJdatabase.CheckSiNumber(used, 0) == null) bad.Add("CheckSiNumber accepts a used number");
            long before = Long("SELECT COUNT(*) FROM Invoices");
            typeof(BillingView).GetMethod("BtnSave_Click", CpF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (Long("SELECT COUNT(*) FROM Invoices") != before) bad.Add("Issue saved an invoice with a used number");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_TripPickerHidden_AndStatementInvoiceStillSaves()
    {
        Fresh(); RunBackfill();
        int a = Seed.Delivery(client: "CLIENT A"); Seed.TripReport(a);
        int soaId = SaveSoaFor("CLIENT A", (a, 6000m, 300m));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var (view, host) = CpBilling();
            var bad = new List<string>();
            typeof(BillingView).GetMethod("OpenNewRecordForm", CpF)!.Invoke(view, null); Pump(400);
            var lbl = CpField<Label>(view, "lblDel"); var cb = CpField<ComboBox>(view, "cbDelivery");
            if (lbl.Visible || cb.Visible) bad.Add($"the Completed Delivery Trip row shows on a new invoice (label {lbl.Visible}, box {cb.Visible})");
            if (!lbl.Text.Contains("Completed Delivery Trip")) bad.Add("setup: unexpected label text " + lbl.Text);
            var cbSoa = CpField<ComboBox>(view, "cbSoa");
            if (cbSoa.Items.Count == 0) { host.Close(); return "the statement waiting for an invoice is not offered: " + bad.Count + " problems so far"; }
            cbSoa.SelectedIndex = 0; Pump(300);
            CpField<TextBox>(view, "txtInvoiceNumber").Text = "8801"; Pump(400);
            typeof(BillingView).GetMethod("UpdateBillGuide", CpF)!.Invoke(view, null);
            var checklist = CpField<ValidationChecklist>(view, "billChecklist");
            if (checklist.Checks.Any(c => c.Text.Contains("Completed Delivery Trip", StringComparison.OrdinalIgnoreCase))) bad.Add("a check still mentions Completed Delivery Trip");
            if (checklist.Failed > 0) bad.Add("a statement-based invoice is blocked: " + string.Join(" | ", checklist.Checks.Where(c => !c.Passed).Select(c => c.Text)));
            else
            {
                typeof(BillingView).GetMethod("BtnSave_Click", CpF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(600);
                if (Long("SELECT COUNT(*) FROM Invoices WHERE InvoiceNumber='8801'") != 1) bad.Add("the statement-based invoice was not saved with the trip picker hidden");
            }
            if (cb.Visible || lbl.Visible) bad.Add("the trip row showed up again");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_PaidInvoice_NotListed_NotEditable()
    {
        Fresh(); RunBackfill();
        int paid = Seed.Invoice(Seed.Delivery(), 700m, PayStatus.Paid);
        int open = Seed.Invoice(Seed.Delivery(), 800m, PayStatus.Unpaid);
        int voided = Seed.Invoice(Seed.Delivery(), 900m, PayStatus.Unpaid);
        MJdatabase.CancelInvoice(voided, typingMistake: false, "the client returned the paper invoice");
        string paidNo = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", paid));
        string openNo = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", open));
        string voidNo = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", voided));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser();
            var (view, host) = CpBilling();
            var bad = new List<string>();
            var listed = CpListed(view).Select(i => i.InvoiceNumber).ToList();
            if (!listed.Contains(openNo)) bad.Add("the open invoice is not listed");
            if (listed.Contains(paidNo)) bad.Add("a Paid invoice is listed in Service Invoices");
            if (listed.Contains(voidNo)) bad.Add("a cancelled invoice is listed in Service Invoices");
            var search = CpField<TextBox>(view, "txtGridSearch");
            search.Text = paidNo; Pump(300);
            if (CpListed(view).Any(i => i.InvoiceNumber == paidNo)) bad.Add("searching brings the Paid invoice back into the list");
            search.Text = voidNo; Pump(300);
            if (CpListed(view).Any(i => i.InvoiceNumber == voidNo)) bad.Add("searching brings the cancelled invoice back into the list");
            search.Text = ""; Pump(200);
            // Opening a Paid invoice (e.g. from a link) is read-only: no Issue / Update, boxes locked, and Update refuses.
            var p = MJdatabase.GetInvoices().First(i => i.Id == paid);
            typeof(BillingView).GetMethod("SetFormVisibility", CpF)!.Invoke(view, new object[] { true }); Pump(300); // the form must be showing for Visible to mean anything
            if (!CpField<Panel>(view, "pnlForm").Visible) bad.Add("setup: the invoice form did not open");
            typeof(BillingView).GetMethod("BindInvoiceToForm", CpF)!.Invoke(view, new object[] { p }); Pump(300);
            if (CpField<Button>(view, "btnUpdate").Visible || CpField<Button>(view, "btnSave").Visible) bad.Add("a Paid invoice shows Issue / Update");
            if (!CpField<TextBox>(view, "txtInvoiceNumber").ReadOnly) bad.Add("the invoice number of a Paid invoice is editable");
            if (CpField<NumericUpDown>(view, "numTotalAmount").Enabled || CpField<NumericUpDown>(view, "numFuel").Enabled) bad.Add("the amounts of a Paid invoice are editable");
            CpField<NumericUpDown>(view, "numTotalAmount").Value = 1m;
            typeof(BillingView).GetMethod("BtnUpdate_Click", CpF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (Convert.ToDecimal(Scalar("SELECT NetAmount FROM Invoices WHERE Id=@i", ("@i", paid)) ?? 0) != 700m) bad.Add("Update changed a Paid invoice");
            // An open invoice still opens for editing.
            var o = MJdatabase.GetInvoices().First(i => i.Id == open);
            typeof(BillingView).GetMethod("BindInvoiceToForm", CpF)!.Invoke(view, new object[] { o }); Pump(300);
            view.GoToStep(2); Pump(200); // the step form shows Update on its last step only
            if (CpField<TextBox>(view, "txtInvoiceNumber").ReadOnly || !CpField<Button>(view, "btnUpdate").Visible) bad.Add("an open invoice can no longer be edited");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    private static (int Soa, int[] Invoices, string ReceiptNo) CpPaidStatement(string receiptNo, int daysAgo = 0)
    {
        var (soa, ids) = SoaWithThreeInvoices();
        MJdatabase.SaveOneReceipt(receiptNo, TargetFor(soa), "Cash", "", "");
        if (daysAgo > 0) Exec("UPDATE CollectionReceipts SET PaymentDate=@d WHERE ReceiptNumber=@n", ("@d", DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd")), ("@n", receiptNo));
        return (soa, ids, receiptNo);
    }

    [Check]
    public static string CompletedPayments_IssuedReceipt_ListedReadOnly_NotEditable()
    {
        Fresh(); RunBackfill();
        var (soa, ids, no) = CpPaidStatement("0311");
        var (soa2, ids2) = SoaWithThreeInvoices(); // one more statement still waiting for payment
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser();
            var view = new CollectionView { Dock = DockStyle.Fill };
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            host.Controls.Add(view); host.Show(); Pump(800);
            var bad = new List<string>();
            var grid = CpField<DataGridView>(view, "gridCollections");
            var listed = (grid.DataSource as List<CollectionReceipt>) ?? new List<CollectionReceipt>();
            // Nov 25: the list shows the issued receipts, preview only.
            if (!listed.Any(r => r.ReceiptNumber == no)) bad.Add("an issued receipt is not listed in Collection Receipts");
            // Nov 26: the page title counts what waits for a receipt; the completed list has its own title right above it
            var header = CpField<Label>(view, "lblGridHeader");
            if (!header.Text.StartsWith("For Collection Receipt (")) bad.Add("the page title is '" + header.Text + "'");
            var done = CpField<Label>(view, "lblCompletedTitle");
            if (!done.Text.StartsWith("Completed Collection receipts (")) bad.Add("the completed list title is '" + done.Text + "'");
            if (CpField<Label>(view, "lblSummary").Visible) bad.Add("the issued receipts line is still shown under the title");
            int row = listed.FindIndex(r => r.ReceiptNumber == no);
            if (row >= 0 && row < grid.Rows.Count)
            {
                grid.ClearSelection(); grid.Rows[row].Selected = true; Pump(300);
                if (typeof(CollectionView).GetField("selectedCollection", CpF)!.GetValue(view) != null) bad.Add("clicking a listed receipt loads it into the entry form");
                // Nov 25: double-click opens the read-only details window (no inline preview panel any more)
                using var dlg = Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms.CollectionReceiptDetailsDialog.Create(view, listed[row]);
                dlg.Show(host); Pump(300);
                var all = new List<Control>();
                void Walk(Control c) { all.Add(c); foreach (Control k in c.Controls) Walk(k); }
                Walk(dlg);
                if (!dlg.Text.Contains(no) || !all.OfType<Label>().Any(l => l.Text.Contains(no))) bad.Add("the details window does not show the receipt number");
                if (all.Any(c => c is TextBox || c is ComboBox || c is NumericUpDown || c is DateTimePicker)) bad.Add("the details window has an editable control");
                if (all.OfType<DataGridView>().Any(gv => !gv.ReadOnly || gv.AllowUserToAddRows || gv.AllowUserToDeleteRows)) bad.Add("a table in the details window is editable");
                if (!all.OfType<Button>().Any(b => b.Text.Contains("Print"))) bad.Add("the details window has no Print button");
                dlg.Close();
            }
            var ready = CpField<object>(view, "collReady");
            if (ready == null) bad.Add("the waiting-for-payment list is gone");
            // A saved receipt opened in the form (reflection) is read-only: no Update, boxes locked, Update refuses.
            var saved = MJdatabase.GetCollectionReceipts().First(r => r.ReceiptNumber == no);
            typeof(CollectionView).GetMethod("SetFormVisibility", CpF)!.Invoke(view, new object[] { true }); Pump(300);
            typeof(CollectionView).GetMethod("BindCollectionToForm", CpF)!.Invoke(view, new object[] { saved }); Pump(300);
            var update = CpField<Button>(view, "btnUpdate"); var save = CpField<Button>(view, "btnSave");
            if (update.Enabled || (bool)typeof(CollectionView).GetField("updateVisWanted", CpF)!.GetValue(view)!) bad.Add("an issued receipt offers Update");
            if (save.Enabled || (bool)typeof(CollectionView).GetField("saveVisWanted", CpF)!.GetValue(view)!) bad.Add("an issued receipt offers Save");
            if (!CpField<TextBox>(view, "txtRefNo").ReadOnly || !CpField<TextBox>(view, "txtNotes").ReadOnly) bad.Add("the reference / notes of an issued receipt are editable");
            CpField<TextBox>(view, "txtRefNo").Text = "CHANGED-REF";
            typeof(CollectionView).GetMethod("BtnUpdate_Click", CpF)!.Invoke(view, new object?[] { null, EventArgs.Empty }); Pump(300);
            if (Str("SELECT COALESCE(ReferenceNumber,'') FROM CollectionReceipts WHERE ReceiptNumber=@n LIMIT 1", ("@n", no)) == "CHANGED-REF") bad.Add("Update changed an issued receipt");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_FinanceHistory_ListsCompleted_NewestFirst()
    {
        Fresh(); RunBackfill();
        var (soaNew, idsNew, noNew) = CpPaidStatement("0321");
        var (soaOld, idsOld, noOld) = CpPaidStatement("0322", daysAgo: 2);
        var (soaOpen, idsOpen) = SoaWithThreeInvoices();                       // unpaid: must not be listed
        int voidedReceiptSoa = SoaWithThreeInvoices().Soa;
        var rows = MJdatabase.SaveOneReceipt("0323", TargetFor(voidedReceiptSoa), "Cash", "", "");
        MJdatabase.DeleteRecord("CollectionReceipts", rows[0].Id);              // voided: must not be listed as completed
        string soaNewNo = Str("SELECT SoaNumber FROM StatementsOfAccount WHERE Id=@i", ("@i", soaNew));
        string soaOldNo = Str("SELECT SoaNumber FROM StatementsOfAccount WHERE Id=@i", ("@i", soaOld));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var hist = new FinanceHistoryView { Dock = DockStyle.Fill };
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            host.Controls.Add(hist); host.Show(); Pump(800);
            var bad = new List<string>();
            var list = hist.CompletedList;
            if (list == null || hist.CurrentList != "Completed") return "Finance History does not open on the Completed list";
            list.RefreshNow(); Pump(200);
            var shown = list.Shown;
            bool Has(string kind, string number) => shown.Any(r => r.Kind == kind && r.Number == number);
            if (!Has(FinanceHistoryRow.Soa, soaNewNo) || !Has(FinanceHistoryRow.Soa, soaOldNo)) bad.Add("a paid statement is missing");
            if (!Has(FinanceHistoryRow.Receipt, noNew) || !Has(FinanceHistoryRow.Receipt, noOld)) bad.Add("an issued receipt is missing");
            foreach (int id in idsNew.Concat(idsOld))
                if (!shown.Any(r => r.Kind == FinanceHistoryRow.Invoice && r.Id == id)) { bad.Add("a paid service invoice is missing (id " + id + ")"); break; }
            if (shown.Any(r => r.Number == "0323")) bad.Add("a voided receipt is listed as completed");
            if (shown.Any(r => r.Kind == FinanceHistoryRow.Invoice && idsOpen.Contains(r.Id)) || shown.Any(r => r.Kind == FinanceHistoryRow.Soa && r.Id == soaOpen)) bad.Add("open work is listed as completed");
            if (shown.Count(r => r.Kind == FinanceHistoryRow.Receipt) != 2) bad.Add("a statement's receipt is not one row per receipt number");
            for (int i = 1; i < shown.Count; i++)
                if (shown[i].Date.Date > shown[i - 1].Date.Date || (shown[i].Date.Date == shown[i - 1].Date.Date && shown[i].Id > shown[i - 1].Id)) { bad.Add("not newest first at row " + i); break; }
            int newRow = shown.ToList().FindIndex(r => r.Kind == FinanceHistoryRow.Soa && r.Number == soaNewNo), oldRow = shown.ToList().FindIndex(r => r.Kind == FinanceHistoryRow.Soa && r.Number == soaOldNo);
            if (newRow < 0 || oldRow < 0 || newRow > oldRow) bad.Add("today's statement is not above the older one");
            if (list.Grid.Rows.Count != shown.Count) bad.Add("the grid does not show every row");
            if (!list.Grid.ReadOnly) bad.Add("the history grid is editable");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_FinanceHistory_Voided_ListsCancelledAndVoided()
    {
        Fresh(); RunBackfill();
        int cancelledSoa = Seed.Soa("CLIENT A", PayStatus.Unpaid, 1500m);
        Seed.Invoice(Seed.Delivery(), 1500m, PayStatus.Unpaid, soaId: cancelledSoa);
        MJdatabase.CancelStatementOfAccount(cancelledSoa, "Wrong client was picked");
        int cancelledInv = Seed.Invoice(Seed.Delivery(), 640m, PayStatus.Unpaid);
        MJdatabase.CancelInvoice(cancelledInv, typingMistake: false, "client returned the spoiled paper invoice");
        var (voidSoa, _) = SoaWithThreeInvoices();
        var rows = MJdatabase.SaveOneReceipt("0331", TargetFor(voidSoa), "Cash", "", "");
        MJdatabase.DeleteRecord("CollectionReceipts", rows[0].Id);
        var (paidSoa, _, paidNo) = CpPaidStatement("0332");
        string soaNo = Str("SELECT SoaNumber FROM StatementsOfAccount WHERE Id=@i", ("@i", cancelledSoa));
        string invNo = Str("SELECT InvoiceNumber FROM Invoices WHERE Id=@i", ("@i", cancelledInv));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            var hist = new FinanceHistoryView { Dock = DockStyle.Fill };
            using var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
            host.Controls.Add(hist); host.Show(); Pump(800);
            var bad = new List<string>();
            hist.ShowList(true); Pump(300);
            var list = hist.VoidedList;
            if (list == null || hist.CurrentList != "Voided") return "the Voided list does not open";
            list.RefreshNow(); Pump(200);
            var shown = list.Shown;
            var s = shown.FirstOrDefault(r => r.Kind == FinanceHistoryRow.Soa && r.Number == soaNo);
            var i = shown.FirstOrDefault(r => r.Kind == FinanceHistoryRow.Invoice && r.Number == invNo);
            var c = shown.FirstOrDefault(r => r.Kind == FinanceHistoryRow.Receipt && r.Number == "0331");
            if (s == null) bad.Add("the cancelled statement is not under Voided"); else if (!s.Reason.Contains("Wrong client")) bad.Add("its reason is not shown: '" + s.Reason + "'");
            if (i == null) bad.Add("the cancelled invoice is not under Voided"); else if (!i.Reason.Contains("spoiled")) bad.Add("its reason is not shown: '" + i.Reason + "'");
            if (c == null) bad.Add("the voided receipt is not under Voided");
            if (shown.Any(r => r.Number == paidNo)) bad.Add("a live receipt is listed as voided");
            if (s != null && s.Client != "CLIENT A") bad.Add("the client is missing");
            if (i != null && i.Amount != 640m) bad.Add("the amount is wrong: " + i.Amount);
            for (int k = 1; k < shown.Count; k++)
                if (shown[k].Date.Date > shown[k - 1].Date.Date || (shown[k].Date.Date == shown[k - 1].Date.Date && shown[k].Id > shown[k - 1].Id)) { bad.Add("not newest first at row " + k); break; }
            if (!list.Grid.ReadOnly) bad.Add("the voided grid is editable");
            hist.ShowList(false); Pump(200);
            hist.CompletedList!.RefreshNow();
            if (hist.CompletedList.Shown.Any(r => r.Number == soaNo || r.Number == invNo || r.Number == "0331")) bad.Add("a cancelled / voided record is listed as completed");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    private static MenuTexts CpMenu(FinanceHistoryRow row, FinanceSettledView list)
    {
        using var m = new ContextMenuStrip();
        list.BuildMenu(row, m);
        return new MenuTexts(m.Items.OfType<ToolStripMenuItem>().Select(i => i.Text ?? "").ToList(), m.Items.OfType<ToolStripMenuItem>().ToList());
    }
    private sealed record MenuTexts(List<string> Texts, List<ToolStripMenuItem> Items);

    private static (Form host, FinanceHistoryView hist, FinanceSettledView list) CpHistory()
    {
        var hist = new FinanceHistoryView { Dock = DockStyle.Fill };
        var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-5000, -5000), ClientSize = new System.Drawing.Size(1500, 900) };
        host.Controls.Add(hist); host.Show(); Pump(800);
        hist.CompletedList!.RefreshNow(); Pump(200);
        return (host, hist, hist.CompletedList!);
    }

    [Check]
    public static string CompletedPayments_VoidReceiptFromHistory_MovesToVoided_AndStatementWaitsAgain()
    {
        Fresh(); RunBackfill();
        var (soa, ids, no) = CpPaidStatement("0341");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser(yes: true);
            var (host, hist, list) = CpHistory();
            var bad = new List<string>();
            var row = list.Shown.FirstOrDefault(r => r.Kind == FinanceHistoryRow.Receipt && r.Number == no);
            if (row == null) { host.Close(); return "the issued receipt is not in Completed"; }
            var menu = CpMenu(row, list);
            var item = menu.Items.FirstOrDefault(i => (i.Text ?? "").StartsWith("Void receipt"));
            if (item == null) bad.Add("no Void receipt item for an administrator: " + string.Join(" | ", menu.Texts));
            else { item.PerformClick(); Pump(1500); }
            if (Long("SELECT COUNT(*) FROM CollectionReceipts WHERE ReceiptNumber=@n AND COALESCE(IsVoid,0)=0", ("@n", no)) != 0) bad.Add("the receipt is still live");
            if (SoaStatus(soa) != PayStatus.Unpaid || ids.Any(i => InvStatus(i) != PayStatus.Unpaid)) bad.Add($"statement {SoaStatus(soa)} / invoices not back to unpaid");
            if (!MJdatabase.GetPayTargets().Any(t => t.SoaId == soa)) bad.Add("the statement is not waiting for payment again");
            hist.ShowList(true); Pump(200); hist.VoidedList!.RefreshNow();
            if (!hist.VoidedList.Shown.Any(r => r.Kind == FinanceHistoryRow.Receipt && r.Number == no)) bad.Add("the voided receipt is not under Voided");
            hist.ShowList(false); Pump(200); hist.CompletedList!.RefreshNow();
            if (hist.CompletedList.Shown.Any(r => r.Number == no || r.Id == soa && r.Kind == FinanceHistoryRow.Soa)) bad.Add("voided work is still under Completed");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_CancelFromHistory_RefusedWhilePaid_PointsToFinanceHistory()
    {
        Fresh(); RunBackfill();
        var (soa, ids, no) = CpPaidStatement("0351");
        string soaNo = Str("SELECT SoaNumber FROM StatementsOfAccount WHERE Id=@i", ("@i", soa));
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser(yes: true);
            var (host, hist, list) = CpHistory();
            var bad = new List<string>();
            string why = MJdatabase.ValidateRecordDeletion("Invoices", ids[0]) ?? "";
            if (why.Length == 0) bad.Add("cancelling a paid invoice is not guarded");
            else if (!why.Contains("Finance History") || why.Contains("Collections")) bad.Add("invoice message does not point to Finance History: " + why);
            string whySoa = MJdatabase.WhySoaCannotBeCancelled(MJdatabase.GetStatementsOfAccount().First(s => s.Id == soa));
            if (!whySoa.Contains("Finance History")) bad.Add("statement message does not point to Finance History: " + whySoa);
            var invRow = list.Shown.First(r => r.Kind == FinanceHistoryRow.Invoice && r.Id == ids[0]);
            var m1 = CpMenu(invRow, list);
            var cancelInv = m1.Items.FirstOrDefault(i => (i.Text ?? "").StartsWith("Cancel invoice"));
            if (cancelInv == null) bad.Add("no Cancel invoice item"); else { cancelInv.PerformClick(); Pump(1000); }
            var soaRow = list.Shown.First(r => r.Kind == FinanceHistoryRow.Soa && r.Number == soaNo);
            var cancelSoa = CpMenu(soaRow, list).Items.FirstOrDefault(i => (i.Text ?? "").StartsWith("Cancel statement"));
            if (cancelSoa == null) bad.Add("no Cancel statement item"); else { cancelSoa.PerformClick(); Pump(1000); }
            if (InvStatus(ids[0]) != PayStatus.Paid) bad.Add("the paid invoice was cancelled while a receipt pays it");
            if (SoaStatus(soa) != PayStatus.Paid) bad.Add("the paid statement was cancelled while a receipt pays it");
            host.Close();
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string CompletedPayments_HistoryMenu_AdminActionsHiddenForStaff()
    {
        Fresh(); RunBackfill();
        var (soa, ids, no) = CpPaidStatement("0361");
        return RunOnSta(() =>
        {
            SignInAsAdmin();
            using var boxes = new BoxCloser(GetCurrentThreadId());
            using var dialogs = new CpDialogCloser();
            var (host, hist, list) = CpHistory();
            var bad = new List<string>();
            bool Actions(FinanceHistoryRow r) => CpMenu(r, list).Texts.Any(t => t.StartsWith("Void receipt") || t.StartsWith("Cancel invoice") || t.StartsWith("Cancel statement"));
            var rows = list.Shown.GroupBy(r => r.Kind).Select(g => g.First()).ToList();
            if (rows.Count != 3) bad.Add("setup: expected one row of each kind, got " + rows.Count);
            foreach (var r in rows) if (!Actions(r)) bad.Add("an administrator has no action on a " + r.Kind + " row");
            SessionContext.ActiveUser = new User { Id = 2, Username = "staff", Role = "Staff", FullName = "Staff", IsActive = true };
            foreach (var r in rows) if (Actions(r)) bad.Add("a non-admin sees a Void / Cancel action on a " + r.Kind + " row");
            if (!CpMenu(rows[0], list).Texts.Any(t => t.StartsWith("Open"))) bad.Add("Open (read only) is missing for staff");
            SignInAsAdmin();
            host.Close();
            return string.Join("; ", bad);
        });
    }
}