import sqlite3
import os

paths = [
    r"bin\Release\net9.0-windows\mj_billones.db",
    "mj_billones.db"
]

for p in paths:
    if os.path.exists(p):
        print(f"--- DB Check: {p} ---")
        conn = sqlite3.connect(p)
        c = conn.cursor()
        c.execute("SELECT Id, CategoryType, ItemValue FROM LookupCategories WHERE CategoryType = 'Crew Role' ORDER BY ItemValue ASC;")
        roles = c.fetchall()
        print(f"Total Crew Roles: {len(roles)}")
        for r in roles:
            # Check how many active crew have this role
            role_name = r[2]
            c.execute("SELECT COUNT(*) FROM Crews WHERE TRIM(LOWER(Role)) = TRIM(LOWER(?)) AND IsActive = 1", (role_name,))
            active_count = c.fetchone()[0]
            print(f"  - Id {r[0]}: '{role_name}' -> {active_count} active crew")
        conn.close()
