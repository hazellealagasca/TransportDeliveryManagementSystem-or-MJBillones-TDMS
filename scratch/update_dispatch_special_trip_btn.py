with open('Views/DispatchManagementView.cs', 'r', encoding='utf-8') as f:
    text = f.read()

target = '            pnlGridActionsFlow.Controls.Add(btnToggleForm);'
replacement = '''            pnlGridActionsFlow.Controls.Add(btnToggleForm);

            Button btnSpecialTrip = new Button
            {
                Text = "✨ + Special Trip",
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(14, 116, 144),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 8, 4)
            };
            btnSpecialTrip.FlatAppearance.BorderSize = 0;
            btnSpecialTrip.Click += (s, e) =>
            {
                using (var dlg = new Forms.SpecialTripFormDialog())
                {
                    dlg.ShowDialog(this.FindForm());
                }
            };
            pnlGridActionsFlow.Controls.Add(btnSpecialTrip);'''

if target in text:
    text = text.replace(target, replacement, 1)
    print("Replaced with LF")
elif target.replace('\n', '\r\n') in text:
    text = text.replace(target.replace('\n', '\r\n'), replacement.replace('\n', '\r\n'), 1)
    print("Replaced with CRLF")
else:
    raise Exception("target not found")

with open('Views/DispatchManagementView.cs', 'w', encoding='utf-8') as f:
    f.write(text)

print("SUCCESS: DispatchManagementView.cs updated with Special Trip button!")
