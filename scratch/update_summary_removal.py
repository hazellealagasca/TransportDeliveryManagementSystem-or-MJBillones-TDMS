with open(r'Views\LoadingManagementView.Designer.cs', 'r', encoding='utf-8') as f:
    load_designer = f.read()

target1 = "            pnlGridCard.Controls.Add(pnlSummary);"
assert target1 in load_designer, "target1 not found in LoadingManagementView.Designer.cs"
load_designer = load_designer.replace(target1, "", 1)

with open(r'Views\LoadingManagementView.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(load_designer)

with open(r'Views\DispatchManagementView.Designer.cs', 'r', encoding='utf-8') as f:
    disp_designer = f.read()

target2 = "            pnlGridCard.Controls.Add(pnlSummary);"
assert target2 in disp_designer, "target2 not found in DispatchManagementView.Designer.cs"
disp_designer = disp_designer.replace(target2, "", 1)

with open(r'Views\DispatchManagementView.Designer.cs', 'w', encoding='utf-8') as f:
    f.write(disp_designer)

print("Both Designer files updated to remove pnlSummary from pnlGridCard!")
