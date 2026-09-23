using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Database;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class DraftAutoSaveService
    {
        public static string SerializeDraft(Dictionary<string, string> values)
        {
            try
            {
                return JsonSerializer.Serialize(values);
            }
            catch
            {
                return "{}";
            }
        }

        public static Dictionary<string, string> DeserializeDraft(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static Panel CreateDraftBanner(string moduleName, DateTime draftTime, Action onRestore, Action onDiscard)
        {
            Panel banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(14, 6, 14, 6),
                Margin = new Padding(0, 0, 0, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lbl = new Label
            {
                Text = $"📝 Unsaved draft found for {moduleName} from {draftTime:MMM dd, hh:mm tt}. Would you like to restore your work?",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            FlowLayoutPanel flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            Button btnRestore = new Button
            {
                Text = "Restore Draft",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(217, 119, 6),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 30),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += (s, e) =>
            {
                banner.Visible = false;
                onRestore?.Invoke();
            };

            Button btnDiscard = new Button
            {
                Text = "Discard",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 30),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnDiscard.FlatAppearance.BorderSize = 0;
            btnDiscard.Click += (s, e) =>
            {
                banner.Visible = false;
                onDiscard?.Invoke();
            };

            flowBtns.Controls.Add(btnRestore);
            flowBtns.Controls.Add(btnDiscard);

            banner.Controls.Add(flowBtns);
            banner.Controls.Add(lbl);

            return banner;
        }
    }
}
