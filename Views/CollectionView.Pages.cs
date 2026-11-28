using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    /// <summary>
    /// Nov 28: the Collection Receipts tab is two pages. List (default): "Completed Collection receipts (N)", the toolbar with
    /// "New Collection Receipt" and the completed receipts. Creation: Back, "For Collection Receipt (N)" and the waiting list.
    /// The receipt form hides both; closing it always lands on the list page.
    /// </summary>
    public partial class CollectionView
    {
        private enum CollPage { List, Create }
        private CollPage collPage = CollPage.List;
        private bool collFormOpen;
        private Label? lblCollCreateTitle;
        private Panel collBackBar = null!;
        private Button btnCollBack = null!;
        private TableLayoutPanel collHeaderHost = null!;
        private TableLayoutPanel collCreatePage = null!;

        /// <summary>The creation page: row 0 Back bar, row 1 title + description, row 2 the waiting list (a table keeps the order, docking did not).</summary>
        private void BuildCollCreatePage()
        {
            btnCollBack = new Button { Text = "←  Back to collection receipts", Dock = DockStyle.Left, Width = Dpi(250), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9.5F), Cursor = Cursors.Hand };
            btnCollBack.Click += (s, e) => ShowCollListPage();
            UIThemeHelper.SetButtonRole(btnCollBack, "secondary");
            btnCollBack.ForeColor = AppTheme.TextPrimary;
            collBackBar = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Height = Dpi(48), Padding = new Padding(Dpi(16), Dpi(8), 0, Dpi(4)), BackColor = Color.Transparent };
            collBackBar.Controls.Add(btnCollBack);
            lblCollCreateTitle = new Label { AutoSize = true, Font = UITheme.CardHeaderFont, ForeColor = AppTheme.TextPrimary, BackColor = Color.Transparent, UseMnemonic = false, Margin = new Padding(0, 0, 0, Dpi(2)), Text = "For Collection Receipt (0)" };
            var lblDesc = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = AppTheme.TextMuted, BackColor = Color.Transparent, UseMnemonic = false, Margin = Padding.Empty, Text = "Statements and invoices waiting for payment. Pick one, then record the payment in full." };
            collHeaderHost = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = new Padding(Dpi(16), Dpi(4), Dpi(16), Dpi(8)), BackColor = Color.Transparent };
            collHeaderHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            collHeaderHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            collHeaderHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            collHeaderHost.Controls.Add(lblCollCreateTitle, 0, 0);
            collHeaderHost.Controls.Add(lblDesc, 0, 1);
            collReady.Dock = DockStyle.Fill; collReady.Margin = Padding.Empty;
            collReady.Padding = new Padding(Dpi(16), 0, Dpi(16), Dpi(8));
            var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Color.Transparent, Visible = false, Margin = Padding.Empty, Padding = Padding.Empty };
            page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            page.Controls.Add(collBackBar, 0, 0);
            page.Controls.Add(collHeaderHost, 0, 1);
            page.Controls.Add(collReady, 0, 2);
            collCreatePage = page;
            Controls.Add(collCreatePage);
            UpdateCollTitles();
        }

        /// <summary>The one place that decides what is visible: list page, creation page, or the receipt form (which hides both).</summary>
        private void ApplyCollPage()
        {
            if (collCreatePage == null || tblMasterContent == null) return;
            bool create = !collFormOpen && collPage == CollPage.Create;
            SuspendLayout();
            collCreatePage.Visible = create;
            tblMasterContent.Visible = !create; // holds the list card and the form
            if (btnToggleForm != null) btnToggleForm.Visible = true;
            ResumeLayout(true);
        }
        private void ShowCollCreatePage()
        {
            collPage = CollPage.Create;
            RefreshCollReady();
            ApplyCollPage();
        }
        private void ShowCollListPage() { collPage = CollPage.List; ApplyCollPage(); }
    }
}
