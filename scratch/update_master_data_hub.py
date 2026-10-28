with open(r'Views\MasterDataHubView.cs', 'r', encoding='utf-8') as f:
    text = f.read()

old_block = """        public MasterDataHubView(string defaultTab = "Categories")
        {
            InitializeComponent();
            SetupTabs();
            SelectTab(defaultTab);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("Categories", "🏢  Categories & Clients", () => new CategoryManagementView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data History", new[] { "LookupCategories", "Clients", "DeliveryRoutes" }, isAdminOnly: true));
        }"""

new_block = """        public MasterDataHubView(string defaultTab = "Clients")
        {
            InitializeComponent();
            SetupTabs();
            SelectTab(defaultTab);
            UIThemeHelper.ApplyTheme(this);
        }

        private void SetupTabs()
        {
            AddTab("Clients", "🏢  Clients", () => new ClientManagementView());
            AddTab("Routes", "🗺️  Delivery Routes", () => new DeliveryRoutesView());
            AddTab("Categories", "🏷️  Categories", () => new CategoryManagementView());
            AddTab("History", "📜  Master Data History", () => new ModuleHistoryView("Master Data History", new[] { "LookupCategories", "Clients", "DeliveryRoutes" }, isAdminOnly: true));
        }"""

text_norm = text.replace('\r\n', '\n')
old_norm = old_block.replace('\r\n', '\n')
new_norm = new_block.replace('\r\n', '\n')

assert old_norm in text_norm, "old_norm not found"
text_norm = text_norm.replace(old_norm, new_norm, 1)

with open(r'Views\MasterDataHubView.cs', 'w', encoding='utf-8', newline='\r\n') as f:
    f.write(text_norm)

print("MasterDataHubView.cs updated successfully!")
