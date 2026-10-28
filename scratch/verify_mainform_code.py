import re

with open('MainForm.cs', 'r', encoding='utf-8') as f:
    text = f.read()

checks = [
    ('pnlNavFlyout field', 'private Panel? pnlNavFlyout;'),
    ('tmrNavFlyoutGrace field', 'private System.Windows.Forms.Timer? tmrNavFlyoutGrace;'),
    ('currentHoveredNavBtn field', 'private Button? currentHoveredNavBtn;'),
    ('SetupNavFlyoutMenus call', 'SetupNavFlyoutMenus();'),
    ('HideNavFlyout call on main content', 'HideNavFlyout();'),
    ('SetupNavFlyoutMenus method', 'private void SetupNavFlyoutMenus()'),
    ('ShowNavFlyout method', 'private void ShowNavFlyout('),
    ('HideNavFlyout method', 'private void HideNavFlyout()'),
    ('NavigateToOperations method', 'public void NavigateToOperations('),
    ('NavigateToResources method', 'public void NavigateToResources('),
    ('NavigateToFinance method', 'public void NavigateToFinance('),
    ('NavigateToMasterData method', 'public void NavigateToMasterData('),
    ('Operations flyout items', '🚚  Loading Schedule & Dispatch'),
    ('Resources flyout items', '🚛  Trucks'),
    ('Payments flyout items', '📑  Statement of Account'),
    ('Master Data flyout items', '🏢  Categories & Clients'),
]

all_passed = True
for name, target in checks:
    if target in text:
        print(f"  [PASS] {name}")
    else:
        print(f"  [FAIL] {name} - '{target}' not found!")
        all_passed = False

if all_passed:
    print("\nALL MAINFORM FLYOUT CHECKS PASSED!")
else:
    exit(1)
