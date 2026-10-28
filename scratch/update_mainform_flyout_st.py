with open('MainForm.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '                        ("✅  Completed Dispatches", () => NavigateToOperations("Completed")),'
replacement = '                        ("✅  Completed Dispatches", () => NavigateToOperations("Completed")),\n                        ("🌟  Special Trip History", () => NavigateToOperations("SpecialTrips")),'

if target in text:
    text = text.replace(target, replacement, 1)
    print("Replaced with LF")
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), replacement.replace('\n', '\r\n'), 1)
    print("Replaced with CRLF")
else:
    raise Exception("target not found")

with open('MainForm.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("SUCCESS: MainForm.cs updated with SpecialTripHistory flyout button!")
