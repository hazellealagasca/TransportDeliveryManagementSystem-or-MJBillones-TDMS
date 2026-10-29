using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class DataExportHelper
    {
        public static void AttachExportContextMenu(DataGridView grid, string defaultFileName, string? sheetTitle = null)
        {
            ContextMenuStrip menu = grid.ContextMenuStrip ?? new ContextMenuStrip();

            if (menu.Items.Count > 0)
            {
                menu.Items.Add(new ToolStripSeparator());
            }

            ToolStripMenuItem itemCsv = new ToolStripMenuItem("📊 Export Table to CSV...", null, (s, e) =>
            {
                ExportToCsv(grid, defaultFileName, sheetTitle);
            });

            ToolStripMenuItem itemExcel = new ToolStripMenuItem("📑 Export Table to Excel (.xml)...", null, (s, e) =>
            {
                ExportToExcelXml(grid, defaultFileName, sheetTitle);
            });

            menu.Items.Add(itemCsv);
            menu.Items.Add(itemExcel);
            grid.ContextMenuStrip = menu;
        }

        public static bool ExportToCsv(DataGridView grid, string defaultFileName, string? sheetTitle = null)
        {
            if (grid.Rows.Count == 0)
            {
                MessageBox.Show("There is no tabular data available to export.", "Export Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string cleanBaseName = Path.GetFileNameWithoutExtension(defaultFileName);
            string initialName = $"{cleanBaseName}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            using (SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Export Data to CSV (Comma-Separated Values)",
                Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = initialName
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return false;

                try
                {
                    StringBuilder sb = new StringBuilder();

                    bool firstCol = true;
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        var col = grid.Columns[i];
                        if (!col.Visible) continue;
                        if (IsInternalColumn(col)) continue;

                        if (!firstCol) sb.Append(",");
                        firstCol = false;

                        string header = col.HeaderText ?? col.Name;
                        sb.Append(EscapeCsv(header.Replace("_", " ")));
                    }
                    sb.AppendLine();

                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.IsNewRow) continue;
                        firstCol = true;

                        for (int i = 0; i < grid.Columns.Count; i++)
                        {
                            var col = grid.Columns[i];
                            if (!col.Visible) continue;
                            if (IsInternalColumn(col)) continue;

                            if (!firstCol) sb.Append(",");
                            firstCol = false;

                            object? val = row.Cells[i].FormattedValue ?? row.Cells[i].Value;
                            sb.Append(EscapeCsv(val?.ToString() ?? ""));
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));

                    OfferToOpenFile(sfd.FileName, "CSV");
                    return true;
                }
                catch (Exception ex)
                { ErrLog.Error("DataExportHelper", ex); 
                    MessageBox.Show($"Failed to export data to CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        public static bool ExportToExcelXml(DataGridView grid, string defaultFileName, string? sheetTitle = null)
        {
            if (grid.Rows.Count == 0)
            {
                MessageBox.Show("There is no tabular data available to export.", "Export Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string cleanBaseName = Path.GetFileNameWithoutExtension(defaultFileName);
            string initialName = $"{cleanBaseName}_{DateTime.Now:yyyyMMdd_HHmm}.xml";

            using (SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Export Data to Excel Spreadsheet (XML format)",
                Filter = "Excel XML Spreadsheet (*.xml)|*.xml|Excel File (*.xls)|*.xls",
                FileName = initialName
            })
            {
                if (sfd.ShowDialog() != DialogResult.OK) return false;

                try
                {
                    string title = string.IsNullOrWhiteSpace(sheetTitle) ? cleanBaseName.Replace("_", " ") : sheetTitle;
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
                    sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                    sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                    sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    sb.AppendLine(" xmlns:html=\"http:" + "/" + "/www.w3.org/TR/REC-html40\">");

                    sb.AppendLine(" <Styles>");
                    sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\">");
                    sb.AppendLine("   <Alignment ss:Vertical=\"Bottom\"/>");
                    sb.AppendLine("   <Font ss:FontName=\"Calibri\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Color=\"#000000\"/>");
                    sb.AppendLine("  </Style>");
                    sb.AppendLine("  <Style ss:ID=\"HeaderStyle\">");
                    sb.AppendLine("   <Font ss:FontName=\"Calibri\" x:Family=\"Swiss\" ss:Size=\"11\" ss:Color=\"#FFFFFF\" ss:Bold=\"1\"/>");
                    sb.AppendLine("   <Interior ss:Color=\"#0F172A\" ss:Pattern=\"Solid\"/>");
                    sb.AppendLine("   <Alignment ss:Vertical=\"Center\"/>");
                    sb.AppendLine("  </Style>");
                    sb.AppendLine("  <Style ss:ID=\"TitleStyle\">");
                    sb.AppendLine("   <Font ss:FontName=\"Calibri\" x:Family=\"Swiss\" ss:Size=\"14\" ss:Color=\"#0F172A\" ss:Bold=\"1\"/>");
                    sb.AppendLine("  </Style>");
                    sb.AppendLine(" </Styles>");

                    sb.AppendLine($" <Worksheet ss:Name=\"DataExport\">");
                    sb.AppendLine("  <Table>");

                    sb.AppendLine("   <Row ss:Height=\"24\">");
                    sb.AppendLine($"    <Cell ss:StyleID=\"TitleStyle\"><Data ss:Type=\"String\">{EscapeXml(title)}</Data></Cell>");
                    sb.AppendLine("   </Row>");
                    sb.AppendLine("   <Row ss:Height=\"18\">");
                    sb.AppendLine($"    <Cell><Data ss:Type=\"String\">Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | M.J. Billones Transport Service</Data></Cell>");
                    sb.AppendLine("   </Row>");
                    sb.AppendLine("   <Row ss:Height=\"10\"/>");

                    sb.AppendLine("   <Row ss:Height=\"22\">");
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        var col = grid.Columns[i];
                        if (!col.Visible) continue;
                        if (IsInternalColumn(col)) continue;

                        string header = col.HeaderText ?? col.Name;
                        sb.AppendLine($"    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">{EscapeXml(header.Replace("_", " "))}</Data></Cell>");
                    }
                    sb.AppendLine("   </Row>");

                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.IsNewRow) continue;
                        sb.AppendLine("   <Row ss:Height=\"18\">");

                        for (int i = 0; i < grid.Columns.Count; i++)
                        {
                            var col = grid.Columns[i];
                            if (!col.Visible) continue;
                            if (IsInternalColumn(col)) continue;

                            object? val = row.Cells[i].FormattedValue ?? row.Cells[i].Value;
                            string strVal = val?.ToString() ?? "";

                            if (decimal.TryParse(strVal.Replace("₱", "").Replace("PHP", "").Replace("$", "").Replace(",", "").Trim(), out decimal numVal) && !strVal.Contains(":") && !strVal.Contains("-"))
                            {
                                sb.AppendLine($"    <Cell><Data ss:Type=\"Number\">{numVal}</Data></Cell>");
                            }
                            else
                            {
                                sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(strVal)}</Data></Cell>");
                            }
                        }
                        sb.AppendLine("   </Row>");
                    }

                    sb.AppendLine("  </Table>");
                    sb.AppendLine(" </Worksheet>");
                    sb.AppendLine("</Workbook>");

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);

                    OfferToOpenFile(sfd.FileName, "Excel XML");
                    return true;
                }
                catch (Exception ex)
                { ErrLog.Error("DataExportHelper", ex); 
                    MessageBox.Show($"Failed to export data to Excel XML: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }

        private static bool IsInternalColumn(DataGridViewColumn col)
        {
            string name = (col.DataPropertyName ?? col.Name ?? "").ToLowerInvariant();
            if (name == "id" || name.EndsWith("ids") || name.EndsWith("hash") || name.EndsWith("password") || name.EndsWith("salt"))
            {
                return true;
            }
            return false;
        }

        private static string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "\"\"";
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n") || text.Contains("\r"))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        private static void OfferToOpenFile(string filePath, string fileType)
        {
            var res = MessageBox.Show($"File successfully exported to {fileType}:\n\n{filePath}\n\nWould you like to open this file now?", "Export Successful", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    try
                    {
                        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                    catch (System.Exception swx1) { ErrLog.Swallowed(swx1); }
                }
            }
        }
    }
}
