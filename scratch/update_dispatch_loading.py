import os
import re

# 1. Update DispatchManagementView.cs
dsp_path = os.path.join("Views", "DispatchManagementView.cs")
with open(dsp_path, "r", encoding="utf-8") as f:
    text_dsp = f.read()

# Replace rate matrix lookup in DispatchManagementView
rate_needle = """            var rate = MJdatabase.GetRateMatrix().FirstOrDefault(r => r.Destination.Equals(dest, StringComparison.OrdinalIgnoreCase));

            if (rate != null)

            {

                tipRouteStores.SetToolTip(cbDestination, $"📍 Route: {rate.RouteName} ({rate.Destination}) | Standard Rate: ₱{rate.RateAmount:N2}");

            }

            else

            {

                tipRouteStores.SetToolTip(cbDestination, $"📍 Destination: {dest} (Direct Delivery)");

            }"""
rate_needle_crlf = rate_needle.replace("\n", "\r\n")

new_tooltip = """            tipRouteStores.SetToolTip(cbDestination, $"📍 Destination: {dest} (Direct Delivery)");"""

if rate_needle in text_dsp:
    text_dsp = text_dsp.replace(rate_needle, new_tooltip, 1)
elif rate_needle_crlf in text_dsp:
    text_dsp = text_dsp.replace(rate_needle_crlf, new_tooltip.replace("\n", "\r\n"), 1)
else:
    raise Exception("rate_needle not found in DispatchManagementView.cs")

# Add cbClient.SelectedIndexChanged in DispatchManagementView.cs
dest_sel_change = "cbDestination.SelectedIndexChanged += (s, e) =>"
assert dest_sel_change in text_dsp, "dest_sel_change not found in DispatchManagementView.cs"

client_sel_handler = """cbClient.SelectedIndexChanged += async (s, e) =>
            {
                if (!isBindingForm)
                {
                    string cl = cbClient.Text.Trim();
                    var routes = await Task.Run(() => MJdatabase.GetDeliveryRoutesByClient(cl, activeOnly: true));
                    string curDest = cbDestination.Text;
                    cbDestination.BeginUpdate();
                    cbDestination.Items.Clear();
                    foreach (var r in routes.GroupBy(x => x.RouteCode).Select(g => g.First()))
                    {
                        cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
                    }
                    cbDestination.EndUpdate();
                    if (!string.IsNullOrEmpty(curDest) && cbDestination.Items.Contains(curDest))
                    {
                        cbDestination.SelectedItem = curDest;
                    }
                }
            };

            """

text_dsp = text_dsp.replace(dest_sel_change, client_sel_handler + dest_sel_change, 1)

with open(dsp_path, "w", encoding="utf-8") as f:
    f.write(text_dsp)
print("DispatchManagementView.cs updated successfully!")

# 2. Update LoadingManagementView.cs
load_path = os.path.join("Views", "LoadingManagementView.cs")
with open(load_path, "r", encoding="utf-8") as f:
    text_load = f.read()

# Update RefreshDestinationDropdown
old_ref_dest = """        private void RefreshDestinationDropdown()
        {
            var activeRoutes = MJdatabase.GetDeliveryRoutes(activeOnly: true);
            string currentText = cbDestination.Text;
            cbDestination.BeginUpdate();
            cbDestination.Items.Clear();
            foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First()))
            {
                cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
            }
            cbDestination.EndUpdate();
            if (!string.IsNullOrEmpty(currentText) && cbDestination.Items.Contains(currentText))
            {
                cbDestination.SelectedItem = currentText;
            }
        }"""
old_ref_dest_crlf = old_ref_dest.replace("\n", "\r\n")

new_ref_dest = """        private void RefreshDestinationDropdown(string? clientName = null)
        {
            string cl = clientName ?? cbClient.Text.Trim();
            var activeRoutes = MJdatabase.GetDeliveryRoutesByClient(cl, activeOnly: true);
            string currentText = cbDestination.Text;
            cbDestination.BeginUpdate();
            cbDestination.Items.Clear();
            foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First()))
            {
                cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
            }
            cbDestination.EndUpdate();
            if (!string.IsNullOrEmpty(currentText) && cbDestination.Items.Contains(currentText))
            {
                cbDestination.SelectedItem = currentText;
            }
        }"""

if old_ref_dest in text_load:
    text_load = text_load.replace(old_ref_dest, new_ref_dest, 1)
elif old_ref_dest_crlf in text_load:
    text_load = text_load.replace(old_ref_dest_crlf, new_ref_dest.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_ref_dest not found in LoadingManagementView.cs")

# Update event wiring in LoadingManagementView.cs
old_wiring = """            cbClient.TextChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };"""
old_wiring_crlf = old_wiring.replace("\n", "\r\n")

new_wiring = """            cbClient.TextChanged += (s, e) => { if (!isBindingForm) { UpdateClientContactHint(); RefreshDestinationDropdown(); } };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) { UpdateClientContactHint(); RefreshDestinationDropdown(); } };"""

if old_wiring in text_load:
    text_load = text_load.replace(old_wiring, new_wiring, 1)
elif old_wiring_crlf in text_load:
    text_load = text_load.replace(old_wiring_crlf, new_wiring.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_wiring not found in LoadingManagementView.cs")

with open(load_path, "w", encoding="utf-8") as f:
    f.write(text_load)
print("LoadingManagementView.cs updated successfully!")
