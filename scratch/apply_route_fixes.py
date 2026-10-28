import os

mjdb_path = r"Database/MJdatabase.cs"
with open(mjdb_path, "r", encoding="utf-8") as f:
    mjdb = f.read()

old_get_trip_sum = """        public static ClientTripSummary GetClientTripSummary(string clientName)
        {
            var summary = new ClientTripSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        COUNT(CASE WHEN LOWER(Status) = 'completed' THEN 1 END) AS CompletedCount,
                        MAX(CASE WHEN LOWER(Status) = 'completed' THEN DispatchDate END) AS LatestCompleted,
                        COUNT(CASE WHEN LOWER(Status) = 'cancelled' THEN 1 END) AS CancelledCount,
                        MAX(CASE WHEN LOWER(Status) = 'cancelled' THEN DispatchDate END) AS LatestCancelled
                    FROM Deliveries 
                    WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))";
                cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        summary.CompletedCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                        summary.LatestCompletedDate = r.IsDBNull(1) ? "" : r.GetString(1);
                        summary.CancelledCount = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                        summary.LatestCancelledDate = r.IsDBNull(3) ? "" : r.GetString(3);
                    }
                }

                var cmdSpecial = conn.CreateCommand();
                cmdSpecial.CommandText = @"
                    SELECT COUNT(*), MAX(TripDate) 
                    FROM SpecialTrips 
                    WHERE LOWER(TRIM(CustomerName)) = LOWER(TRIM(@cl)) 
                       OR LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))";
                cmdSpecial.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var r = cmdSpecial.ExecuteReader())
                {
                    if (r.Read())
                    {
                        summary.SpecialCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                        summary.LatestSpecialDate = r.IsDBNull(1) ? "" : r.GetString(1);
                    }
                }
            }
            return summary;
        }"""

new_get_trip_sum = """        public static ClientTripSummary GetClientTripSummary(string clientName)
        {
            var summary = new ClientTripSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT 
                            COUNT(CASE WHEN LOWER(Status) = 'completed' THEN 1 END) AS CompletedCount,
                            MAX(CASE WHEN LOWER(Status) = 'completed' THEN DispatchDate END) AS LatestCompleted,
                            COUNT(CASE WHEN LOWER(Status) = 'cancelled' THEN 1 END) AS CancelledCount,
                            MAX(CASE WHEN LOWER(Status) = 'cancelled' THEN DispatchDate END) AS LatestCancelled
                        FROM Deliveries 
                        WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))";
                    cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.CompletedCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.LatestCompletedDate = r.IsDBNull(1) ? "" : r.GetString(1);
                            summary.CancelledCount = r.IsDBNull(2) ? 0 : Convert.ToInt32(r.GetValue(2));
                            summary.LatestCancelledDate = r.IsDBNull(3) ? "" : r.GetString(3);
                        }
                    }

                    var cmdSpecial = conn.CreateCommand();
                    cmdSpecial.CommandText = @"
                        SELECT COUNT(*), MAX(DispatchDate) 
                        FROM SpecialTrips 
                        WHERE LOWER(TRIM(Company)) = LOWER(TRIM(@cl))";
                    cmdSpecial.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmdSpecial.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.SpecialCount = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.LatestSpecialDate = r.IsDBNull(1) ? "" : r.GetString(1);
                        }
                    }
                }
            }
            catch
            {
            }
            return summary;
        }"""

assert old_get_trip_sum in mjdb, "old_get_trip_sum not found in MJdatabase.cs"
mjdb = mjdb.replace(old_get_trip_sum, new_get_trip_sum)

old_get_pay_sum = """        public static ClientPaymentSummary GetClientPaymentSummary(string clientName)
        {
            var summary = new ClientPaymentSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT 
                        COUNT(*),
                        COUNT(CASE WHEN LOWER(Status) != 'paid' THEN 1 END),
                        COALESCE(SUM(TotalAmount), 0)
                    FROM StatementsOfAccount 
                    WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@cl))";
                cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        summary.TotalInvoices = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                        summary.UnpaidInvoices = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                        summary.TotalBilledAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                    }
                }

                var cmdCr = conn.CreateCommand();
                cmdCr.CommandText = @"
                    SELECT ReceiptNumber, ReceiptDate, COALESCE(SUM(AmountPaid), 0)
                    FROM CollectionReceipts 
                    WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))
                    GROUP BY ReceiptNumber, ReceiptDate
                    ORDER BY ReceiptDate DESC, Id DESC LIMIT 1";
                cmdCr.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var r = cmdCr.ExecuteReader())
                {
                    if (r.Read())
                    {
                        summary.LatestReceiptNumber = r.IsDBNull(0) ? "" : r.GetString(0);
                        summary.LatestReceiptDate = r.IsDBNull(1) ? "" : r.GetString(1);
                        summary.TotalPaidAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                    }
                }
                summary.PaymentStatus = summary.UnpaidInvoices > 0 ? $"{summary.UnpaidInvoices} Pending SOA" : "Up to date";
            }
            return summary;
        }"""

