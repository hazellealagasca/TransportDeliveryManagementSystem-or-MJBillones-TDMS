with open('Program.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = 'var dbPath = Database.MJdatabase.DbFilePath;\n                sb.AppendLine($"DB Path: {dbPath}");\n\n                var routes = Database.MJdatabase.GetDeliveryRoutes();'
target_crlf = target.replace('\n', '\r\n')

replacement = 'var dbPath = Database.MJdatabase.DbFilePath;\n                sb.AppendLine($"DB Path: {dbPath}");\n\n                Database.MJdatabase.InitializeDatabase();\n                sb.AppendLine("Database initialized and migrated successfully.");\n\n                var routes = Database.MJdatabase.GetDeliveryRoutes();'
replacement_crlf = replacement.replace('\n', '\r\n')

if target in text:
    text = text.replace(target, replacement, 1)
elif target_crlf in text:
    text = text.replace(target_crlf, replacement_crlf, 1)
else:
    raise Exception("target not found")

with open('Program.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("Program.cs updated with InitializeDatabase()!")
