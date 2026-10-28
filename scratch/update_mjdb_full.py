import os
import re

db_path = os.path.join("Database", "MJdatabase.cs")
with open(db_path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Update CREATE TABLE IF NOT EXISTS DeliveryRoutes if not already updated
tbl_needle = "CREATE TABLE IF NOT EXISTS DeliveryRoutes (\n\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,"
tbl_needle_crlf = "CREATE TABLE IF NOT EXISTS DeliveryRoutes (\r\n\r\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,"
if "ClientCompanyName TEXT NOT NULL DEFAULT ''," in text:
    print("CREATE TABLE DeliveryRoutes already has ClientCompanyName")
elif tbl_needle in text:
    replacement = "CREATE TABLE IF NOT EXISTS DeliveryRoutes (\n\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\n\n                        ClientCompanyName TEXT NOT NULL DEFAULT '',"
    text = text.replace(tbl_needle, replacement, 1)
elif tbl_needle_crlf in text:
    replacement = "CREATE TABLE IF NOT EXISTS DeliveryRoutes (\r\n\r\n                        Id INTEGER PRIMARY KEY AUTOINCREMENT,\r\n\r\n                        ClientCompanyName TEXT NOT NULL DEFAULT '',"
    text = text.replace(tbl_needle_crlf, replacement, 1)
else:
    raise Exception("DeliveryRoutes table definition not matched")

# 2. Add migrations if not present
mig_target = "\"ALTER TABLE StatementsOfAccount ADD COLUMN ReceivedBy2 TEXT DEFAULT 'Ma''am Francis Pauline Rodillas';\","
if "ALTER TABLE DeliveryRoutes ADD COLUMN ClientCompanyName" not in text:
    assert mig_target in text, "mig_target not found"
    new_mig = mig_target + '\n\n                    "ALTER TABLE DeliveryRoutes ADD COLUMN ClientCompanyName TEXT DEFAULT \'\';",\n\n                    "UPDATE DeliveryRoutes SET ClientCompanyName = \'BCS GROUP COMPANY\' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = \'\';",'
    text = text.replace(mig_target, new_mig, 1)
else:
    print("Migration already present")

# 3. Update EnsureDefaultDeliveryRoutes
r1_target = "UPDATE DeliveryRoutes SET RouteCode = 'R1' WHERE RouteCode = 'RT-001';"
if "UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY'" not in text:
    assert r1_target in text, "r1_target not found"
    text = text.replace(r1_target, "UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';\n\n                    " + r1_target, 1)

ins_needle = 'ins.CommandText = "INSERT INTO DeliveryRoutes (RouteCode, RouteName, Description, ConnectedPlaces, IsActive) VALUES (@code, @name, @desc, @places, 1)";'
if ins_needle in text:
    new_ins = 'ins.CommandText = "INSERT INTO DeliveryRoutes (ClientCompanyName, RouteCode, RouteName, Description, ConnectedPlaces, IsActive) VALUES (@client, @code, @name, @desc, @places, 1)";\n\n                    ins.Parameters.AddWithValue("@client", "BCS GROUP COMPANY");'
    text = text.replace(ins_needle, new_ins, 1)

# 4. Replace GetDeliveryRoutes, GetDeliveryRoutesAsync, SaveDeliveryRoute
m_route_block = re.search(r'public static List<DeliveryRoute> GetDeliveryRoutes\(bool activeOnly = false\)[\s\S]*?(?=public static bool DeleteDeliveryRoute)', text)
if m_route_block:
    new_route_block = """public static List<DeliveryRoute> GetDeliveryRoutes(bool activeOnly = false)

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

                        ClientCompanyName = reader.IsDBNull(8) ? "" : reader.GetString(8)

                    });

                }

            }

            return list;

        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesAsync(bool activeOnly = false)

        {

            return await Task.Run(() => GetDeliveryRoutes(activeOnly));

        }

        public static List<DeliveryRoute> GetDeliveryRoutesByClient(string clientCompanyName, bool activeOnly = false)

        {

            var list = new List<DeliveryRoute>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                string sql = "SELECT Id, RouteCode, RouteName, Description, ConnectedPlaces, IsActive, CreatedAt, UpdatedAt, ClientCompanyName FROM DeliveryRoutes WHERE 1=1";

                if (!string.IsNullOrWhiteSpace(clientCompanyName))

                {

                    sql += " AND (ClientCompanyName = @client OR ClientCompanyName = '' OR ClientCompanyName IS NULL)";

                    cmd.Parameters.AddWithValue("@client", clientCompanyName.Trim());

                }

                if (activeOnly)

                {

                    sql += " AND IsActive = 1";

                }

                sql += " ORDER BY RouteName ASC";

                cmd.CommandText = sql;

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

                        UpdatedAt = uAt,

                        ClientCompanyName = reader.IsDBNull(8) ? "" : reader.GetString(8)

                    });

                }

            }

            return list;

        }

        public static async Task<List<DeliveryRoute>> GetDeliveryRoutesByClientAsync(string clientCompanyName, bool activeOnly = false)

        {

            return await Task.Run(() => GetDeliveryRoutesByClient(clientCompanyName, activeOnly));

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

                cmd.Parameters.AddWithValue("@client", r.ClientCompanyName ?? "");

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

        """
    text = text.replace(m_route_block.group(0), new_route_block, 1)

# 5. Add TruckCategory and CrewRole methods before closing class MJdatabase
if "public static List<TruckCategoryItem> GetTruckCategories()" not in text:
    extra_methods = """
        public static List<TruckCategoryItem> GetTruckCategories()

        {

            var list = new List<TruckCategoryItem>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT lc.Id, lc.ItemValue, COALESCE(lc.ExtraValue, '300'),

                                           (SELECT COUNT(*) FROM Trucks t WHERE TRIM(LOWER(t.TruckType)) = TRIM(LOWER(lc.ItemValue)))

                                    FROM LookupCategories lc

                                    WHERE lc.CategoryType = 'Truck Type / Category' OR lc.CategoryType = 'TruckType'

                                    ORDER BY lc.ItemValue ASC";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    int cap = 300;

                    string extra = reader.IsDBNull(2) ? "300" : reader.GetString(2);

                    int.TryParse(extra, out cap);

                    list.Add(new TruckCategoryItem

                    {

                        Id = reader.GetInt32(0),

                        CategoryName = reader.GetString(1),

                        CrateCapacity = cap > 0 ? cap : 300,

                        AssignedTrucksCount = reader.GetInt32(3)

                    });

                }

            }

            return list;

        }

        public static bool SaveTruckCategory(string categoryName, int crateCapacity, string oldCategoryName = null)

        {

            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                if (!string.IsNullOrWhiteSpace(oldCategoryName) && !oldCategoryName.Trim().Equals(categoryName.Trim(), StringComparison.OrdinalIgnoreCase))

                {

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @newVal, ExtraValue = @cap WHERE (CategoryType = 'Truck Type / Category' OR CategoryType = 'TruckType') AND ItemValue = @oldVal";

                    cmd.Parameters.AddWithValue("@newVal", categoryName.Trim());

                    cmd.Parameters.AddWithValue("@cap", crateCapacity.ToString());

                    cmd.Parameters.AddWithValue("@oldVal", oldCategoryName.Trim());

                    cmd.ExecuteNonQuery();

                    using var updTrucks = conn.CreateCommand();

                    updTrucks.CommandText = "UPDATE Trucks SET TruckType = @newVal WHERE TruckType = @oldVal";

                    updTrucks.Parameters.AddWithValue("@newVal", categoryName.Trim());

                    updTrucks.Parameters.AddWithValue("@oldVal", oldCategoryName.Trim());

                    updTrucks.ExecuteNonQuery();

                }

                else

                {

                    SaveLookupItemWithExtra("Truck Type / Category", categoryName.Trim(), crateCapacity.ToString());

                }

                return true;

            }

        }

        public static bool DeleteTruckCategory(string categoryName)

        {

            if (string.IsNullOrWhiteSpace(categoryName)) return false;

            bool r1 = DeleteLookupItem("Truck Type / Category", categoryName.Trim());

            bool r2 = DeleteLookupItem("TruckType", categoryName.Trim());

            return r1 || r2;

        }

        public static List<CrewRoleItem> GetCrewRoles()

        {

            var list = new List<CrewRoleItem>();

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = @"SELECT lc.Id, lc.ItemValue,

                                           (SELECT COUNT(*) FROM Crews c WHERE TRIM(LOWER(c.Role)) = TRIM(LOWER(lc.ItemValue)) AND c.IsActive = 1)

                                    FROM LookupCategories lc

                                    WHERE lc.CategoryType = 'Crew Role'

                                    ORDER BY lc.ItemValue ASC";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())

                {

                    list.Add(new CrewRoleItem

                    {

                        Id = reader.GetInt32(0),

                        RoleName = reader.GetString(1),

                        ActiveCrewCount = reader.GetInt32(2)

                    });

                }

            }

            return list;

        }

        public static bool SaveCrewRole(string roleName, string oldRoleName = null)

        {

            if (string.IsNullOrWhiteSpace(roleName)) return false;

            using (var conn = new SqliteConnection(connectionString))

            {

                conn.Open();

                if (!string.IsNullOrWhiteSpace(oldRoleName) && !oldRoleName.Trim().Equals(roleName.Trim(), StringComparison.OrdinalIgnoreCase))

                {

                    using var cmd = conn.CreateCommand();

                    cmd.CommandText = "UPDATE LookupCategories SET ItemValue = @newVal WHERE CategoryType = 'Crew Role' AND ItemValue = @oldVal";

                    cmd.Parameters.AddWithValue("@newVal", roleName.Trim());

                    cmd.Parameters.AddWithValue("@oldVal", oldRoleName.Trim());

                    cmd.ExecuteNonQuery();

                    using var updCrew = conn.CreateCommand();

                    updCrew.CommandText = "UPDATE Crews SET Role = @newVal WHERE Role = @oldVal";

                    updCrew.Parameters.AddWithValue("@newVal", roleName.Trim());

                    updCrew.Parameters.AddWithValue("@oldVal", oldRoleName.Trim());

                    updCrew.ExecuteNonQuery();

                }

                else

                {

                    SaveLookupItem("Crew Role", roleName.Trim());

                }

                return true;

            }

        }

        public static bool DeleteCrewRole(string roleName)

        {

            if (string.IsNullOrWhiteSpace(roleName)) return false;

            return DeleteLookupItem("Crew Role", roleName.Trim());

        }
"""
    last_brace_idx = text.rfind("}")
    assert last_brace_idx != -1
    second_last_brace = text.rfind("}", 0, last_brace_idx)
    assert second_last_brace != -1
    text = text[:second_last_brace] + extra_methods + text[second_last_brace:]

with open(db_path, "w", encoding="utf-8") as f:
    f.write(text)

print("MJdatabase.cs updated successfully!")
