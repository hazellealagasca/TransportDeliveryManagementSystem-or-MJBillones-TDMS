import sqlite3
import os

paths = [
    r"bin\Debug\net9.0-windows\mj_billones.db",
    r"bin\Release\net9.0-windows\mj_billones.db",
    "mj_billones.db"
]

for p in paths:
    if os.path.exists(p):
        print("Checking DB at:", p)
        conn = sqlite3.connect(p)
        c = conn.cursor()
        c.execute("SELECT name FROM sqlite_master WHERE type='table';")
        tables = [t[0] for t in c.fetchall()]
        print("  Tables:", len(tables), tables[:8])
        if "DeliveryRoutes" in tables:
            c.execute("PRAGMA table_info(DeliveryRoutes)")
            cols = [col[1] for col in c.fetchall()]
            print("  DeliveryRoutes cols:", cols)
            if "ClientCompanyName" not in cols:
                c.execute("ALTER TABLE DeliveryRoutes ADD COLUMN ClientCompanyName TEXT DEFAULT '';")
                c.execute("UPDATE DeliveryRoutes SET ClientCompanyName = 'BCS GROUP COMPANY' WHERE ClientCompanyName IS NULL OR TRIM(ClientCompanyName) = '';")
                conn.commit()
                print("  Migration applied directly to", p)
            c.execute("SELECT Id, ClientCompanyName, RouteCode, RouteName FROM DeliveryRoutes LIMIT 3")
            print("  Sample routes:", c.fetchall())
        conn.close()
