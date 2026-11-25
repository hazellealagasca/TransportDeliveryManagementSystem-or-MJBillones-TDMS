using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class ValidationHelper
    {
        private static Color ErrorBorderColor => Pal.C(239, 68, 68);
        private static Color DefaultBorderColor => Pal.C(203, 213, 225);
        public static bool ValidateRequiredText(Control control, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            string val = control.Text?.Trim() ?? "";
            bool valid = !string.IsNullOrWhiteSpace(val);
            ApplyValidationState(control, valid, valid ? "" : $"{fieldName} is required and cannot be blank.", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} is required.");
            }
            return valid;
        }

        public static bool ValidateRequiredCombo(ComboBox combo, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            bool valid = combo.SelectedIndex >= 0 || !string.IsNullOrWhiteSpace(combo.Text?.Trim());
            ApplyValidationState(combo, valid, valid ? "" : $"Please select or enter a valid {fieldName}.", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} selection is required.");
            }
            return valid;
        }

        public static bool ValidatePositiveNumber(NumericUpDown num, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            bool valid = num.Value > 0;
            ApplyValidationState(num, valid, valid ? "" : $"{fieldName} must be greater than zero.", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} must be greater than 0.");
            }
            return valid;
        }

        public static bool ValidateNonNegativeNumber(NumericUpDown num, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            bool valid = num.Value >= 0;
            ApplyValidationState(num, valid, valid ? "" : $"{fieldName} cannot be negative.", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} cannot be negative.");
            }
            return valid;
        }

        public static bool ValidatePhoneNumber(Control control, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            string raw = control.Text?.Trim() ?? "";
            string digits = Regex.Replace(raw, @"\D", "");
            bool valid = digits.StartsWith("09") && digits.Length == 11;
            ApplyValidationState(control, valid, valid ? "" : $"{fieldName} must be an 11-digit mobile number starting with 09 (e.g. 09425144754).", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} must follow the format 09XXXXXXXXX (11 digits).");
            }
            return valid;
        }

        public static bool ValidateLicenseNumber(Control control, string fieldName, List<string> errors, ErrorProvider? ep = null)
        {
            string val = control.Text?.Trim() ?? "";
            bool valid = !string.IsNullOrWhiteSpace(val) && val.Length >= 4;
            ApplyValidationState(control, valid, valid ? "" : $"{fieldName} is required.", ep);
            if (!valid)
            {
                errors.Add($"{fieldName} is required.");
            }
            return valid;
        }

        public static void ApplyValidationState(Control control, bool isValid, string errorMessage, ErrorProvider? ep = null)
        {
            if (ep != null)
            {
                ep.SetError(control, isValid ? "" : errorMessage);
            }

            // Nov 25: tint + outline through MissingInput (clears as soon as a proper value is entered); Tag is no longer touched
            if (isValid) MissingInput.Clear(control);
            else MissingInput.Mark(control);
        }

        public static void ClearErrors(Control parent, ErrorProvider? ep = null)
        {
            if (ep != null)
            {
                ep.Clear();
            }

            MissingInput.ClearAll(parent); // Nov 25
        }

        public static bool ShowValidationSummaryIfAny(IWin32Window owner, List<string> errors, Control? firstInvalidControl = null)
        {
            if (errors == null || errors.Count == 0) return true;

            if (firstInvalidControl != null)
            {
                // Nov 25: scroll to it and tint it before the message; focus it after OK
                for (var p = firstInvalidControl.Parent; p != null; p = p.Parent)
                    if (p is ScrollableControl sc && sc.AutoScroll) { try { sc.ScrollControlIntoView(firstInvalidControl); } catch (Exception ex) { ErrLog.Swallowed(ex); } break; }
                MissingInput.Mark(firstInvalidControl);
            }

            string bulletList = string.Join(Environment.NewLine + " • ", errors.Take(7));
            if (errors.Count > 7)
            {
                bulletList += Environment.NewLine + $" • ...and {errors.Count - 7} more item(s)";
            }

            MessageBox.Show(
                owner,
                "Please complete all required fields before proceeding:" + Environment.NewLine + Environment.NewLine + " • " + bulletList,
                "Missing input",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            if (firstInvalidControl != null && !firstInvalidControl.IsDisposed && firstInvalidControl.CanFocus)
            {
                firstInvalidControl.Focus();
                if (firstInvalidControl is TextBox tb) tb.SelectAll();
            }

            return false;
        }

        public static IEnumerable<Control> GetAllControls(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                foreach (Control child in current.Controls)
                {
                    stack.Push(child);
                }
            }
        }
    }
}
