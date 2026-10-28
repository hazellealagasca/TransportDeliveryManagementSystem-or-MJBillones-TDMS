import re

with open(r'Database\MJdatabase.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update CREATE TABLE IF NOT EXISTS Clients
old_clients_table = """                    CREATE TABLE IF NOT EXISTS Clients (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        CompanyName TEXT UNIQUE NOT NULL,

                        ContactPerson TEXT DEFAULT '',

                        ContactNumber TEXT DEFAULT '',

                        Address TEXT DEFAULT '',

                        Email TEXT DEFAULT ''

                    );"""

new_clients_table = """                    CREATE TABLE IF NOT EXISTS Clients (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        CompanyName TEXT UNIQUE NOT NULL,

                        ContactPerson TEXT DEFAULT '',

                        ContactNumber TEXT DEFAULT '',

                        Address TEXT DEFAULT '',

                        Email TEXT DEFAULT '',

                        TIN TEXT DEFAULT ''

                    );

                    CREATE TABLE IF NOT EXISTS ClientStores (

                        Id INTEGER PRIMARY KEY AUTOINCREMENT,

                        ClientId INTEGER NOT NULL DEFAULT 0,

                        ClientCompanyName TEXT NOT NULL DEFAULT '',

                        StoreCode TEXT NOT NULL DEFAULT '',

                        StoreName TEXT NOT NULL DEFAULT '',

                        DestinationRoute TEXT NOT NULL DEFAULT '',

                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP

                    );"""

assert old_clients_table in code, "old_clients_table not found"
code = code.replace(old_clients_table, new_clients_table, 1)

# 2. Add migrations
old_mig = '"ALTER TABLE Clients ADD COLUMN MiddleName TEXT DEFAULT \'\';",'
new_mig = '''"ALTER TABLE Clients ADD COLUMN MiddleName TEXT DEFAULT '';",

                    "ALTER TABLE Clients ADD COLUMN TIN TEXT DEFAULT '';",

                    "CREATE TABLE IF NOT EXISTS ClientStores (Id INTEGER PRIMARY KEY AUTOINCREMENT, ClientId INTEGER NOT NULL DEFAULT 0, ClientCompanyName TEXT NOT NULL DEFAULT '', StoreCode TEXT NOT NULL DEFAULT '', StoreName TEXT NOT NULL DEFAULT '', DestinationRoute TEXT NOT NULL DEFAULT '', CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP);",'''

assert old_mig in code, "old_mig not found"
code = code.replace(old_mig, new_mig, 1)

# 3. Update GetClients()
old_get_clients_sql = 'cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName FROM Clients ORDER BY CompanyName ASC";'
new_get_clients_sql = 'cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN FROM Clients ORDER BY CompanyName ASC";'
assert old_get_clients_sql in code, "old_get_clients_sql not found"
code = code.replace(old_get_clients_sql, new_get_clients_sql, 1)

old_get_clients_pop = """                            ContactPersonMiddleName = mn

                        });"""

new_get_clients_pop = """                            ContactPersonMiddleName = mn,

                            TIN = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : ""

                        });"""

assert old_get_clients_pop in code, "old_get_clients_pop not found"
code = code.replace(old_get_clients_pop, new_get_clients_pop, 1)

# 4. Update GetClientByCompanyName()
old_get_one_sql = 'cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName FROM Clients WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@name)) LIMIT 1";'
new_get_one_sql = 'cmd.CommandText = "SELECT Id, CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN FROM Clients WHERE LOWER(TRIM(CompanyName)) = LOWER(TRIM(@name)) LIMIT 1";'
assert old_get_one_sql in code, "old_get_one_sql not found"
code = code.replace(old_get_one_sql, new_get_one_sql, 1)

old_get_one_pop = """                            ContactPersonMiddleName = mn

                        };"""

new_get_one_pop = """                            ContactPersonMiddleName = mn,

                            TIN = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetString(9) : ""

                        };"""

assert old_get_one_pop in code, "old_get_one_pop not found"
code = code.replace(old_get_one_pop, new_get_one_pop, 1)

# 5. Update SaveClient()
old_save_sql = """                    INSERT INTO Clients (CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName)

                    VALUES (@name, @person, @contact, @addr, @email, @ln, @fn, @mn)

                    ON CONFLICT(CompanyName) DO UPDATE SET

                        ContactPerson = @person,

                        ContactNumber = @contact,

                        Address = @addr,

                        Email = @email,

                        LastName = @ln,

                        FirstName = @fn,

                        MiddleName = @mn;"""

new_save_sql = """                    INSERT INTO Clients (CompanyName, ContactPerson, ContactNumber, Address, Email, LastName, FirstName, MiddleName, TIN)

                    VALUES (@name, @person, @contact, @addr, @email, @ln, @fn, @mn, @tin)

                    ON CONFLICT(CompanyName) DO UPDATE SET

                        ContactPerson = @person,

                        ContactNumber = @contact,

                        Address = @addr,

                        Email = @email,

                        LastName = @ln,

                        FirstName = @fn,

                        MiddleName = @mn,

                        TIN = @tin;"""

assert old_save_sql in code, "old_save_sql not found"
code = code.replace(old_save_sql, new_save_sql, 1)

old_save_params = """                cmd.Parameters.AddWithValue("@mn", (client.ContactPersonMiddleName ?? "").Trim());

                cmd.ExecuteNonQuery();"""

new_save_params = """                cmd.Parameters.AddWithValue("@mn", (client.ContactPersonMiddleName ?? "").Trim());

                cmd.Parameters.AddWithValue("@tin", (client.TIN ?? "").Trim());

                cmd.ExecuteNonQuery();"""

assert old_save_params in code, "old_save_params not found"
code = code.replace(old_save_params, new_save_params, 1)

# 6. Add ClientStore and Client summary helper methods before DeleteClient
stores_and_summary_methods = """
        public static List<ClientStore> GetClientStores(string clientName)
        {
            var list = new List<ClientStore>();
            if (string.IsNullOrWhiteSpace(clientName)) return list;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute, CreatedAt FROM ClientStores WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@cl)) ORDER BY StoreCode ASC";
                cmd.Parameters.AddWithValue("@cl", clientName.Trim());
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ClientStore
                        {
                            Id = reader.GetInt32(0),
                            ClientId = reader.GetInt32(1),
                            ClientCompanyName = reader.GetString(2),
                            StoreCode = reader.GetString(3),
                            StoreName = reader.GetString(4),
                            DestinationRoute = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            CreatedAt = reader.IsDBNull(6) ? "" : reader.GetString(6)
                        });
                    }
                }
            }
            if (list.Count == 0)
            {
                var routes = GetDeliveryRoutesByClient(clientName, activeOnly: false);
                foreach (var r in routes)
                {
                    if (string.IsNullOrWhiteSpace(r.ConnectedPlaces)) continue;
                    var parts = r.ConnectedPlaces.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int idx = 1;
                    foreach (var p in parts)
                    {
                        string storeName = p.Trim();
                        if (string.IsNullOrWhiteSpace(storeName)) continue;
                        string code = $"{r.RouteCode}-S{idx:D2}";
                        var cs = new ClientStore
                        {
                            ClientId = 0,
                            ClientCompanyName = clientName,
                            StoreCode = code,
                            StoreName = storeName,
                            DestinationRoute = $"{r.RouteCode} - {r.RouteName}"
                        };
                        SaveClientStore(cs);
                        list.Add(cs);
                        idx++;
                    }
                }
            }
            return list;
        }

        public static void SaveClientStore(ClientStore store)
        {
            if (store == null || string.IsNullOrWhiteSpace(store.StoreName)) return;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                if (store.Id > 0)
                {
                    cmd.CommandText = @"UPDATE ClientStores SET 
                        StoreCode = @sc, 
                        StoreName = @sn, 
                        DestinationRoute = @dr, 
                        ClientCompanyName = @cl 
                        WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@id", store.Id);
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO ClientStores 
                        (ClientId, ClientCompanyName, StoreCode, StoreName, DestinationRoute) 
                        VALUES (@cid, @cl, @sc, @sn, @dr)";
                    cmd.Parameters.AddWithValue("@cid", store.ClientId);
                }
                cmd.Parameters.AddWithValue("@cl", (store.ClientCompanyName ?? "").Trim());
                cmd.Parameters.AddWithValue("@sc", (store.StoreCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@sn", (store.StoreName ?? "").Trim());
                cmd.Parameters.AddWithValue("@dr", (store.DestinationRoute ?? "").Trim());
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteClientStore(int storeId)
        {
            if (storeId <= 0) return;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM ClientStores WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", storeId);
                cmd.ExecuteNonQuery();
            }
        }

        public static ClientTripSummary GetClientTripSummary(string clientName)
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
        }

        public static ClientPaymentSummary GetClientPaymentSummary(string clientName)
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
        }

"""

target_before = "        public static void DeleteClient(string companyName)"
assert target_before in code, "target_before not found"
code = code.replace(target_before, stores_and_summary_methods + target_before, 1)

with open(r'Database\MJdatabase.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("MJdatabase.cs successfully updated!")
