with open(r'MainForm.cs', 'r', encoding='utf-8') as f:
    code = f.read()

old_flyout = """                        ("🏢  Categories & Clients", () => NavigateToMasterData("Categories")),
                        ("📜  Master Data History", () => NavigateToMasterData("History"))"""

new_flyout = """                        ("🏢  Clients", () => NavigateToMasterData("Clients")),
                        ("🗺️  Delivery Routes", () => NavigateToMasterData("Routes")),
                        ("🏷️  Categories", () => NavigateToMasterData("Categories")),
                        ("📜  Master Data History", () => NavigateToMasterData("History"))"""

old_nav = 'public void NavigateToMasterData(string tab = "Categories")'
new_nav = 'public void NavigateToMasterData(string tab = "Clients")'

code_norm = code.replace('\r\n', '\n')
old_f_norm = old_flyout.replace('\r\n', '\n')
new_f_norm = new_flyout.replace('\r\n', '\n')

assert old_f_norm in code_norm, "old_flyout not found in MainForm.cs"
assert old_nav in code_norm, "old_nav not found in MainForm.cs"

code_norm = code_norm.replace(old_f_norm, new_f_norm, 1)
code_norm = code_norm.replace(old_nav, new_nav, 1)

with open(r'MainForm.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(code_norm)

print("MainForm.cs updated with new Master Data navigation!")
