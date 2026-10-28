import os

load_path = os.path.join("Views", "LoadingManagementView.cs")
with open(load_path, "r", encoding="utf-8") as f:
    text = f.read()

old_reload = """        public void ReloadRoutesList()
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
            if (!string.IsNullOrWhiteSpace(currentText))
            {
                cbDestination.Text = currentText;
            }
        }"""
old_reload_crlf = old_reload.replace("\n", "\r\n")

new_reload = """        public void ReloadRoutesList()
        {
            string cl = cbClient.Text.Trim();
            var activeRoutes = MJdatabase.GetDeliveryRoutesByClient(cl, activeOnly: true);
            string currentText = cbDestination.Text;
            cbDestination.BeginUpdate();
            cbDestination.Items.Clear();
            foreach (var r in activeRoutes.GroupBy(x => x.RouteCode).Select(g => g.First()))
            {
                cbDestination.Items.Add($"{r.RouteCode} - {r.RouteName}");
            }
            cbDestination.EndUpdate();
            if (!string.IsNullOrWhiteSpace(currentText))
            {
                cbDestination.Text = currentText;
            }
        }"""

if old_reload in text:
    text = text.replace(old_reload, new_reload, 1)
elif old_reload_crlf in text:
    text = text.replace(old_reload_crlf, new_reload.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_reload not found")

old_wiring = """            cbClient.TextChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) UpdateClientContactHint(); };"""
old_wiring_crlf = old_wiring.replace("\n", "\r\n")

new_wiring = """            cbClient.TextChanged += (s, e) => { if (!isBindingForm) { UpdateClientContactHint(); ReloadRoutesList(); } };
            cbClient.SelectedIndexChanged += (s, e) => { if (!isBindingForm) { UpdateClientContactHint(); ReloadRoutesList(); } };"""

if old_wiring in text:
    text = text.replace(old_wiring, new_wiring, 1)
elif old_wiring_crlf in text:
    text = text.replace(old_wiring_crlf, new_wiring.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_wiring not found")

with open(load_path, "w", encoding="utf-8") as f:
    f.write(text)

print("LoadingManagementView.cs updated successfully!")
