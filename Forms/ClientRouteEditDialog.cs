using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Forms
{
    public class ClientRouteEditDialog : Form
    {
        public DeliveryRoute CurrentRoute { get; private set; }
        private readonly string clientCompanyName;
        private readonly bool isEditMode;

        private TextBox txtClient = null!;
        private TextBox txtRouteCode = null!;
        private TextBox txtRouteName = null!;
        private TextBox txtDescription = null!;
        private CheckBox chkActive = null!;

        private TextBox txtNewPlace = null!;
        private Button btnAddPlace = null!;
        private ListBox lstPlaces = null!;
        private Button btnMoveUp = null!;
        private Button btnMoveDown = null!;
        private Button btnRemovePlace = null!;
        private Button btnClearPlaces = null!;
        private Label lblPreview = null!;

        private Button btnSave = null!;
        private Button btnCancel = null!;

        public ClientRouteEditDialog(string clientCompanyName, DeliveryRoute? routeToEdit = null)
        {
            this.clientCompanyName = string.IsNullOrWhiteSpace(clientCompanyName) ? "Default Client" : clientCompanyName.Trim();
            this.isEditMode = routeToEdit != null && routeToEdit.Id > 0;
            this.CurrentRoute = routeToEdit ?? new DeliveryRoute
            {
                ClientCompanyName = this.clientCompanyName,
                RouteCode = MJdatabase.GenerateNextRouteCode(),
                IsActive = true
            };

            InitializeCustomComponent();
            PopulateData();
            UIThemeHelper.ApplyTheme(this);
        }

        private void InitializeCustomComponent()
        {
            this.Text = isEditMode ? "Edit Client Delivery Route" : "Add Client Delivery Route";
            this.Size = new Size(560, 680);
            this.MinimumSize = new Size(520, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Pal.C(248, 250, 252);
            this.Font = new Font("Segoe UI", 9F);

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Pal.C(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };

            Label lblTitle = new Label
            {
                Text = isEditMode ? "✏️ Edit Delivery Route" : "🛣️ Add New Delivery Route",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Pal.White,
                Dock = DockStyle.Top,
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Configuring route for client: {clientCompanyName}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Pal.C(148, 163, 184),
                Dock = DockStyle.Bottom,
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 14),
                AutoScroll = true
            };

            TableLayoutPanel tblFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 5,
                Margin = new Padding(0)
            };
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tblFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));

            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tblFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Panel pnlClientWrap = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 8, 8) };
            Label lblClient = new Label { Text = "Client Company:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtClient = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), ReadOnly = true, BackColor = Pal.C(241, 245, 249) };
            pnlClientWrap.Controls.Add(txtClient);
            pnlClientWrap.Controls.Add(lblClient);

            Panel pnlCodeWrap = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(8, 0, 0, 8) };
            Label lblCode = new Label { Text = "Route Code (e.g. R1):", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtRouteCode = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 20 };
            pnlCodeWrap.Controls.Add(txtRouteCode);
            pnlCodeWrap.Controls.Add(lblCode);

            tblFields.Controls.Add(pnlClientWrap, 0, 0);
            tblFields.Controls.Add(pnlCodeWrap, 1, 0);

            Panel pnlNameWrap = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
            Label lblName = new Label { Text = "Route Name (e.g. EODLAB (Ilocos Route)):", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtRouteName = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 150 };
            pnlNameWrap.Controls.Add(txtRouteName);
            pnlNameWrap.Controls.Add(lblName);
            tblFields.Controls.Add(pnlNameWrap, 0, 1);
            tblFields.SetColumnSpan(pnlNameWrap, 2);

            Panel pnlDescWrap = new Panel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
            Label lblDesc = new Label { Text = "Description / Region / Area Notes:", Dock = DockStyle.Top, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5F) };
            txtDescription = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F), MaxLength = 250 };
            pnlDescWrap.Controls.Add(txtDescription);
            pnlDescWrap.Controls.Add(lblDesc);
            tblFields.Controls.Add(pnlDescWrap, 0, 2);
            tblFields.SetColumnSpan(pnlDescWrap, 2);

            chkActive = new CheckBox
            {
                Text = "Route is active and available for loading and dispatch schedules",
                Font = new Font("Segoe UI Semibold", 8.5F),
                Dock = DockStyle.Top,
                Checked = true,
                Margin = new Padding(0, 0, 0, 12),
                AutoSize = true
            };
            tblFields.Controls.Add(chkActive, 0, 3);
            tblFields.SetColumnSpan(chkActive, 2);

            GroupBox grpPlaces = new GroupBox
            {
                Text = "Connected Places / Delivery Stops Sequence",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Pal.C(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 270,
                Padding = new Padding(12, 10, 12, 10),
                BackColor = Pal.White
            };

            Panel pnlAddPlace = new Panel { Dock = DockStyle.Top, Height = 34, Margin = new Padding(0, 0, 0, 6) };
            txtNewPlace = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F), PlaceholderText = "Enter store code, depot name, or branch (e.g. 203077 - RR-Laoag)" };
            txtNewPlace.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddPlace(); } };

            btnAddPlace = new Button
            {
                Text = "+ Add Stop",
                Dock = DockStyle.Right,
                Width = 95,
                Margin = new Padding(6, 0, 0, 0),
                BackColor = Pal.C(37, 99, 235),
                ForeColor = Pal.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddPlace.FlatAppearance.BorderSize = 0;
            btnAddPlace.Click += (s, e) => AddPlace();

            pnlAddPlace.Controls.Add(txtNewPlace);
            pnlAddPlace.Controls.Add(btnAddPlace);

            Panel pnlListWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
            lstPlaces = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ItemHeight = 22,
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel pnlListBtns = new Panel { Dock = DockStyle.Right, Width = 95, Padding = new Padding(6, 0, 0, 0) };
            btnMoveUp = new Button { Text = "▲ Move Up", Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 0, 0, 4), FlatStyle = FlatStyle.Flat, BackColor = Pal.C(241, 245, 249), Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand };
            btnMoveUp.FlatAppearance.BorderSize = 1;
            btnMoveUp.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnMoveUp.Click += (s, e) => MovePlace(-1);

            btnMoveDown = new Button { Text = "▼ Move Down", Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 4, 0, 4), FlatStyle = FlatStyle.Flat, BackColor = Pal.C(241, 245, 249), Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand };
            btnMoveDown.FlatAppearance.BorderSize = 1;
            btnMoveDown.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnMoveDown.Click += (s, e) => MovePlace(1);

            btnRemovePlace = new Button { Text = "✖ Remove", Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 4, 0, 4), FlatStyle = FlatStyle.Flat, BackColor = Pal.C(254, 242, 242), ForeColor = Pal.C(220, 38, 38), Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand };
            btnRemovePlace.FlatAppearance.BorderSize = 1;
            btnRemovePlace.FlatAppearance.BorderColor = Pal.C(254, 202, 202);
            btnRemovePlace.Click += (s, e) => RemoveSelectedPlace();

            btnClearPlaces = new Button { Text = "🗑️ Clear", Dock = DockStyle.Top, Height = 28, Margin = new Padding(0, 4, 0, 0), FlatStyle = FlatStyle.Flat, BackColor = Pal.C(241, 245, 249), Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand };
            btnClearPlaces.FlatAppearance.BorderSize = 1;
            btnClearPlaces.FlatAppearance.BorderColor = Pal.C(203, 213, 225);
            btnClearPlaces.Click += (s, e) => { lstPlaces.Items.Clear(); UpdatePreview(); };

            pnlListBtns.Controls.Add(btnClearPlaces);
            pnlListBtns.Controls.Add(btnRemovePlace);
            pnlListBtns.Controls.Add(btnMoveDown);
            pnlListBtns.Controls.Add(btnMoveUp);

            pnlListWrap.Controls.Add(lstPlaces);
            pnlListWrap.Controls.Add(pnlListBtns);

            lblPreview = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Pal.C(100, 116, 139),
                Text = "Sequence Preview: None configured.",
                AutoEllipsis = true
            };

            grpPlaces.Controls.Add(pnlListWrap);
            grpPlaces.Controls.Add(lblPreview);
            grpPlaces.Controls.Add(pnlAddPlace);

            pnlBody.Controls.Add(grpPlaces);
            pnlBody.Controls.Add(tblFields);

            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Pal.White,
                Padding = new Padding(20, 10, 20, 10)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                BackColor = Pal.C(241, 245, 249),
                ForeColor = Pal.C(71, 85, 105),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnSave = new Button
            {
                Text = isEditMode ? "💾 Save Changes" : "💾 Save Route",
                Dock = DockStyle.Right,
                Width = 130,
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Pal.C(22, 163, 74),
                ForeColor = Pal.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 10 });
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void PopulateData()
        {
            txtClient.Text = clientCompanyName;
            txtRouteCode.Text = CurrentRoute.RouteCode;
            txtRouteName.Text = CurrentRoute.RouteName;
            txtDescription.Text = CurrentRoute.Description;
            chkActive.Checked = CurrentRoute.IsActive;

            lstPlaces.Items.Clear();
            if (!string.IsNullOrWhiteSpace(CurrentRoute.ConnectedPlaces))
            {
                var parts = CurrentRoute.ConnectedPlaces.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string clean = p.Trim();
                    if (!string.IsNullOrEmpty(clean))
                    {
                        lstPlaces.Items.Add(clean);
                    }
                }
            }
            UpdatePreview();
        }

        private void AddPlace()
        {
            string place = txtNewPlace.Text.Trim();
            if (string.IsNullOrEmpty(place)) return;
            lstPlaces.Items.Add(place);
            txtNewPlace.Clear();
            txtNewPlace.Focus();
            UpdatePreview();
        }

        private void RemoveSelectedPlace()
        {
            int idx = lstPlaces.SelectedIndex;
            if (idx >= 0)
            {
                lstPlaces.Items.RemoveAt(idx);
                if (idx < lstPlaces.Items.Count)
                {
                    lstPlaces.SelectedIndex = idx;
                }
                else if (lstPlaces.Items.Count > 0)
                {
                    lstPlaces.SelectedIndex = lstPlaces.Items.Count - 1;
                }
                UpdatePreview();
            }
        }

        private void MovePlace(int offset)
        {
            int idx = lstPlaces.SelectedIndex;
            if (idx < 0) return;
            int newIdx = idx + offset;
            if (newIdx < 0 || newIdx >= lstPlaces.Items.Count) return;

            object item = lstPlaces.Items[idx];
            lstPlaces.Items.RemoveAt(idx);
            lstPlaces.Items.Insert(newIdx, item);
            lstPlaces.SelectedIndex = newIdx;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (lstPlaces.Items.Count == 0)
            {
                lblPreview.Text = "Sequence Preview: None configured.";
                return;
            }
            var list = new List<string>();
            foreach (var item in lstPlaces.Items)
            {
                list.Add(item.ToString() ?? "");
            }
            lblPreview.Text = "Sequence: " + string.Join(" ➔ ", list);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string code = txtRouteCode.Text.Trim();
            string name = txtRouteName.Text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Please enter a route code (e.g. R1, R2).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRouteCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a route name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRouteName.Focus();
                return;
            }

            if (lstPlaces.Items.Count == 0)
            {
                MessageBox.Show("Please add at least one delivery stop / connected place in the sequence.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNewPlace.Focus();
                return;
            }

            var placesList = new List<string>();
            foreach (var item in lstPlaces.Items)
            {
                placesList.Add(item.ToString() ?? "");
            }

            CurrentRoute.ClientCompanyName = clientCompanyName;
            CurrentRoute.RouteCode = code;
            CurrentRoute.RouteName = name;
            CurrentRoute.Description = txtDescription.Text.Trim();
            CurrentRoute.ConnectedPlaces = string.Join("; ", placesList);
            CurrentRoute.IsActive = chkActive.Checked;

            bool success = MJdatabase.SaveDeliveryRoute(CurrentRoute);
            if (!success)
            {
                MessageBox.Show("Failed to save route. Please check if the route code is unique.", "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
