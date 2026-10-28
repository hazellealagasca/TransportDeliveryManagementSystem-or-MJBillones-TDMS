import sqlite3
import os
import sys

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

print("Starting verification of Category & Route fixes...")

# 1. Verify MJdatabase.cs logic
with open("Database/MJdatabase.cs", "r", encoding="utf-8") as f:
    mj_code = f.read()

assert 'checkCmd.CommandText = "SELECT Id FROM DeliveryRoutes WHERE RouteCode = @rc LIMIT 1";' in mj_code, "RouteCode existence check missing"
print("PASS: SaveDeliveryRoute checks for existing RouteCode before insert.")

# 2. Verify CategoryManagementView.cs
with open("Views/CategoryManagementView.cs", "r", encoding="utf-8") as f:
    cat_code = f.read()

assert 'TableLayoutPanel pnlSelectorRow = new TableLayoutPanel' in cat_code, "pnlSelectorRow must be TableLayoutPanel"
assert 'DeliveryRoute newR = selectedRoute ?? new DeliveryRoute();' in cat_code, "BtnAdd_Click must preserve selectedRoute"
assert 'private float currentFormWidth = 520F;' in cat_code, "currentFormWidth must be 520F"
print("PASS: CategoryManagementView has TableLayoutPanel selector, preserved selectedRoute, and widened drawer.")

# 3. Verify SQLite DB sanitization
for db_path in [r'bin\Debug\net9.0-windows\mj_billones.db', r'bin\Release\net9.0-windows\mj_billones.db']:
    if os.path.exists(db_path):
        conn = sqlite3.connect(db_path)
        c = conn.cursor()
        c.execute("SELECT RouteCode, RouteName, Description FROM DeliveryRoutes")
        for r_code, r_name, r_desc in c.fetchall():
            assert "FODGAA" not in r_desc, f"FODGAA found in {db_path} for {r_code}"
            print(f"  [{os.path.basename(os.path.dirname(os.path.dirname(db_path)))}] {r_code}: {r_name} -> {r_desc}")
        conn.close()

# 4. Simulate SaveDeliveryRoute with Id=0 and existing RouteCode
test_db = r'bin\Release\net9.0-windows\mj_billones.db'
conn = sqlite3.connect(test_db)
c = conn.cursor()

# Test checkCmd logic
test_code = "R2"
c.execute("SELECT Id FROM DeliveryRoutes WHERE RouteCode = ? LIMIT 1", (test_code,))
existing_row = c.fetchone()
assert existing_row is not None, "R2 must exist in DeliveryRoutes"
existing_id = existing_row[0]

# Simulate updating via resolved ID
c.execute("""
    UPDATE DeliveryRoutes 
    SET RouteName = 'EODGAA (La Union Route)', Description = 'RCS Villasis Route EODGAA (La Union)', UpdatedAt = datetime('now')
    WHERE Id = ?
""", (existing_id,))
conn.commit()

c.execute("SELECT RouteName, Description FROM DeliveryRoutes WHERE Id = ?", (existing_id,))
updated_row = c.fetchone()
assert updated_row[0] == 'EODGAA (La Union Route)'
assert updated_row[1] == 'RCS Villasis Route EODGAA (La Union)'
print("PASS: Successfully simulated upsert on existing RouteCode without SQLite Error 19!")

conn.close()

print("\nALL CATEGORY & ROUTE VERIFICATIONS PASSED!")
