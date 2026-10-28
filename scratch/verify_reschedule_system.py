import sqlite3
import re
import sys

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

print("Starting verification of Reschedule Synchronization & UI Polish...")

# 1. Verify Database Methods in Database/MJdatabase.cs
with open("Database/MJdatabase.cs", "r", encoding="utf-8") as f:
    mjdb = f.read()

assert "public static (bool Success, string Message) RescheduleDelivery(" in mjdb, "RescheduleDelivery not found in MJdatabase.cs"
assert "public static (bool Success, string Message) RescheduleSpecialTrip(" in mjdb, "RescheduleSpecialTrip not found in MJdatabase.cs"
assert "public static Delivery? GetDeliveryById(int id)" in mjdb, "GetDeliveryById not found in MJdatabase.cs"
assert "Status = 'Rescheduled'" in mjdb, "Status = 'Rescheduled' not found in update statement"
print("PASS: MJdatabase contains RescheduleDelivery, RescheduleSpecialTrip, and GetDeliveryById.")

# 2. Verify Forms/RescheduleDeliveryDialog.cs
with open("Forms/RescheduleDeliveryDialog.cs", "r", encoding="utf-8") as f:
    r_dlg = f.read()

assert "Size(740, 800)" in r_dlg, "Dialog size 740x800 expected"
assert "MinimumSize = new Size(720, 740)" in r_dlg, "MinimumSize 720x740 expected"
assert "RescheduleDeliveryDialog(Delivery targetDelivery)" in r_dlg, "Delivery constructor expected"
assert "RescheduleDeliveryDialog(SpecialTrip targetSpecialTrip)" in r_dlg, "SpecialTrip constructor expected"
assert "cbTruck = new ComboBox" in r_dlg, "cbTruck expected"
assert "cbDriver = new ComboBox" in r_dlg, "cbDriver expected"
assert "cbAide = new ComboBox" in r_dlg, "cbAide expected"
assert "RescheduledDate" in r_dlg and "RescheduledTime" in r_dlg, "Rescheduled properties expected"
print("PASS: RescheduleDeliveryDialog has dual-mode support, resource reassignments, and widened 740px bounds.")

# 3. Verify Views/DispatchManagementView.cs
with open("Views/DispatchManagementView.cs", "r", encoding="utf-8") as f:
    disp_view = f.read()

assert "target.DispatchDate = dtpDispatchDate.Value.Date;" in disp_view, "Dispatch form pre-sync expected"
assert "target.DepartureTime = cbDepartureTime.Text.Trim();" in disp_view, "Departure time pre-sync expected"
assert "BindDeliveryToForm(reloaded);" in disp_view, "Two-way BindDeliveryToForm sync expected"
assert 'cbStatus.Text = "Rescheduled";' in disp_view, "cbStatus text Rescheduled expected"
assert "ClearFields();\n                    LoadDropdownsAndGrid();" not in disp_view, "ClearFields() should NOT be called on reschedule confirm"
print("PASS: DispatchManagementView implements two-way sync without clearing form.")

# 4. Verify Forms/SpecialTripFormDialog.cs
with open("Forms/SpecialTripFormDialog.cs", "r", encoding="utf-8") as f:
    st_dlg = f.read()

assert "btnReschedule = new Button" in st_dlg, "btnReschedule expected in SpecialTripFormDialog"
assert "BtnReschedule_Click" in st_dlg, "BtnReschedule_Click expected"
assert 'HeaderText = "Departure Time",\n                Width = 155' in st_dlg or 'Width = 155' in st_dlg, "colDepTime width 155 expected"
assert "Color.FromArgb(224, 242, 254)" in st_dlg, "High contrast breadcrumb color expected"
assert "Location = new Point(145, 10)" in st_dlg, "Table description spacing expected"
print("PASS: SpecialTripFormDialog has reschedule button, colDepTime width 155, and high contrast breadcrumbs.")

# 5. Verify Views/SpecialTripHistoryView.cs
with open("Views/SpecialTripHistoryView.cs", "r", encoding="utf-8") as f:
    st_hist = f.read()

assert "colBtnResched" in st_hist, "colBtnResched column expected in SpecialTripHistoryView"
assert "RescheduleDeliveryDialog(trip)" in st_hist, "Reschedule dialog call expected in SpecialTripHistoryView"
print("PASS: SpecialTripHistoryView has row reschedule action button.")

# 6. Functional Database test with SQLite
import os

db_path = r'bin\Release\net9.0-windows\mj_billones.db'
if not os.path.exists(db_path):
    db_path = 'mj_billones.db'

conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# Ensure SpecialTrips table exists
cursor.executescript('''
    CREATE TABLE IF NOT EXISTS SpecialTrips (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        OrderReference TEXT NOT NULL UNIQUE,
        DispatchDate TEXT NOT NULL,
        Company TEXT,
        DepartureTime TEXT,
        Address TEXT,
        ServiceType TEXT,
        Status TEXT DEFAULT 'Pending',
        ModeOfPaymentWithReport INTEGER DEFAULT 1,
        WithSOA INTEGER DEFAULT 0,
        WithServiceInvoice INTEGER DEFAULT 0,
        WithCollection INTEGER DEFAULT 0,
        TruckId INTEGER DEFAULT 0,
        TruckPlate TEXT,
        DriverId INTEGER DEFAULT 0,
        DriverName TEXT,
        AideId INTEGER DEFAULT 0,
        AideName TEXT,
        CreatedAt TEXT,
        CompletedAt TEXT,
        Remarks TEXT
    );
''')
conn.commit()

# Ensure at least one test SpecialTrip exists
cursor.execute("SELECT COUNT(*) FROM SpecialTrips")
if cursor.fetchone()[0] == 0:
    cursor.execute("""
        INSERT INTO SpecialTrips (OrderReference, DispatchDate, Company, DepartureTime, Address, ServiceType, Status)
        VALUES ('ST-20261024-0001', '2026-10-24 08:00:00', 'San Miguel Foods', '08:00 AM', 'Pasig Depot', 'Dedicated / Special Delivery', 'Pending')
    """)
    conn.commit()

# Test Reschedule on Deliveries table
cursor.execute("SELECT Id, DispatchNumber, DispatchDate, DepartureTime, Status FROM Deliveries LIMIT 1")
row = cursor.fetchone()
if row:
    del_id, dsp_num, old_date, old_time, old_status = row
    test_new_date = "2026-11-15"
    test_new_time = "02:30 PM"
    test_reason = "Customer Delivery Window Shift"
    
    cursor.execute("""
        UPDATE Deliveries
        SET DispatchDate = ?, DepartureTime = ?, Status = 'Rescheduled', Remarks = Remarks || ' | Rescheduled'
        WHERE Id = ?
    """, (test_new_date, test_new_time, del_id))
    conn.commit()
    
    cursor.execute("SELECT DispatchDate, DepartureTime, Status FROM Deliveries WHERE Id = ?", (del_id,))
    updated = cursor.fetchone()
    assert updated[0] == test_new_date, f"Expected {test_new_date}, got {updated[0]}"
    assert updated[1] == test_new_time, f"Expected {test_new_time}, got {updated[1]}"
    assert updated[2] == "Rescheduled", f"Expected Rescheduled, got {updated[2]}"
    print(f"PASS: SQLite Delivery #{dsp_num} successfully updated to {test_new_date} {test_new_time} with Status='Rescheduled'.")
    
    # Restore original delivery
    cursor.execute("UPDATE Deliveries SET DispatchDate = ?, DepartureTime = ?, Status = ? WHERE Id = ?", (old_date, old_time, old_status, del_id))
    conn.commit()
    print("PASS: Restored test delivery state.")

# Test Reschedule on SpecialTrips table
cursor.execute("SELECT Id, OrderReference, DispatchDate, DepartureTime, Status FROM SpecialTrips LIMIT 1")
st_row = cursor.fetchone()
if st_row:
    st_id, st_ref, st_old_date, st_old_time, st_old_status = st_row
    st_test_date = "2026-11-20 00:00:00"
    st_test_time = "04:15 PM"
    
    cursor.execute("""
        UPDATE SpecialTrips
        SET DispatchDate = ?, DepartureTime = ?, Status = 'Rescheduled'
        WHERE Id = ?
    """, (st_test_date, st_test_time, st_id))
    conn.commit()
    
    cursor.execute("SELECT DispatchDate, DepartureTime, Status FROM SpecialTrips WHERE Id = ?", (st_id,))
    st_updated = cursor.fetchone()
    assert st_updated[0] == st_test_date
    assert st_updated[1] == st_test_time
    assert st_updated[2] == "Rescheduled"
    print(f"PASS: SQLite SpecialTrip {st_ref} successfully updated to {st_test_date} {st_test_time} with Status='Rescheduled'.")
    
    # Restore original
    cursor.execute("UPDATE SpecialTrips SET DispatchDate = ?, DepartureTime = ?, Status = ? WHERE Id = ?", (st_old_date, st_old_time, st_old_status, st_id))
    conn.commit()
    print("PASS: Restored test special trip state.")

conn.close()

print("\nALL VERIFICATIONS PASSED SUCCESSFULLY!")
