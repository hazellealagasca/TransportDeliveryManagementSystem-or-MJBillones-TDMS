import re
import sys

with open('Views/CategoryManagementView.cs', 'r', encoding='utf-8') as f:
    text = f.read()

checks = [
    # 2-column Master Layout & Drawer
    ('tblMasterContent definition', 'private TableLayoutPanel tblMasterContent = null!;'),
    ('pnlForm drawer definition', 'private Panel pnlForm = null!;'),
    ('pnlFormBody scrollable panel', 'private Panel pnlFormBody = null!;'),
    ('pnlGridCard definition', 'private Panel pnlGridCard = null!;'),
    ('pnlGridToolbar definition', 'private Panel pnlGridToolbar = null!;'),
    ('btnToggleForm button definition', 'private Button btnToggleForm = null!;'),
    ('SetFormVisibility method', 'public void SetFormVisibility(bool visible)'),
    ('AttachSideBySideFormResizer usage', 'UIThemeHelper.AttachSideBySideFormResizer(tblMasterContent, pnlForm, pnlFormBody, gridItems, currentFormWidth, w => currentFormWidth = w);'),
    ('AutoScroll set on Form Body', 'AutoScroll = true,'),

    # Client Route & Stores Editor Fields
    ('pnlClientRoutesSection definition', 'private Panel pnlClientRoutesSection = null!;'),
    ('cmbClientRoutes definition', 'private ComboBox cmbClientRoutes = null!;'),
    ('btnNewRoute definition', 'private Button btnNewRoute = null!;'),
    ('txtRouteCode definition', 'private TextBox txtRouteCode = null!;'),
    ('txtRouteName definition', 'private TextBox txtRouteName = null!;'),
    ('txtRouteDescription definition', 'private TextBox txtRouteDescription = null!;'),
    ('chkRouteActive definition', 'private CheckBox chkRouteActive = null!;'),
    ('gridRouteStores definition', 'private DataGridView gridRouteStores = null!;'),
    ('txtNewStoreCode definition', 'private TextBox txtNewStoreCode = null!;'),
    ('txtNewStoreName definition', 'private TextBox txtNewStoreName = null!;'),
    ('btnAddStoreStop definition', 'private Button btnAddStoreStop = null!;'),
    ('btnMoveStoreUp definition', 'private Button btnMoveStoreUp = null!;'),
    ('btnMoveStoreDown definition', 'private Button btnMoveStoreDown = null!;'),
    ('btnRemoveStore definition', 'private Button btnRemoveStore = null!;'),
    ('btnClearStores definition', 'private Button btnClearStores = null!;'),
    ('btnSaveRoute definition', 'private Button btnSaveRoute = null!;'),
    ('btnDeleteRoute definition', 'private Button btnDeleteRoute = null!;'),

    # Methods
    ('BuildClientRoutesSection method', 'private Panel BuildClientRoutesSection()'),
    ('LoadClientRoutes method', 'private void LoadClientRoutes(string clientName, string? routeToSelect = null)'),
    ('CmbClientRoutes_SelectedIndexChanged method', 'private void CmbClientRoutes_SelectedIndexChanged(object? sender, EventArgs e)'),
    ('PopulateRouteFields method', 'private void PopulateRouteFields(DeliveryRoute? r)'),
    ('ClearRouteFields method', 'private void ClearRouteFields()'),
    ('UpdateRouteStoresCountLabel method', 'private void UpdateRouteStoresCountLabel()'),
    ('BtnNewRoute_Click handler', 'private void BtnNewRoute_Click(object? sender, EventArgs e)'),
    ('BtnAddStoreStop_Click handler', 'private void BtnAddStoreStop_Click(object? sender, EventArgs e)'),
    ('BtnMoveStoreUp_Click handler', 'private void BtnMoveStoreUp_Click(object? sender, EventArgs e)'),
    ('BtnMoveStoreDown_Click handler', 'private void BtnMoveStoreDown_Click(object? sender, EventArgs e)'),
    ('BtnRemoveStore_Click handler', 'private void BtnRemoveStore_Click(object? sender, EventArgs e)'),
    ('BtnClearStores_Click handler', 'private void BtnClearStores_Click(object? sender, EventArgs e)'),
    ('BtnSaveRoute_Click handler', 'private void BtnSaveRoute_Click(object? sender, EventArgs e)'),
    ('BtnDeleteRoute_Click handler', 'private void BtnDeleteRoute_Click(object? sender, EventArgs e)'),

    # Store format compatibility: "Code - Name; ..."
    ('Store format generation', 'string.IsNullOrWhiteSpace(sc) ? sn : $"{sc} - {sn}"'),
    ('ConnectedPlaces join', 'routeToSave.ConnectedPlaces = string.Join("; ", placesList);'),
    ('Auto-populate on client grid selection', 'LoadClientRoutes(val);'),
]

print("=== VERIFYING CATEGORY MANAGEMENT VIEW REFACTORING ===")
all_passed = True
for name, target in checks:
    if target in text:
        print(f"  [PASS] {name}")
    else:
        print(f"  [FAIL] {name} - '{target}' not found!")
        all_passed = False

if all_passed:
    print(f"\nALL {len(checks)} CATEGORY MANAGEMENT REFACTORING CHECKS PASSED SUCCESSFULLY!")
else:
    sys.exit(1)
