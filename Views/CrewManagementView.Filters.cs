using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>The Type (All / Regular / Seasonal) and Status (All / Active / On trip / Inactive) filters, left of the Actions button.</summary>
    public partial class CrewManagementView
    {
        private ComboBox? cbTypeFilter, cbStatusFilter;
        public static readonly string[] TypeFilterChoices = { "All", CrewRules.TypeRegular, CrewRules.TypeSeasonal };
        public static readonly string[] StatusFilterChoices = { "All", "Active", "On trip", "Inactive" };
        private void BuildCrewFilters(Control before)
        {
            Label Caption(string t) => new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 9F), ForeColor = AppTheme.TextSecondary, Margin = new Padding(Dp(6), Dp(11), Dp(4), 0) };
            ComboBox Combo(string[] items) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dp(96), Font = new Font("Segoe UI", 9.5F), Margin = new Padding(0, Dp(5), Dp(4), 0) }; c.Items.AddRange(items); c.SelectedIndex = 0; c.SelectedIndexChanged += (s, e) => FilterGridData(); return c; }
            cbTypeFilter = Combo(TypeFilterChoices);
            cbStatusFilter = Combo(StatusFilterChoices);
            foreach (Control c in new Control[] { Caption("Type"), cbTypeFilter, Caption("Status"), cbStatusFilter })
            {
                pnlGridActionsFlow.Controls.Add(c);
                pnlGridActionsFlow.Controls.SetChildIndex(c, pnlGridActionsFlow.Controls.GetChildIndex(before));
            }
        }
        /// <summary>The list as the Type and Status filters leave it.</summary>
        private List<Crew> ApplyCrewFilters(List<Crew> list) => CrewRules.Filter(list, cbTypeFilter?.Text, cbStatusFilter?.Text);
    }
}
