using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Date and number boxes refuse a value outside their own limits by throwing "Value of … is not valid for 'Value'.
    /// 'Value' should be between 'MinDate' and 'MaxDate'" (or 'Minimum' and 'Maximum'). That popped up on the Crew screen
    /// after the app was left open overnight, because the "up to today" limit was still yesterday.
    /// Fill every date and number box through <see cref="SetSafe(DateTimePicker, DateTime)"/> / <see cref="SetSafe(NumericUpDown, decimal)"/>:
    /// the value is first fitted inside the box's limits, so filling a box can never fail. A source check in the Verify
    /// project fails the build when someone assigns <c>.Value =</c> directly again.
    /// </summary>
    public static class RangeGuard
    {
        private sealed class TodayCap { public int DaysAhead; }
        private static readonly ConditionalWeakTable<DateTimePicker, TodayCap> todayCaps = new();

        /// <summary>The date closest to <paramref name="value"/> that the picker accepts.</summary>
        public static DateTime Fit(this DateTimePicker picker, DateTime value)
        {
            MoveTodayCap(picker);
            DateTime lo = picker.MinDate, hi = picker.MaxDate;
            return value < lo ? lo : value > hi ? hi : value;
        }

        /// <summary>Puts a date in the picker. A date outside its limits (a future date, a blank 0001 date) is moved to the nearest allowed one instead of failing.</summary>
        public static void SetSafe(this DateTimePicker picker, DateTime value) => picker.Value = picker.Fit(value);

        /// <summary>
        /// Limits the picker to "today" (or a few days after it). The limit moves forward by itself when the day changes,
        /// so a screen that stays open past midnight still accepts today.
        /// </summary>
        public static void CapAtToday(this DateTimePicker picker, int daysAhead = 0)
        {
            if (!todayCaps.TryGetValue(picker, out var cap))
            {
                todayCaps.Add(picker, cap = new TodayCap());
                picker.DropDown += OnTouched;
                picker.Enter += OnTouched;
            }
            cap.DaysAhead = daysAhead;
            MoveTodayCap(picker);
        }

        private static void OnTouched(object? sender, EventArgs e) { if (sender is DateTimePicker p) MoveTodayCap(p); }

        private static void MoveTodayCap(DateTimePicker picker)
        {
            if (!todayCaps.TryGetValue(picker, out var cap)) return;
            DateTime limit = DateTime.Today.AddDays(cap.DaysAhead);
            if (limit < picker.MinDate) limit = picker.MinDate;
            if (picker.MaxDate != limit) picker.MaxDate = limit;
        }

        /// <summary>The number closest to <paramref name="value"/> that the box accepts.</summary>
        public static decimal Fit(this NumericUpDown box, decimal value) => value < box.Minimum ? box.Minimum : value > box.Maximum ? box.Maximum : value;

        /// <summary>Puts a number in the box. A number outside its limits is moved to the nearest allowed one instead of failing.</summary>
        public static void SetSafe(this NumericUpDown box, decimal value) => box.Value = box.Fit(value);
    }
}
