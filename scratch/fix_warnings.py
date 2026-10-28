# Fix ClientManagementView warning
with open(r'Views\ClientManagementView.cs', 'r', encoding='utf-8') as f:
    client_code = f.read()

old_dest = "defaultDest = colDest.Items[0].ToString() ?? \"\";"
new_dest = "defaultDest = colDest.Items[0]?.ToString() ?? \"\";"

assert old_dest in client_code, "old_dest not found"
client_code = client_code.replace(old_dest, new_dest, 1)

with open(r'Views\ClientManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(client_code)

# Fix DispatchManagementView warning
with open(r'Views\DispatchManagementView.cs', 'r', encoding='utf-8') as f:
    disp_code = f.read()

old_badges_ret = """            if (pnlSummary == null) return;

            pnlSummary.Visible = false;
            return;

            lblSummary.Visible = false;"""

new_badges_ret = """            if (pnlSummary == null) return;

            pnlSummary.Visible = false;

            lblSummary.Visible = false;"""

assert old_badges_ret in disp_code, "old_badges_ret not found"
disp_code = disp_code.replace(old_badges_ret, new_badges_ret, 1)

with open(r'Views\DispatchManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(disp_code)

print("Warnings fixed successfully!")
