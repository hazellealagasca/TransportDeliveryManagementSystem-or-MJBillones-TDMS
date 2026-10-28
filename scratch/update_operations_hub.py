with open('Views/OperationsHubView.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '            AddTab("Incidents", "⚠️  Incident & Damage Logs",'
replacement = '            AddTab("SpecialTrips", "🌟  Special Trip History", () => new SpecialTripHistoryView());\n\n            AddTab("Incidents", "⚠️  Incident & Damage Logs",'

if target in text:
    text = text.replace(target, replacement, 1)
    print("Replaced with LF")
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), replacement.replace('\n', '\r\n'), 1)
    print("Replaced with CRLF")
else:
    raise Exception("target not found")

with open('Views/OperationsHubView.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("SUCCESS: OperationsHubView.cs updated with SpecialTrips tab!")
