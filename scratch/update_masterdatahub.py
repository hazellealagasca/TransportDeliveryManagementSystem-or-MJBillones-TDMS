import os

path = os.path.join("Views", "MasterDataHubView.cs")
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# Replace constructor defaultTab
text = text.replace('public MasterDataHubView(string defaultTab = "Rates")', 'public MasterDataHubView(string defaultTab = "Categories")', 1)

# Replace SetupTabs
old_setup = """        private void SetupTabs()
        {
            AddTab("Rates", "📈  Rate Matrix", () => new RateMatrixView());
            AddTab("Routes", "🛣️  Route of Deliveries", () => new DeliveryRoutesView());
            AddTab("Categories", "🏢  Categories & Clients", () => new CategoryManagementView());
            AddTab("Reports", "📊  Reports & Analytics", () => new ReportsView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data & Rates History", new[] { "RateMatrix", "LookupCategories", "Clients" }, isAdminOnly: true));
        }"""
new_setup = """        private void SetupTabs()
        {
            AddTab("Categories", "🏢  Categories & Clients", () => new CategoryManagementView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data History", new[] { "LookupCategories", "Clients", "DeliveryRoutes" }, isAdminOnly: true));
        }"""

# Handle both CRLF and LF
if old_setup in text:
    text = text.replace(old_setup, new_setup, 1)
elif old_setup.replace("\n", "\r\n") in text:
    text = text.replace(old_setup.replace("\n", "\r\n"), new_setup.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_setup not found")

# Replace GetDeliveryRoutesView with GetCategoryManagementView
old_routes_view = """        public DeliveryRoutesView? GetDeliveryRoutesView()
        {
            if (cachedViews.TryGetValue("Routes", out var v) && v is DeliveryRoutesView drv)
            {
                return drv;
            }
            return null;
        }"""
new_cat_view = """        public CategoryManagementView? GetCategoryManagementView()
        {
            if (cachedViews.TryGetValue("Categories", out var v) && v is CategoryManagementView cmv)
            {
                return cmv;
            }
            return null;
        }"""

if old_routes_view in text:
    text = text.replace(old_routes_view, new_cat_view, 1)
elif old_routes_view.replace("\n", "\r\n") in text:
    text = text.replace(old_routes_view.replace("\n", "\r\n"), new_cat_view.replace("\n", "\r\n"), 1)
else:
    raise Exception("old_routes_view not found")

with open(path, "w", encoding="utf-8") as f:
    f.write(text)

print("MasterDataHubView.cs updated successfully!")
