using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views;

namespace TDMS.Verify;

/// <summary>
/// Nov 25: a box with missing input is tinted light red (with a red outline) and the tint goes away as soon as a proper value is entered.
/// The original look is kept in the helper, never in Tag.
/// </summary>
public static partial class Checks
{
    [Check]
    public static string MissingInput_MarksAndClearsOnInput()
    {
        return RunOnSta(() =>
        {
            var bad = new List<string>();
            var host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000), ClientSize = new Size(500, 400), FormBorderStyle = FormBorderStyle.None };
            var tb = new TextBox { Location = new Point(10, 10), Width = 200, Tag = "keep" };
            var cb = new ComboBox { Location = new Point(10, 50), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cb.Items.AddRange(new object[] { "REF", "DRY" });
            var num = new NumericUpDown { Location = new Point(10, 90), Width = 100, Minimum = 0, Maximum = 1000 };
            var dtp = new DateTimePicker { Location = new Point(10, 130), Width = 200 };
            var tb2 = new TextBox { Location = new Point(10, 170), Width = 200 };
            host.Controls.AddRange(new Control[] { tb, cb, num, dtp, tb2 });
            host.Show(); Pump(300);
            try
            {
                var tbBack = tb.BackColor; var cbBack = cb.BackColor; var numBack = num.BackColor;
                MissingInput.Mark(tb); MissingInput.Mark(cb); MissingInput.Mark(num); MissingInput.Mark(dtp);
                MissingInput.Mark(tb); // twice must not double-hook
                if (!MissingInput.IsMarked(tb) || !MissingInput.IsMarked(cb) || !MissingInput.IsMarked(num) || !MissingInput.IsMarked(dtp)) bad.Add("a marked box is not reported as marked");
                if (tb.BackColor == tbBack) bad.Add("TextBox was not tinted");
                if (cb.BackColor == cbBack) bad.Add("ComboBox was not tinted");
                if (num.BackColor == numBack) bad.Add("NumericUpDown was not tinted");
                if (!Equals(tb.Tag, "keep")) bad.Add("Mark changed the TextBox Tag to '" + tb.Tag + "'");

                tb.Text = "   "; Pump(50);
                if (!MissingInput.IsMarked(tb)) bad.Add("blank spaces cleared the mark");
                tb.Text = "x"; Pump(50);
                if (MissingInput.IsMarked(tb)) bad.Add("typing text did not clear the TextBox mark");
                if (tb.BackColor != tbBack) bad.Add("TextBox colour was not restored");
                if (!Equals(tb.Tag, "keep")) bad.Add("Clear changed the TextBox Tag to '" + tb.Tag + "'");

                cb.SelectedIndex = 0; Pump(50);
                if (MissingInput.IsMarked(cb)) bad.Add("choosing an item did not clear the ComboBox mark");
                if (cb.BackColor != cbBack) bad.Add("ComboBox colour was not restored");

                num.Value = 5; Pump(50);
                if (MissingInput.IsMarked(num)) bad.Add("a value did not clear the NumericUpDown mark");
                if (num.BackColor != numBack) bad.Add("NumericUpDown colour was not restored");

                dtp.Value = dtp.Value.AddDays(3); Pump(50);
                if (MissingInput.IsMarked(dtp)) bad.Add("changing the date did not clear the DateTimePicker mark");

                // an own rule: stays marked until the predicate is true
                tb2.Text = ""; MissingInput.Mark(tb2, () => tb2.Text.Length >= 3);
                tb2.Text = "ab"; Pump(50);
                if (!MissingInput.IsMarked(tb2)) bad.Add("the mark cleared before the box passed its own rule");
                tb2.Text = "abc"; Pump(50);
                if (MissingInput.IsMarked(tb2)) bad.Add("the mark stayed after the box passed its own rule");

                // ClearAll walks the children
                MissingInput.Mark(tb); MissingInput.Mark(num);
                MissingInput.ClearAll(host);
                if (MissingInput.IsMarked(tb) || MissingInput.IsMarked(num)) bad.Add("ClearAll left a mark");
            }
            finally { host.Close(); host.Dispose(); }
            return string.Join("; ", bad);
        });
    }

    [Check]
    public static string MissingInput_SoaNextWithoutDeliveryType_MarksIt()
    {
        Fresh(); RunBackfill(); LockClient();
        return RunOnSta(() =>
        {
            using var dialogs = new CpDialogCloser();
            var (view, host) = LockOpen();
            using var _ = host;
            var cbClient = LockMember<ComboBox>(view, "cbClient");
            var cbDelivery = LockMember<ComboBox>(view, "cbDeliveryType");
            if (cbClient == null || cbDelivery == null) return "form fields not found";
            const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
            view.GetType().GetMethod("OpenNewStatement", f)!.Invoke(view, null); Pump(500);
            cbClient.Text = "CLIENT A"; cbDelivery.SelectedIndex = -1; Pump(200);
            var validate = view.GetType().GetMethod("ValidateSoaStep", f)!;
            bool passed = (bool)validate.Invoke(view, new object[] { 1 })!;
            Pump(200);
            var bad = new List<string>();
            if (passed) bad.Add("step 1 passed without a delivery type");
            if (!MissingInput.IsMarked(cbDelivery)) bad.Add("the delivery type box was not marked");
            if (cbDelivery.Items.Count > 0) { cbDelivery.SelectedIndex = 0; Pump(100); if (MissingInput.IsMarked(cbDelivery)) bad.Add("choosing a delivery type did not clear the mark"); }
            else bad.Add("the delivery type box has no items");
            return string.Join("; ", bad);
        });
    }
}