new_get_pay_sum = """        public static ClientPaymentSummary GetClientPaymentSummary(string clientName)
        {
            var summary = new ClientPaymentSummary();
            if (string.IsNullOrWhiteSpace(clientName)) return summary;
            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT 
                            COUNT(*),
                            COUNT(CASE WHEN LOWER(Status) != 'paid' THEN 1 END),
                            COALESCE(SUM(TotalAmount), 0)
                        FROM StatementsOfAccount 
                        WHERE LOWER(TRIM(ClientName)) = LOWER(TRIM(@cl))";
                    cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.TotalInvoices = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                            summary.UnpaidInvoices = r.IsDBNull(1) ? 0 : Convert.ToInt32(r.GetValue(1));
                            summary.TotalBilledAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                        }
                    }

                    var cmdCr = conn.CreateCommand();
                    cmdCr.CommandText = @"
                        SELECT cr.ReceiptNumber, cr.PaymentDate, COALESCE(SUM(cr.AmountPaid), 0)
                        FROM CollectionReceipts cr
                        LEFT JOIN Invoices inv ON (cr.InvoiceId = inv.Id OR cr.InvoiceNumber = inv.InvoiceNumber)
                        WHERE LOWER(TRIM(inv.ClientName)) = LOWER(TRIM(@cl))
                        GROUP BY cr.ReceiptNumber, cr.PaymentDate
                        ORDER BY cr.PaymentDate DESC, cr.Id DESC LIMIT 1";
                    cmdCr.Parameters.AddWithValue("@cl", clientName.Trim());
                    using (var r = cmdCr.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            summary.LatestReceiptNumber = r.IsDBNull(0) ? "" : r.GetString(0);
                            summary.LatestReceiptDate = r.IsDBNull(1) ? "" : r.GetString(1);
                            summary.TotalPaidAmount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                        }
                    }
                    summary.PaymentStatus = summary.UnpaidInvoices > 0 ? $"{summary.UnpaidInvoices} Pending SOA" : "Up to date";
                }
            }
            catch
            {
            }
            return summary;
        }"""

assert old_get_pay_sum in mjdb, "old_get_pay_sum not found in MJdatabase.cs"
mjdb = mjdb.replace(old_get_pay_sum, new_get_pay_sum)

with open(mjdb_path, "w", encoding="utf-8") as f:
    f.write(mjdb)

print("MJdatabase.cs updated successfully.")

dispatch_path = r"Views/DispatchManagementView.cs"
with open(dispatch_path, "r", encoding="utf-8") as f:
    disp = f.read()

old_dispatch_new_route = """        private void OpenDeliveryRoutesForNewRoute()
        {
            MainForm.Instance?.NavigateToDeliveryRoutes(
                routeCode: null,
                isNewRoute: true,
                returnTarget: "Dispatch",
                backAction: () => ReturnFromDeliveryRoutes("Dispatch"));
        }"""

new_dispatch_new_route = """        private void OpenDeliveryRoutesForNewRoute()
        {
            BtnAddRoute_Click(this, EventArgs.Empty);
        }"""

assert old_dispatch_new_route in disp, "old_dispatch_new_route not found"
disp = disp.replace(old_dispatch_new_route, new_dispatch_new_route)

with open(dispatch_path, "w", encoding="utf-8") as f:
    f.write(disp)

print("DispatchManagementView.cs updated successfully.")

loading_path = r"Views/LoadingManagementView.cs"
with open(loading_path, "r", encoding="utf-8") as f:
    load = f.read()

old_load_dest_panel = """            Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            btnAddStop.Dock = DockStyle.Right;
            btnAddStop.Width = 140;
            pnlDestContainer.Controls.Add(cbDestination);
            pnlDestContainer.Controls.Add(btnAddStop);"""

new_load_dest_panel = """            btnAddRoute = new Button
            {
                Text = "+",
                Dock = DockStyle.Right,
                Width = 36,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Bold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddRoute.FlatAppearance.BorderSize = 0;
            btnAddRoute.Click += BtnAddRoute_Click;

            Panel pnlDestContainer = new Panel { Dock = DockStyle.Fill };
            cbDestination.Dock = DockStyle.Fill;
            btnAddStop.Dock = DockStyle.Right;
            btnAddStop.Width = 140;
            pnlDestContainer.Controls.Add(cbDestination);
            pnlDestContainer.Controls.Add(btnAddRoute);
            pnlDestContainer.Controls.Add(btnAddStop);"""

assert old_load_dest_panel in load, "old_load_dest_panel not found"
load = load.replace(old_load_dest_panel, new_load_dest_panel)

old_load_apply_layout = "private void ApplyTwoColumnFormLayout()"
new_load_apply_layout = "private Button btnAddRoute = null!;\n\n        private void ApplyTwoColumnFormLayout()"
assert old_load_apply_layout in load, "old_load_apply_layout not found"
load = load.replace(old_load_apply_layout, new_load_apply_layout, 1)

old_load_new_route = """        private void OpenDeliveryRoutesForNewRoute()
        {
            MainForm.Instance?.NavigateToDeliveryRoutes(
                routeCode: null,
                isNewRoute: true,
                returnTarget: "Loading",
                backAction: () => ReturnFromDeliveryRoutes("Loading"));
        }"""

new_load_new_route = """        private void BtnAddRoute_Click(object? sender, EventArgs e)
        {
            string currentClient = cbClient.Text.Trim();
            using var dlg = new QuickAddRouteDialog(currentClient);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.CreatedRoute != null)
            {
                var route = dlg.CreatedRoute;
                if (!string.IsNullOrWhiteSpace(route.ClientCompanyName) && cbClient.Items.Contains(route.ClientCompanyName))
                {
                    cbClient.SelectedItem = route.ClientCompanyName;
                }
                ReloadRoutesList();
                string targetText = $"{route.RouteCode} - {route.RouteName}";
                if (cbDestination.Items.Contains(targetText))
                {
                    cbDestination.SelectedItem = targetText;
                }
                else
                {
                    cbDestination.Text = targetText;
                }
                PopulateRouteStoresGrid(route);
            }
        }

        private void OpenDeliveryRoutesForNewRoute()
        {
            BtnAddRoute_Click(this, EventArgs.Empty);
        }"""

assert old_load_new_route in load, "old_load_new_route not found"
load = load.replace(old_load_new_route, new_load_new_route)

with open(loading_path, "w", encoding="utf-8") as f:
    f.write(load)

print("LoadingManagementView.cs updated successfully.")
