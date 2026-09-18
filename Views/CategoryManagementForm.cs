using System;
using System.Drawing;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Models;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Views
{
    public partial class CategoryManagementForm : Form
    {
        private string selectedCategory = "Client Company";

        public CategoryManagementForm()
        {
            InitializeComponent();
            cbCategoryType.SelectedIndex = 0;
            LoadCategoryData();
            UIThemeHelper.ApplyTheme(this);
        }

        private void CbCategoryType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            selectedCategory = cbCategoryType.SelectedItem?.ToString() ?? "Client Company";
            bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
            bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

            lblContactPerson.Visible = isClient;
            txtContactPerson.Visible = isClient;
            lblContactNumber.Visible = isClient;
            txtContactNumber.Visible = isClient;
            lblAddress.Visible = isClient;
            txtAddress.Visible = isClient;

            lblCapacity.Visible = isTruckType;
            numCapacity.Visible = isTruckType;

            if (isClient)
            {
                lblVal.Text = "Client Company Name:";
                btnUpdate.Visible = true;
            }
            else if (isTruckType)
            {
                lblVal.Text = "Truck Category Name:";
                btnUpdate.Visible = false;
            }
            else
            {
                lblVal.Text = "Category / Item Name:";
                btnUpdate.Visible = false;
            }

            LoadCategoryData();
        }

        private void LoadCategoryData()
        {
            try
            {
                ClearInputFields();
                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

                gridItems.DataSource = null;
                gridItems.Columns.Clear();
                gridItems.AutoGenerateColumns = false;
                gridItems.Rows.Clear();

                if (isClient)
                {
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Company Name", FillWeight = 28, MinimumWidth = 140 });
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact Person", FillWeight = 22, MinimumWidth = 120 });
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contact #", FillWeight = 20, MinimumWidth = 100 });
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Address / Notes", FillWeight = 30, MinimumWidth = 140 });

                    var clients = MJdatabase.GetClients();
                    foreach (var c in clients)
                    {
                        gridItems.Rows.Add(c.CompanyName, c.ContactPerson, c.ContactNumber, c.Address);
                    }
                }
                else if (isTruckType)
                {
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Truck Category Name", FillWeight = 65, MinimumWidth = 180 });
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Capacity (Crates)", FillWeight = 35, MinimumWidth = 120 });

                    var itemsWithExtra = MJdatabase.GetLookupItemsWithExtra(selectedCategory);
                    foreach (var item in itemsWithExtra)
                    {
                        string capStr = string.IsNullOrEmpty(item.ExtraValue) ? "300" : item.ExtraValue;
                        gridItems.Rows.Add(item.ItemValue, $"{capStr} crates");
                    }
                }
                else
                {
                    gridItems.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = $"{selectedCategory} Name / Entry", FillWeight = 100, MinimumWidth = 180 });

                    var items = MJdatabase.GetLookupItems(selectedCategory);
                    foreach (var item in items)
                    {
                        gridItems.Rows.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load categories: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridItems_SelectionChanged(object? sender, EventArgs e)
        {
            if (gridItems.SelectedRows.Count > 0 && gridItems.SelectedRows[0].Cells[0].Value != null)
            {
                var row = gridItems.SelectedRows[0];
                txtItemValue.Text = row.Cells[0].Value?.ToString() ?? "";

                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

                if (isClient)
                {
                    txtContactPerson.Text = row.Cells.Count > 1 ? row.Cells[1].Value?.ToString() ?? "" : "";
                    txtContactNumber.Text = row.Cells.Count > 2 ? row.Cells[2].Value?.ToString() ?? "" : "";
                    txtAddress.Text = row.Cells.Count > 3 ? row.Cells[3].Value?.ToString() ?? "" : "";
                }
                else if (isTruckType && row.Cells.Count > 1 && row.Cells[1].Value != null)
                {
                    string capVal = row.Cells[1].Value?.ToString() ?? "";
                    string cleanDigits = System.Text.RegularExpressions.Regex.Replace(capVal, @"[^\d]", "");
                    if (int.TryParse(cleanDigits, out int parsedCap) && parsedCap >= numCapacity.Minimum && parsedCap <= numCapacity.Maximum)
                    {
                        numCapacity.Value = parsedCap;
                    }
                }
            }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please enter a category or company name.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtItemValue.Focus();
                    return;
                }

                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                bool isTruckType = selectedCategory.Contains("Truck Type", StringComparison.OrdinalIgnoreCase);

                if (isClient)
                {
                    ClientCompany client = new ClientCompany
                    {
                        CompanyName = val,
                        ContactPerson = txtContactPerson.Text.Trim(),
                        ContactNumber = txtContactNumber.Text.Trim(),
                        Address = txtAddress.Text.Trim()
                    };
                    MJdatabase.SaveClient(client);
                    MessageBox.Show($"Client Company '{val}' with contact info saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (isTruckType)
                {
                    string cap = numCapacity.Value.ToString();
                    MJdatabase.SaveLookupItemWithExtra(selectedCategory, val, cap);
                    MessageBox.Show($"Truck Category '{val}' with capacity of {cap} crates saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MJdatabase.SaveLookupItem(selectedCategory, val);
                    MessageBox.Show($"'{val}' saved to {selectedCategory} dropdown database successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                MJdatabase.NotifyCategoriesChanged();
                LoadCategoryData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save category item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please select or enter a client company name to update.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtItemValue.Focus();
                    return;
                }

                bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                if (isClient)
                {
                    ClientCompany client = new ClientCompany
                    {
                        CompanyName = val,
                        ContactPerson = txtContactPerson.Text.Trim(),
                        ContactNumber = txtContactNumber.Text.Trim(),
                        Address = txtAddress.Text.Trim()
                    };
                    MJdatabase.SaveClient(client);
                    MessageBox.Show($"Client Company '{val}' contact details updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MJdatabase.NotifyCategoriesChanged();
                    LoadCategoryData();
                }
                else
                {
                    BtnAdd_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update client company: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                string val = txtItemValue.Text.Trim();
                if (string.IsNullOrEmpty(val))
                {
                    MessageBox.Show("Please select an entry to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Are you sure you want to delete '{val}' from {selectedCategory}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    bool isClient = selectedCategory.Equals("Client Company", StringComparison.OrdinalIgnoreCase);
                    if (isClient)
                    {
                        MJdatabase.DeleteClient(val);
                    }
                    else
                    {
                        MJdatabase.DeleteLookupItem(selectedCategory, val);
                    }
                    MJdatabase.NotifyCategoriesChanged();
                    MessageBox.Show($"'{val}' removed from database.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCategoryData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            ClearInputFields();
        }

        private void ClearInputFields()
        {
            txtItemValue.Clear();
            txtContactPerson.Clear();
            txtContactNumber.Clear();
            txtAddress.Clear();
            if (numCapacity.Maximum < 1000000) numCapacity.Maximum = 1000000;
            if (numCapacity.Minimum < 1) numCapacity.Minimum = 1;
            numCapacity.Value = 300;
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
