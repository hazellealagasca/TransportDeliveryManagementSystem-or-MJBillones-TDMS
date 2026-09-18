namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    partial class CategoryManagementForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblCatType = new System.Windows.Forms.Label();
            this.cbCategoryType = new System.Windows.Forms.ComboBox();
            this.lblVal = new System.Windows.Forms.Label();
            this.txtItemValue = new System.Windows.Forms.TextBox();
            this.lblContactPerson = new System.Windows.Forms.Label();
            this.txtContactPerson = new System.Windows.Forms.TextBox();
            this.lblContactNumber = new System.Windows.Forms.Label();
            this.txtContactNumber = new System.Windows.Forms.TextBox();
            this.lblCapacity = new System.Windows.Forms.Label();
            this.numCapacity = new System.Windows.Forms.NumericUpDown();
            this.lblAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.gridItems = new System.Windows.Forms.DataGridView();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.tblMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pnlFormCard = new System.Windows.Forms.Panel();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.tblFormFields = new System.Windows.Forms.TableLayoutPanel();
            this.flowActions = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlRow1Left = new System.Windows.Forms.Panel();
            this.pnlRow1Right = new System.Windows.Forms.Panel();
            this.pnlRow2Col1 = new System.Windows.Forms.Panel();
            this.pnlRow2Col2 = new System.Windows.Forms.Panel();
            this.pnlRow2Col3 = new System.Windows.Forms.Panel();
            this.pnlRow3Addr = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.numCapacity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridItems)).BeginInit();
            this.pnlHeader.SuspendLayout();
            this.tblMainLayout.SuspendLayout();
            this.pnlFormCard.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            this.pnlBottom.SuspendLayout();
            this.tblFormFields.SuspendLayout();
            this.flowActions.SuspendLayout();
            this.pnlRow1Left.SuspendLayout();
            this.pnlRow1Right.SuspendLayout();
            this.pnlRow2Col1.SuspendLayout();
            this.pnlRow2Col2.SuspendLayout();
            this.pnlRow2Col3.SuspendLayout();
            this.pnlRow3Addr.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.AutoSize = true;
            this.pnlHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(20, 16, 20, 12);
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.lblTitle);

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(420, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Dropdown Categories & Client Company Manager";

            this.tblMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMainLayout.ColumnCount = 1;
            this.tblMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMainLayout.RowCount = 2;
            this.tblMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblMainLayout.Controls.Add(this.pnlFormCard, 0, 0);
            this.tblMainLayout.Controls.Add(this.pnlGridCard, 0, 1);
            this.tblMainLayout.Padding = new System.Windows.Forms.Padding(20, 12, 20, 12);
            this.tblMainLayout.Name = "tblMainLayout";

            this.pnlFormCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFormCard.AutoSize = true;
            this.pnlFormCard.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlFormCard.BackColor = System.Drawing.Color.White;
            this.pnlFormCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlFormCard.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.pnlFormCard.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.pnlFormCard.Name = "pnlFormCard";
            this.pnlFormCard.Controls.Add(this.tblFormFields);

            this.tblFormFields.Dock = System.Windows.Forms.DockStyle.Top;
            this.tblFormFields.AutoSize = true;
            this.tblFormFields.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tblFormFields.ColumnCount = 3;
            this.tblFormFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblFormFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblFormFields.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tblFormFields.RowCount = 4;
            this.tblFormFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFormFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFormFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tblFormFields.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));

            this.pnlRow1Left.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow1Left.AutoSize = true;
            this.pnlRow1Left.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
            this.pnlRow1Left.Controls.Add(this.cbCategoryType);
            this.pnlRow1Left.Controls.Add(this.lblCatType);

            this.lblCatType.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCatType.AutoSize = true;
            this.lblCatType.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblCatType.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblCatType.Name = "lblCatType";
            this.lblCatType.Size = new System.Drawing.Size(155, 15);
            this.lblCatType.Text = "Select Category to Manage:";

            this.cbCategoryType.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbCategoryType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCategoryType.FormattingEnabled = true;
            this.cbCategoryType.Items.AddRange(new object[] { "Client Company", "Destination Route", "Truck Type / Category", "Crew Role", "Payment Method" });
            this.cbCategoryType.Name = "cbCategoryType";
            this.cbCategoryType.SelectedIndexChanged += new System.EventHandler(this.CbCategoryType_SelectedIndexChanged);

            this.pnlRow1Right.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow1Right.AutoSize = true;
            this.pnlRow1Right.Margin = new System.Windows.Forms.Padding(8, 0, 0, 8);
            this.pnlRow1Right.Controls.Add(this.txtItemValue);
            this.pnlRow1Right.Controls.Add(this.lblVal);

            this.lblVal.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVal.AutoSize = true;
            this.lblVal.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblVal.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblVal.Name = "lblVal";
            this.lblVal.Size = new System.Drawing.Size(126, 15);
            this.lblVal.Text = "Client Company Name:";

            this.txtItemValue.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtItemValue.Name = "txtItemValue";

            this.tblFormFields.Controls.Add(this.pnlRow1Left, 0, 0);
            this.tblFormFields.SetColumnSpan(this.pnlRow1Right, 2);
            this.tblFormFields.Controls.Add(this.pnlRow1Right, 1, 0);

            this.pnlRow2Col1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow2Col1.AutoSize = true;
            this.pnlRow2Col1.Margin = new System.Windows.Forms.Padding(0, 0, 8, 8);
            this.pnlRow2Col1.Controls.Add(this.txtContactPerson);
            this.pnlRow2Col1.Controls.Add(this.lblContactPerson);

            this.lblContactPerson.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblContactPerson.AutoSize = true;
            this.lblContactPerson.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblContactPerson.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblContactPerson.Name = "lblContactPerson";
            this.lblContactPerson.Size = new System.Drawing.Size(124, 15);
            this.lblContactPerson.Text = "Contact Person Name:";

            this.txtContactPerson.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtContactPerson.Name = "txtContactPerson";

            this.pnlRow2Col2.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow2Col2.AutoSize = true;
            this.pnlRow2Col2.Margin = new System.Windows.Forms.Padding(8, 0, 8, 8);
            this.pnlRow2Col2.Controls.Add(this.txtContactNumber);
            this.pnlRow2Col2.Controls.Add(this.lblContactNumber);

            this.lblContactNumber.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblContactNumber.AutoSize = true;
            this.lblContactNumber.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblContactNumber.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblContactNumber.Name = "lblContactNumber";
            this.lblContactNumber.Size = new System.Drawing.Size(130, 15);
            this.lblContactNumber.Text = "Contact Phone / Mobile:";

            this.txtContactNumber.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtContactNumber.Name = "txtContactNumber";

            this.pnlRow2Col3.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow2Col3.AutoSize = true;
            this.pnlRow2Col3.Margin = new System.Windows.Forms.Padding(8, 0, 0, 8);
            this.pnlRow2Col3.Controls.Add(this.numCapacity);
            this.pnlRow2Col3.Controls.Add(this.lblCapacity);

            this.lblCapacity.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCapacity.AutoSize = true;
            this.lblCapacity.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblCapacity.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblCapacity.Name = "lblCapacity";
            this.lblCapacity.Size = new System.Drawing.Size(88, 15);
            this.lblCapacity.Text = "Crate Capacity:";
            this.lblCapacity.Visible = false;

            this.numCapacity.Dock = System.Windows.Forms.DockStyle.Top;
            this.numCapacity.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.numCapacity.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numCapacity.Name = "numCapacity";
            this.numCapacity.Value = new decimal(new int[] { 300, 0, 0, 0 });
            this.numCapacity.Visible = false;

            this.tblFormFields.Controls.Add(this.pnlRow2Col1, 0, 1);
            this.tblFormFields.Controls.Add(this.pnlRow2Col2, 1, 1);
            this.tblFormFields.Controls.Add(this.pnlRow2Col3, 2, 1);

            this.pnlRow3Addr.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRow3Addr.AutoSize = true;
            this.pnlRow3Addr.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.pnlRow3Addr.Controls.Add(this.txtAddress);
            this.pnlRow3Addr.Controls.Add(this.lblAddress);

            this.lblAddress.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAddress.AutoSize = true;
            this.lblAddress.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F);
            this.lblAddress.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(148, 15);
            this.lblAddress.Text = "Address / Branch Location:";

            this.txtAddress.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtAddress.Name = "txtAddress";

            this.tblFormFields.SetColumnSpan(this.pnlRow3Addr, 3);
            this.tblFormFields.Controls.Add(this.pnlRow3Addr, 0, 2);

            this.flowActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowActions.AutoSize = true;
            this.flowActions.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.flowActions.WrapContents = true;
            this.flowActions.Margin = new System.Windows.Forms.Padding(0);

            this.btnAdd.BackColor = System.Drawing.Color.FromArgb(37, 99, 235);
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.FlatAppearance.BorderSize = 0;
            this.btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(90, 30);
            this.btnAdd.Text = "+ ADD";
            this.btnAdd.UseVisualStyleBackColor = false;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);

            this.btnUpdate.BackColor = System.Drawing.Color.FromArgb(22, 163, 74);
            this.btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpdate.FlatAppearance.BorderSize = 0;
            this.btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnUpdate.ForeColor = System.Drawing.Color.White;
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(90, 30);
            this.btnUpdate.Text = "UPDATE";
            this.btnUpdate.UseVisualStyleBackColor = false;
            this.btnUpdate.Click += new System.EventHandler(this.BtnUpdate_Click);

            this.btnDelete.BackColor = System.Drawing.Color.FromArgb(239, 68, 68);
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.FlatAppearance.BorderSize = 0;
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnDelete.ForeColor = System.Drawing.Color.White;
            this.btnDelete.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(95, 30);
            this.btnDelete.Text = "- REMOVE";
            this.btnDelete.UseVisualStyleBackColor = false;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            this.btnClear.BackColor = System.Drawing.Color.FromArgb(100, 116, 139);
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Margin = new System.Windows.Forms.Padding(0);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(85, 30);
            this.btnClear.Text = "CLEAR";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);

            this.flowActions.Controls.Add(this.btnAdd);
            this.flowActions.Controls.Add(this.btnUpdate);
            this.flowActions.Controls.Add(this.btnDelete);
            this.flowActions.Controls.Add(this.btnClear);

            this.tblFormFields.SetColumnSpan(this.flowActions, 3);
            this.tblFormFields.Controls.Add(this.flowActions, 0, 3);

            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(12);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Controls.Add(this.gridItems);

            this.gridItems.AllowUserToAddRows = false;
            this.gridItems.AllowUserToDeleteRows = false;
            this.gridItems.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridItems.BackgroundColor = System.Drawing.Color.White;
            this.gridItems.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.gridItems.ColumnHeadersHeight = 35;
            this.gridItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridItems.EnableHeadersVisualStyles = false;
            this.gridItems.Name = "gridItems";
            this.gridItems.ReadOnly = true;
            this.gridItems.RowHeadersVisible = false;
            this.gridItems.RowTemplate.Height = 32;
            this.gridItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridItems.SelectionChanged += new System.EventHandler(this.GridItems_SelectionChanged);

            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Height = 48;
            this.pnlBottom.Padding = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.pnlBottom.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            this.pnlBottom.Controls.Add(this.btnClose);

            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(148, 163, 184);
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(130, 32);
            this.btnClose.Text = "DONE / CLOSE";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);

            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.MinimumSize = new System.Drawing.Size(680, 500);
            this.Controls.Add(this.tblMainLayout);
            this.Controls.Add(this.pnlBottom);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "CategoryManagementForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Dropdown Categories & Clients";
            ((System.ComponentModel.ISupportInitialize)(this.numCapacity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridItems)).EndInit();
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.tblMainLayout.ResumeLayout(false);
            this.tblMainLayout.PerformLayout();
            this.pnlFormCard.ResumeLayout(false);
            this.pnlFormCard.PerformLayout();
            this.pnlGridCard.ResumeLayout(false);
            this.pnlBottom.ResumeLayout(false);
            this.tblFormFields.ResumeLayout(false);
            this.tblFormFields.PerformLayout();
            this.flowActions.ResumeLayout(false);
            this.pnlRow1Left.ResumeLayout(false);
            this.pnlRow1Left.PerformLayout();
            this.pnlRow1Right.ResumeLayout(false);
            this.pnlRow1Right.PerformLayout();
            this.pnlRow2Col1.ResumeLayout(false);
            this.pnlRow2Col1.PerformLayout();
            this.pnlRow2Col2.ResumeLayout(false);
            this.pnlRow2Col2.PerformLayout();
            this.pnlRow2Col3.ResumeLayout(false);
            this.pnlRow2Col3.PerformLayout();
            this.pnlRow3Addr.ResumeLayout(false);
            this.pnlRow3Addr.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.TableLayoutPanel tblMainLayout;
        private System.Windows.Forms.Panel pnlFormCard;
        private System.Windows.Forms.TableLayoutPanel tblFormFields;
        private System.Windows.Forms.Panel pnlRow1Left;
        private System.Windows.Forms.Label lblCatType;
        private System.Windows.Forms.ComboBox cbCategoryType;
        private System.Windows.Forms.Panel pnlRow1Right;
        private System.Windows.Forms.Label lblVal;
        private System.Windows.Forms.TextBox txtItemValue;
        private System.Windows.Forms.Panel pnlRow2Col1;
        private System.Windows.Forms.Label lblContactPerson;
        private System.Windows.Forms.TextBox txtContactPerson;
        private System.Windows.Forms.Panel pnlRow2Col2;
        private System.Windows.Forms.Label lblContactNumber;
        private System.Windows.Forms.TextBox txtContactNumber;
        private System.Windows.Forms.Panel pnlRow2Col3;
        private System.Windows.Forms.Label lblCapacity;
        private System.Windows.Forms.NumericUpDown numCapacity;
        private System.Windows.Forms.Panel pnlRow3Addr;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.FlowLayoutPanel flowActions;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.DataGridView gridItems;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnClose;
    }
}
