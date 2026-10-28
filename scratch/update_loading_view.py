with open(r'Views\LoadingManagementView.cs', 'r', encoding='utf-8') as f:
    code = f.read()

old_ctor = """        public LoadingManagementView()
        {
            InitializeComponent();
            gridSchedules.BringToFront();"""

new_ctor = """        public LoadingManagementView()
        {
            InitializeComponent();
            if (pnlSummary != null)
            {
                pnlSummary.Visible = false;
                pnlGridCard.Controls.Remove(pnlSummary);
            }
            gridSchedules.BringToFront();"""

assert old_ctor in code, "old_ctor not found in LoadingManagementView.cs"
code = code.replace(old_ctor, new_ctor, 1)

with open(r'Views\LoadingManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print("LoadingManagementView.cs updated to remove and hide pnlSummary!")
