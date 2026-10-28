Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$dllPath = Resolve-Path "bin\Release\net9.0-windows\Transport Delivery Management System for M.J. Billones Transport Service.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)

Write-Host "============================================================"
Write-Host "TDMS CATEGORY MANAGEMENT & ROUTE STORES EDITOR TEST SUITE"
Write-Host "============================================================"

# 1. Type Resolution
$catViewType = $asm.GetType("Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.CategoryManagementView")
if ($null -eq $catViewType) {
    # Try finding in loaded types
    try {
        $catViewType = $asm.GetTypes() | Where-Object { $_.Name -eq "CategoryManagementView" }
    } catch [System.Reflection.ReflectionTypeLoadException] {
        $catViewType = $_.Exception.Types | Where-Object { $null -ne $_ -and $_.Name -eq "CategoryManagementView" }
    }
}
if ($null -eq $catViewType) { throw "CategoryManagementView type not found in assembly!" }
Write-Host "[1] Type resolved: $($catViewType.FullName)"

# 2. Instantiation
$view = [Activator]::CreateInstance($catViewType)
if ($null -eq $view) { throw "Failed to instantiate CategoryManagementView!" }
Write-Host "[2] CategoryManagementView successfully instantiated."

# 3. Control Introspection via Reflection
function Get-PrivateField($target, $name) {
    $field = $target.GetType().GetField($name, [System.Reflection.BindingFlags]"NonPublic, Instance, Public")
    if ($null -eq $field) {
        throw "Expected field '$name' not found on $($target.GetType().Name)!"
    }
    return $field.GetValue($target)
}

$tblMasterContent = Get-PrivateField $view "tblMasterContent"
$pnlForm = Get-PrivateField $view "pnlForm"
$pnlFormBody = Get-PrivateField $view "pnlFormBody"
$pnlGridCard = Get-PrivateField $view "pnlGridCard"
$gridItems = Get-PrivateField $view "gridItems"
$btnToggleForm = Get-PrivateField $view "btnToggleForm"

# Route Controls
$cmbClientRoutes = Get-PrivateField $view "cmbClientRoutes"
$btnNewRoute = Get-PrivateField $view "btnNewRoute"
$txtRouteCode = Get-PrivateField $view "txtRouteCode"
$txtRouteName = Get-PrivateField $view "txtRouteName"
$txtRouteDescription = Get-PrivateField $view "txtRouteDescription"
$gridRouteStores = Get-PrivateField $view "gridRouteStores"
$txtNewStoreCode = Get-PrivateField $view "txtNewStoreCode"
$txtNewStoreName = Get-PrivateField $view "txtNewStoreName"
$btnAddStoreStop = Get-PrivateField $view "btnAddStoreStop"
$btnMoveStoreUp = Get-PrivateField $view "btnMoveStoreUp"
$btnMoveStoreDown = Get-PrivateField $view "btnMoveStoreDown"
$btnRemoveStore = Get-PrivateField $view "btnRemoveStore"
$btnClearStores = Get-PrivateField $view "btnClearStores"
$btnSaveRoute = Get-PrivateField $view "btnSaveRoute"
$btnDeleteRoute = Get-PrivateField $view "btnDeleteRoute"

Write-Host "[3] Layout & Route Controls Verified:"
Write-Host "  - tblMasterContent: Columns = $($tblMasterContent.ColumnCount), Rows = $($tblMasterContent.RowCount)"
Write-Host "  - pnlForm (Drawer): Width = $($pnlForm.Width)"
Write-Host "  - pnlFormBody: AutoScroll = $($pnlFormBody.AutoScroll)"
Write-Host "  - gridItems (Directory): Columns = $($gridItems.Columns.Count)"
Write-Host "  - gridRouteStores: Columns = $($gridRouteStores.Columns.Count) (Names: $($gridRouteStores.Columns[0].HeaderText), $($gridRouteStores.Columns[1].HeaderText), $($gridRouteStores.Columns[2].HeaderText))"

# 4. Host inside 1366x768 form and measure dimensions
$hostForm = New-Object System.Windows.Forms.Form
$hostForm.Size = New-Object System.Drawing.Size(1366, 768)
$view.Dock = [System.Windows.Forms.DockStyle]::Fill
$hostForm.Controls.Add($view)
$hostForm.Show()
$hostForm.Refresh()

Write-Host "[4] Form Layout Metrics at 1366x768:"
Write-Host "  - Host Form Size: $($hostForm.Width) x $($hostForm.Height)"
Write-Host "  - CategoryManagementView Size: $($view.Width) x $($view.Height)"
Write-Host "  - Left Drawer (pnlForm) Size: $($pnlForm.Width) x $($pnlForm.Height)"
Write-Host "  - Right Grid Card (pnlGridCard) Size: $($pnlGridCard.Width) x $($pnlGridCard.Height)"
Write-Host "  - Client Directory (gridItems) Size: $($gridItems.Width) x $($gridItems.Height)"

if ($gridItems.Height -lt 300) {
    throw "Client Directory grid is still cramped! Height is only $($gridItems.Height)"
}
Write-Host "  SUCCESS: Client Directory table has healthy height of $($gridItems.Height)px (not cramped, fully visible)."

