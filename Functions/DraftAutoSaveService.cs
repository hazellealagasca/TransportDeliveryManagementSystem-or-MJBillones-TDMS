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
                BackColor = Pal.C(254, 243, 199),
                Padding = new Padding(14, 6, 14, 6),
                Margin = new Padding(0, 0, 0, 8),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lbl = new Label
            {
                Text = $"📝 Unsaved draft ({draftTime:MMM dd, hh:mm tt})",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Pal.C(146, 64, 14),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                AutoEllipsis = true
            };

            FlowLayoutPanel flowBtns = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            Button btnRestore = new Button
            {
                Text = "Restore",
                Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold),
                ForeColor = Pal.White,
                BackColor = Pal.C(217, 119, 6),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(72, 28),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 6, 0)
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
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = Pal.C(180, 83, 9),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(64, 28),
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

            banner.Controls.Add(lbl);
            banner.Controls.Add(flowBtns);
            banner.Controls.SetChildIndex(lbl, 0);
            banner.Controls.SetChildIndex(flowBtns, 1);

            return banner;
        }
    }
}
