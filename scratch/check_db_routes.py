import sqlite3

conn = sqlite3.connect(r'bin\Release\net9.0-windows\mj_billones.db')
c = conn.cursor()
c.execute("SELECT name FROM sqlite_master WHERE type='table';")
tables = [r[0] for r in c.fetchall()]
print("Tables:", tables)

if "DeliveryRoutes" in tables:
    c.execute("SELECT Id, RouteCode, RouteName, ClientCompanyName, ConnectedPlaces FROM DeliveryRoutes LIMIT 5;")
    for r in c.fetchall():
        print("Route:", r)
conn.close()