# 5. Test Quick-Add Store Stops and Reordering
Write-Host "[5] Testing Route Stores Table Interactions..."

# Add Stop 1
$txtNewStoreCode.Text = "STR-001"
$txtNewStoreName.Text = "Robinsons Metro East"
$btnAddStoreStop.PerformClick()

# Add Stop 2
$txtNewStoreCode.Text = "STR-002"
$txtNewStoreName.Text = "SM Megamall Building A"
$btnAddStoreStop.PerformClick()

# Add Stop 3
$txtNewStoreCode.Text = "STR-003"
$txtNewStoreName.Text = "Glorietta 4 Makati"
$btnAddStoreStop.PerformClick()

if ($gridRouteStores.Rows.Count -ne 3) {
    throw "Expected 3 store rows in gridRouteStores, found $($gridRouteStores.Rows.Count)"
}
Write-Host "  Added 3 store stops:"
for ($i = 0; $i -lt $gridRouteStores.Rows.Count; $i++) {
    $row = $gridRouteStores.Rows[$i]
    Write-Host "    #$($row.Cells['colStoreSeq'].Value) | Code: $($row.Cells['colStoreCode'].Value) | Name: $($row.Cells['colStoreName'].Value)"
}

# Select row 2 (Glorietta) and move it up
$gridRouteStores.ClearSelection()
$gridRouteStores.Rows[2].Selected = $true
$btnMoveStoreUp.PerformClick()

Write-Host "  After Move Up on Row 2:"
for ($i = 0; $i -lt $gridRouteStores.Rows.Count; $i++) {
    $row = $gridRouteStores.Rows[$i]
    Write-Host "    #$($row.Cells['colStoreSeq'].Value) | Code: $($row.Cells['colStoreCode'].Value) | Name: $($row.Cells['colStoreName'].Value)"
}

if ($gridRouteStores.Rows[1].Cells['colStoreCode'].Value -ne "STR-003") {
    throw "Move Up failed! Row 1 should be STR-003"
}
Write-Host "  SUCCESS: Move Up correctly swapped stop order."

# 6. Test Drawer Toggle
Write-Host "[6] Testing Drawer Collapse / Expand..."
Write-Host "  Initial drawer column width: $($tblMasterContent.ColumnStyles[0].Width)"
$btnToggleForm.PerformClick()
Write-Host "  After click toggle (collapsed): column width: $($tblMasterContent.ColumnStyles[0].Width)"
if ($tblMasterContent.ColumnStyles[0].Width -ne 0) {
    throw "Drawer collapse failed! Width is $($tblMasterContent.ColumnStyles[0].Width)"
}
$btnToggleForm.PerformClick()
Write-Host "  After second click (expanded): column width: $($tblMasterContent.ColumnStyles[0].Width)"
if ($tblMasterContent.ColumnStyles[0].Width -le 0) {
    throw "Drawer expand failed! Width is $($tblMasterContent.ColumnStyles[0].Width)"
}
Write-Host "  SUCCESS: Drawer collapse/expand animation toggle works flawlessly."

# 7. Test Store Format Compatibility with DeliveryRoute & LoadingManagementView
Write-Host "[7] Testing Store Format Parsing & Compatibility..."
$dlvRouteType = $asm.GetType("Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models.DeliveryRoute")
if ($null -eq $dlvRouteType) {
    try {
        $dlvRouteType = $asm.GetTypes() | Where-Object { $_.Name -eq "DeliveryRoute" }
    } catch [System.Reflection.ReflectionTypeLoadException] {
        $dlvRouteType = $_.Exception.Types | Where-Object { $null -ne $_ -and $_.Name -eq "DeliveryRoute" }
    }
}
if ($null -eq $dlvRouteType) { throw "DeliveryRoute model not found!" }

$testRoute = [Activator]::CreateInstance($dlvRouteType)
$testRoute.ConnectedPlaces = "STR-001 - Robinsons Metro East; STR-003 - Glorietta 4 Makati; STR-002 - SM Megamall Building A"

$placesListProp = $dlvRouteType.GetProperty("PlacesList")
$places = $placesListProp.GetValue($testRoute)
Write-Host "  Places parsed count: $($places.Count)"

$splitMethod = $dlvRouteType.GetMethod("SplitStoreCodeAndName", [System.Reflection.BindingFlags]"Public, Static")
foreach ($p in $places) {
    $tuple = $splitMethod.Invoke($null, @($p))
    Write-Host "    Raw: '$p' => Code: '$($tuple.Item1)', Name: '$($tuple.Item2)'"
    if ([string]::IsNullOrWhiteSpace($tuple.Item1) -or [string]::IsNullOrWhiteSpace($tuple.Item2)) {
        throw "Failed to parse code or name from '$p'"
    }
}
Write-Host "  SUCCESS: SplitStoreCodeAndName properly decodes stored routes into Loading Schedule store breakdown!"

$hostForm.Close()
$hostForm.Dispose()

Write-Host "============================================================"
Write-Host "ALL CATEGORY MANAGEMENT & ROUTE STORES EDITOR TESTS PASSED!"
Write-Host "============================================================"
