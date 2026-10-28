import os

path = os.path.join("Database", "MJdatabase.cs")
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

old_tbl = """CREATE TABLE IF NOT EXISTS DeliveryRoutes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RouteCode TEXT UNIQUE NOT NULL,"""
new_tbl = """CREATE TABLE IF NOT EXISTS DeliveryRoutes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ClientCompanyName TEXT NOT NULL DEFAULT '',
                        RouteCode TEXT UNIQUE NOT NULL,"""
assert old_tbl in text, "old_tbl not found"
text = text.replace(old_tbl, new_tbl, 1)

old_mig = """"ALTER TABLE StatementsOfAccount ADD COLUMN ReceivedBy2 TEXT DEFAULT 'Ma''am Francis Pauline Rodillas';", """
if old_mig not in text:
    old_mig = """"ALTER TABLE StatementsOfAccount ADD COLUMN ReceivedBy2 TEXT DEFAULT 'Ma''am Francis Pauline Rodillas';","""
assert old_mig in text, "old_mig not found"
new_mig = old_mig + """
                    "ALTER TABLE DeliveryRoutes ADD COLUMN ClientCompanyName TEXT DEFAULT '';",
                    "UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';","""
text = text.replace(old_mig, new_mig, 1)

old_ensure = """ins.CommandText = "INSERT INTO DeliveryRoutes (RouteCode, RouteName, Description, ConnectedPlaces, IsActive) VALUES (@code, @name, @desc, @places, 1)";"""
new_ensure = """ins.CommandText = "INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive) VALUES (@client, @code, @name, @desc, @places, 1)";
                    ins.Parameters.AddWithValue("@client", "BCS GROUP COMPANY");"""
assert old_ensure in text, "old_ensure not found"
text = text.replace(old_ensure, new_ensure, 1)

old_upd = """UPDATE DeliveryRoutes SET RouteCode = 'R1' WHERE RouteCode = 'RT-001';"""
new_upd = """UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';
                    UPDATE DeliveryRoutes SET RouteCode = 'R1' WHERE RouteCode = 'RT-001';"""
assert old_upd in text, "old_upd not found"
text = text.replace(old_upd, new_upd, 1)

old_get = """        public static List<DeliveryRoute> GetDeliveryRoutes(bool activeOnly = false)
        {
            var list = new List<DeliveryRoute>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = activeOnly
                    ? "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt FROM DeliveryRoutes WHERE IsActive = 1 ORDER BY RouteName ASC"
                    : "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt FROM DeliveryRoutes ORDER BY RouteName ASC";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime cAt = DateTime.TryParse(reader.GetString(6), out var cdt) ? cdt : DateTime.Now;
                    DateTime uAt = DateTime.TryParse(reader.GetString(7), out var udt) ? udt : DateTime.Now;
                    list.Add(new DeliveryRoute
                    {
                        Id = reader.GetInt32(0),
                        RouteCode = reader.GetString(1),
                        RouteName = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        ConnectedPlaces = reader.GetString(4),
                        IsActive = reader.GetInt32(5) == 1,
                        CreatedAt = cAt,
                        UpdatedAt = uAt
                    });
                }
            }
            return list;
        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesAsync(bool activeOnly = false)
        {
            return await Task.Run(() => GetDeliveryRoutes(activeOnly));
        }

        public static bool SaveDeliveryRoute(DeliveryRoute r)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                if (r.Id == 0)
                {
                    cmd.CommandText = @"INSERT INTO DeliveryRoutes (RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt)
                                        VALUES (@c, @n, @d, @p, @a, datetime('now'), datetime('now'));
                                        SELECT last_insert_rowid();";
                }
                else
                {
                    cmd.CommandText = @"UPDATE DeliveryRoutes
                                        SET RouteCode = @c, RouteName = @n, Description = @d, ConnectedPlaces = @p, IsActive = @a, UpdatedAt = datetime('now')
                                        WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", r.Id);
                }
                cmd.Parameters.AddWithValue("@c", r.RouteCode);
                cmd.Parameters.AddWithValue("@n", r.RouteName);
                cmd.Parameters.AddWithValue("@d", r.Description ?? "");
                cmd.Parameters.AddWithValue("@p", r.ConnectedPlaces);
                cmd.Parameters.AddWithValue("@a", r.IsActive ? 1 : 0);
                if (r.Id == 0)
                {
                    r.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
        }"""

