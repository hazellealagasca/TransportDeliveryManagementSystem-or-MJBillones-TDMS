Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$dllPath = Resolve-Path "bin\Release\net9.0-windows\Transport Delivery Management System for M.J. Billones Transport Service.dll"
[System.Reflection.Assembly]::LoadFrom($dllPath) | Out-Null

Write-Host "Assembly loaded successfully."

$types = @(
    "Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.MasterDataHubView",
    "Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.CategoryManagementView",
    "Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.TruckManagementView",
    "Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views.CrewManagementView"
)

foreach ($tname in $types) {
    try {
        $t = [Type]::GetType("$tname, Transport Delivery Management System for M.J. Billones Transport Service")
        if ($null -eq $t) {
            Write-Host "Could not find type $tname"
            continue
        }
        $inst = [Activator]::CreateInstance($t)
        Write-Host "Successfully instantiated: $tname"

        # Test at 1366x768
        $f1 = New-Object System.Windows.Forms.Form
        $f1.Size = New-Object System.Drawing.Size(1366, 768)
        $inst.Dock = [System.Windows.Forms.DockStyle]::Fill
        $f1.Controls.Add($inst)
        $f1.Show()
        $f1.Refresh()
        Write-Host "  Rendered at 1366x768 successfully."
        $f1.Close()
        $f1.Dispose()

        # Test at 1920x1080
        $inst2 = [Activator]::CreateInstance($t)
        $f2 = New-Object System.Windows.Forms.Form
        $f2.Size = New-Object System.Drawing.Size(1920, 1080)
        $inst2.Dock = [System.Windows.Forms.DockStyle]::Fill
        $f2.Controls.Add($inst2)
        $f2.Show()
        $f2.Refresh()
        Write-Host "  Rendered at 1920x1080 successfully."
        $f2.Close()
        $f2.Dispose()
    }
    catch {
        Write-Host "Error testing $tname : $_"
    }
}

Write-Host "UI layout test completed!"
