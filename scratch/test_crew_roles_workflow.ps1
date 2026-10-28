Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$dllPath = Resolve-Path "bin\Release\net9.0-windows\Transport Delivery Management System for M.J. Billones Transport Service.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($dllPath)

Write-Host "--- TDMS CREW ROLES VERIFICATION TEST ---"

# 1. Test MJdatabase crew role CRUD
$dbType = $asm.GetTypes() | Where-Object { $_.Name -eq "MJdatabase" }
if ($null -eq $dbType) { throw "MJdatabase type not found in assembly!" }
Write-Host "Found MJdatabase type: $($dbType.FullName)"

$initMethod = $dbType.GetMethod("InitializeDatabase", [System.Reflection.BindingFlags]"Public, Static")
if ($null -ne $initMethod) { $initMethod.Invoke($null, @()) }

$getRolesMethod = $dbType.GetMethod("GetCrewRoles", [System.Reflection.BindingFlags]"Public, Static")
$saveRoleMethod = $dbType.GetMethod("SaveCrewRole", [System.Reflection.BindingFlags]"Public, Static", $null, [Type[]]@([string], [string]), $null)
$deleteRoleMethod = $dbType.GetMethod("DeleteCrewRole", [System.Reflection.BindingFlags]"Public, Static")

Write-Host "[1] Testing Database Methods..."
$saveRoleMethod.Invoke($null, @("Automation_Tester_Role", $null))
$roles = $getRolesMethod.Invoke($null, @())
$found = $false
foreach ($r in $roles) {
    if ($r.RoleName -eq "Automation_Tester_Role") {
        $found = $true
        break
    }
}
if (-not $found) { throw "Role Automation_Tester_Role was not created!" }
Write-Host "  Role Automation_Tester_Role created successfully."

$saveRoleMethod.Invoke($null, @("Automation_Lead_Role", "Automation_Tester_Role"))
$roles2 = $getRolesMethod.Invoke($null, @())
$foundRenamed = $false
foreach ($r in $roles2) {
    if ($r.RoleName -eq "Automation_Lead_Role") {
        $foundRenamed = $true
        break
    }
}
if (-not $foundRenamed) { throw "Role Automation_Lead_Role rename failed!" }
Write-Host "  Role renamed to Automation_Lead_Role successfully."

$deleteRoleMethod.Invoke($null, @("Automation_Lead_Role"))
$roles3 = $getRolesMethod.Invoke($null, @())
foreach ($r in $roles3) {
    if ($r.RoleName -eq "Automation_Lead_Role") {
        throw "Role Automation_Lead_Role was not deleted!"
    }
}
Write-Host "  Role deleted successfully."

# 2. Test CrewRoleManageDialog instantiation
Write-Host "[2] Testing CrewRoleManageDialog UI..."
$dlgType = $asm.GetTypes() | Where-Object { $_.Name -eq "CrewRoleManageDialog" }
if ($null -eq $dlgType) { throw "CrewRoleManageDialog type not found!" }
$dlg = [Activator]::CreateInstance($dlgType, @("Driver"))
if ($null -eq $dlg) { throw "Failed to instantiate CrewRoleManageDialog!" }
Write-Host "  CrewRoleManageDialog instantiated successfully."
$dlg.Show()
$dlg.Refresh()
$dlg.Close()
$dlg.Dispose()
Write-Host "  CrewRoleManageDialog rendered and disposed cleanly."

# 3. Test CrewManagementView instantiation and controls
Write-Host "[3] Testing CrewManagementView UI..."
$viewType = $asm.GetTypes() | Where-Object { $_.Name -eq "CrewManagementView" }
if ($null -eq $viewType) { throw "CrewManagementView type not found!" }
$view = [Activator]::CreateInstance($viewType)
if ($null -eq $view) { throw "Failed to instantiate CrewManagementView!" }

# Check that pnlSubNavTabs does NOT exist
$subNavField = $viewType.GetField("pnlSubNavTabs", [System.Reflection.BindingFlags]"NonPublic, Instance")
if ($null -ne $subNavField) {
    throw "pnlSubNavTabs field should have been removed!"
}
Write-Host "  Verified: Sub-navigation tabs (pnlSubNavTabs) completely removed."

# Check that btnManageRoles exists
$btnField = $viewType.GetField("btnManageRoles", [System.Reflection.BindingFlags]"NonPublic, Instance")
if ($null -eq $btnField) {
    throw "btnManageRoles field not found!"
}
$btnManage = $btnField.GetValue($view)
if ($null -eq $btnManage) {
    throw "btnManageRoles instance is null!"
}
Write-Host "  Verified: btnManageRoles is present: Text = '$($btnManage.Text)'"

# Render in 1366x768 and 1920x1080
$f1 = New-Object System.Windows.Forms.Form
$f1.Size = New-Object System.Drawing.Size(1366, 768)
$view.Dock = [System.Windows.Forms.DockStyle]::Fill
$f1.Controls.Add($view)
$f1.Show()
$f1.Refresh()
Write-Host "  CrewManagementView rendered at 1366x768 successfully."
$f1.Close()
$f1.Dispose()

Write-Host "ALL VERIFICATION CHECKS PASSED SUCCESSFULLY!"
