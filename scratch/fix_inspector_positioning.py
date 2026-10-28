with open(r'Functions\RecordDetailsDialog.cs', 'r', encoding='utf-8') as f:
    c = f.read()

# 1. pnlHeader
target1 = """            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,"""
repl1 = """            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Width = dlg.ClientSize.Width,
                Height = 80,"""
if target1 not in c:
    target1 = target1.replace('\n', '\r\n')
    repl1 = repl1.replace('\n', '\r\n')
assert target1 in c, 'target1 not found'
c = c.replace(target1, repl1, 1)

# 2. lblBadge
target2 = """            Label lblBadge = new Label
            {
                Text = statusBadge.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = badgeBg,
                ForeColor = badgeFg,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(dlg.ClientSize.Width - 140, 24)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblBadge);"""
repl2 = """            Label lblBadge = new Label
            {
                Text = statusBadge.ToUpperInvariant(),
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                BackColor = badgeBg,
                ForeColor = badgeFg,
                Padding = new Padding(10, 4, 10, 4),
                AutoSize = true,
                Location = new Point(dlg.ClientSize.Width - 140, 24)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblBadge);
            pnlHeader.Resize += (s, e) =>
            {
                lblBadge.Location = new Point(pnlHeader.ClientSize.Width - lblBadge.Width - 20, 24);
            };"""
if target2 not in c:
    target2 = target2.replace('\n', '\r\n')
    repl2 = repl2.replace('\n', '\r\n')
assert target2 in c, 'target2 not found'
c = c.replace(target2, repl2, 1)

# 3. pnlFooter
target3 = """            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,"""
repl3 = """            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Width = dlg.ClientSize.Width,
                Height = 56,"""
if target3 not in c:
    target3 = target3.replace('\n', '\r\n')
    repl3 = repl3.replace('\n', '\r\n')
assert target3 in c, 'target3 not found'
c = c.replace(target3, repl3, 1)

# 4. btnClose
target4 = """            Button btnClose = new Button
            {
                Text = "✕ Close",
                Height = 36,
                Width = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(dlg.ClientSize.Width - 116, 10),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);"""
repl4 = """            Button btnClose = new Button
            {
                Text = "✕ Close",
                Height = 36,
                Width = 100,
                Location = new Point(dlg.ClientSize.Width - 116, 10),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dlg.Close();

            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Resize += (s, e) =>
            {
                btnClose.Location = new Point(pnlFooter.ClientSize.Width - btnClose.Width - 16, 10);
            };"""
if target4 not in c:
    target4 = target4.replace('\n', '\r\n')
    repl4 = repl4.replace('\n', '\r\n')
assert target4 in c, 'target4 not found'
c = c.replace(target4, repl4, 1)

with open(r'Functions\RecordDetailsDialog.cs', 'w', encoding='utf-8') as f:
    f.write(c)
print('Successfully updated RecordDetailsDialog positioning')
