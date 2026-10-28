path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = '''"UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'Franchise ', '') WHERE Description LIKE '%Franchise%';",
                    "UPDATE DeliveryRoutes SET Description = REPLACE(Description, 'FODGAA', 'EODGAA') WHERE Description LIKE '%FODGAA%';",'''

rep = '''"UPDATE DeliveryRoutes SET Description = 'RCS Villasis Route EODLAB (Ilocos Norte & Ilocos Sur)' WHERE RouteCode = 'R1';",
                    "UPDATE DeliveryRoutes SET Description = 'RCS Villasis Route EODGAA (La Union)' WHERE RouteCode = 'R2';",'''

if target in text:
    text = text.replace(target, rep, 1)
    print("Replaced migration with direct update (LF)")
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), rep.replace('\n', '\r\n'), 1)
    print("Replaced migration with direct update (CRLF)")
else:
    raise Exception("Target not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("MJdatabase migration sanitized!")