new_get = """        public static List<DeliveryRoute> GetDeliveryRoutes(bool activeOnly = false)
        {
            var list = new List<DeliveryRoute>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = activeOnly
                    ? "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE IsActive = 1 ORDER BY RouteName ASC"
                    : "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes ORDER BY RouteName ASC";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime cAt = DateTime.TryParse(reader.GetString(6), out var cdt) ? cdt : DateTime.Now;
                    DateTime uAt = DateTime.TryParse(reader.GetString(7), out var udt) ? udt : DateTime.Now;
                    string clientName = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : "BCS GROUP COMPANY";
                    list.Add(new DeliveryRoute
                    {
                        Id = reader.GetInt32(0),
                        RouteCode = reader.GetString(1),
                        RouteName = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        ConnectedPlaces = reader.GetString(4),
                        IsActive = reader.GetInt32(5) == 1,
                        CreatedAt = cAt,
                        UpdatedAt = uAt,
                        ClientCompanyName = clientName
                    });
                }
            }
            return list;
        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesAsync(bool activeOnly = false)
        {
            return await Task.Run(() => GetDeliveryRoutes(activeOnly));
        }

        public static List<DeliveryRoute> GetDeliveryRoutesByClient(string clientName, bool activeOnly = false)
        {
            var list = new List<DeliveryRoute>();
            if (string.IsNullOrWhiteSpace(clientName)) return list;
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = activeOnly
                    ? "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE IsActive = 1 AND LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@client)) ORDER BY RouteName ASC"
                    : "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE LOWER(TRIM(ClientCompanyName)) = LOWER(TRIM(@client)) ORDER BY RouteName ASC";
                cmd.Parameters.AddWithValue("@client", clientName.Trim());
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime cAt = DateTime.TryParse(reader.GetString(6), out var cdt) ? cdt : DateTime.Now;
                    DateTime uAt = DateTime.TryParse(reader.GetString(7), out var udt) ? udt : DateTime.Now;
                    string cName = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetString(8) : clientName;
                    list.Add(new DeliveryRoute
                    {
                        Id = reader.GetInt32(0),
                        RouteCode = reader.GetString(1),
                        RouteName = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        ConnectedPlaces = reader.GetString(4),
                        IsActive = reader.GetInt32(5) == 1,
                        CreatedAt = cAt,
                        UpdatedAt = uAt,
                        ClientCompanyName = cName
                    });
                }
            }
            return list;
        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesByClientAsync(string clientName, bool activeOnly = false)
        {
            return await Task.Run(() => GetDeliveryRoutesByClient(clientName, activeOnly));
        }

        public static bool SaveDeliveryRoute(DeliveryRoute r)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                if (r.Id == 0)
                {
                    cmd.CommandText = @"INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt)
                                        VALUES (@client, @c, @n, @d, @p, @a, datetime('now'), datetime('now'));
                                        SELECT last_insert_rowid();";
                }
                else
                {
                    cmd.CommandText = @"UPDATE DeliveryRoutes
                                        SET ClientCompanyName = @client, RouteCode = @c, RouteName = @n, Description = @d, ConnectedPlaces = @p, IsActive = @a, UpdatedAt = datetime('now')
                                        WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", r.Id);
                }
                cmd.Parameters.AddWithValue("@client", string.IsNullOrWhiteSpace(r.ClientCompanyName) ? "BCS GROUP COMPANY" : r.ClientCompanyName.Trim());
                cmd.Parameters.AddWithValue("@c", r.RouteCode);
                cmd.Parameters.AddWithValue("@n", r.RouteName);
                cmd.Parameters.AddWithValue("@d", r.Description ?? "");
                cmd.Parameters.AddWithValue("@p", r.ConnectedPlaces);
                cmd.Parameters.AddWithValue("@a", r.IsActive ? 1 : 0);
                if (r.Id == 0)
                {
                    r.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }
                else
                {
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
        }

        public static List<TruckCategoryItem> GetTruckCategories()
        {
            var list = new List<TruckCategoryItem>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, ItemValue, ExtraValue FROM LookupCategories WHERE CategoryType IN ('Truck Type / Category', 'TruckType') ORDER BY ItemValue ASC";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int cap = 300;
                    if (!reader.IsDBNull(2) && int.TryParse(reader.GetString(2), out int parsedCap)) cap = parsedCap;
                    list.Add(new TruckCategoryItem
                    {
                        Id = reader.GetInt32(0),
                        CategoryName = reader.GetString(1),
                        CrateCapacity = cap
                    });
                }
            }
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                foreach (var item in list)
                {
                    using var cmdCount = conn.CreateCommand();
                    cmdCount.CommandText = "SELECT COUNT(*) FROM Trucks WHERE LOWER(TRIM(TruckType)) = LOWER(TRIM(@t))";
                    cmdCount.Parameters.AddWithValue("@t", item.CategoryName);
                    item.AssignedTrucksCount = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
                }
            }
            return list;
        }

        public static bool SaveTruckCategory(string name, int capacity, int id = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (id > 0)
            {
                cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @v, ExtraValue = @e WHERE Id = @id AND CategoryType IN ('Truck Type / Category', 'TruckType')";
                cmd.Parameters.AddWithValue("@id", id);
            }
            else
            {
                cmd.CommandText = "INSERT INTO LookupCategories (CategoryType, ItemValue, ExtraValue) VALUES ('Truck Type / Category', @v, @e)";
            }
            cmd.Parameters.AddWithValue("@v", name.Trim());
            cmd.Parameters.AddWithValue("@e", capacity.ToString());
            cmd.ExecuteNonQuery();
            OnCategoriesChanged?.Invoke();
            return true;
        }

        public static bool DeleteTruckCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM LookupCategories WHERE CategoryType IN ('Truck Type / Category', 'TruckType') AND LOWER(TRIM(ItemValue)) = LOWER(TRIM(@v))";
            cmd.Parameters.AddWithValue("@v", name.Trim());
            cmd.ExecuteNonQuery();
            OnCategoriesChanged?.Invoke();
            return true;
        }

        public static List<CrewRoleItem> GetCrewRoles()
        {
            var list = new List<CrewRoleItem>();
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, ItemValue FROM LookupCategories WHERE CategoryType = 'Crew Role' ORDER BY ItemValue ASC";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CrewRoleItem
                    {
                        Id = reader.GetInt32(0),
                        RoleName = reader.GetString(1)
                    });
                }
            }
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                foreach (var item in list)
                {
                    using var cmdCount = conn.CreateCommand();
                    cmdCount.CommandText = "SELECT COUNT(*) FROM Crews WHERE LOWER(TRIM(Role)) = LOWER(TRIM(@r))";
                    cmdCount.Parameters.AddWithValue("@r", item.RoleName);
                    item.ActiveCrewCount = Convert.ToInt32(cmdCount.ExecuteScalar() ?? 0);
                }
            }
            return list;
        }

        public static bool SaveCrewRole(string roleName, int id = 0)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            if (id > 0)
            {
                cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @v WHERE Id = @id AND CategoryType = 'Crew Role'";
                cmd.Parameters.AddWithValue("@id", id);
            }
            else
            {
                cmd.CommandText = "INSERT INTO LookupCategories (CategoryType, ItemValue) VALUES ('Crew Role', @v)";
            }
            cmd.Parameters.AddWithValue("@v", roleName.Trim());
            cmd.ExecuteNonQuery();
            OnCategoriesChanged?.Invoke();
            return true;
        }

        public static bool DeleteCrewRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM LookupCategories WHERE CategoryType = 'Crew Role' AND LOWER(TRIM(ItemValue)) = LOWER(TRIM(@v))";
            cmd.Parameters.AddWithValue("@v", roleName.Trim());
            cmd.ExecuteNonQuery();
            OnCategoriesChanged?.Invoke();
            return true;
        }"""

assert old_get in text, "old_get not found"
text = text.replace(old_get, new_get, 1)

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("Updated Database/MJdatabase.cs successfully")
