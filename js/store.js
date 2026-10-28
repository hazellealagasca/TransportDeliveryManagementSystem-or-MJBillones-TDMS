/**
 * M.J. Billones Transport Service - Reactive Store & Data Layer
 * Handles LocalStorage persistence, auto-migration, seed initialization,
 * and logistics business logic (dispatches, truck matching, trip completion, billing).
 */

class Store {
  constructor() {
    this.STORAGE_KEY = 'mj_billones_tdms_db';
    this.db = null;
    this.listeners = new Set();
    this.init();
  }

  init() {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        this.db = JSON.parse(stored);
      } else {
        this.resetToSeed();
      }
    } catch (e) {
      console.error('Failed to load database from localStorage, resetting to seed:', e);
      this.resetToSeed();
    }
  }

  resetToSeed() {
    const seed = window.INITIAL_SEED_DATA || {};
    this.db = JSON.parse(JSON.stringify(seed));
    this.save();
  }

  save() {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.db));
      this.notify();
    } catch (e) {
      console.error('Failed to save database to localStorage:', e);
    }
  }

  subscribe(listener) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  notify() {
    for (const listener of this.listeners) {
      try {
        listener(this.db);
      } catch (e) {
        console.error(e);
      }
    }
  }

  get(table) {
    return this.db[table] || [];
  }

  getById(table, id) {
    const list = this.get(table);
    return list.find(item => Number(item.Id) === Number(id)) || null;
  }

  insert(table, record, user = 'System') {
    if (!this.db[table]) this.db[table] = [];
    const list = this.db[table];
    const maxId = list.reduce((max, item) => Math.max(max, Number(item.Id) || 0), 0);
    record.Id = maxId + 1;
    list.unshift(record);
    this.logAudit(user, 'INSERT', table, record.Id, JSON.stringify(record));
    this.save();
    return record;
  }

  update(table, id, updates, user = 'System') {
    const list = this.get(table);
    const index = list.findIndex(item => Number(item.Id) === Number(id));
    if (index === -1) return null;
    const oldVal = { ...list[index] };
    list[index] = { ...list[index], ...updates };
    this.logAudit(user, 'UPDATE', table, id, `Changed: ${JSON.stringify(updates)}`);
    this.save();
    return list[index];
  }

  delete(table, id, user = 'System') {
    const list = this.get(table);
    const index = list.findIndex(item => Number(item.Id) === Number(id));
    if (index === -1) return false;
    const deleted = list.splice(index, 1)[0];
    this.logAudit(user, 'DELETE', table, id, `Deleted record: ${JSON.stringify(deleted)}`);
    this.save();
    return true;
  }

  logAudit(user, action, table, recordId, details) {
    if (!this.db.AuditTrail) this.db.AuditTrail = [];
    const now = new Date().toISOString().replace('T', ' ').substring(0, 19);
    this.db.AuditTrail.unshift({
      Id: (this.db.AuditTrail.length + 1),
      Timestamp: now,
      Username: typeof user === 'string' ? user : (user.Username || 'System'),
      UserRole: typeof user === 'object' ? user.Role : 'System',
      ActionType: action,
      TableName: table,
      RecordId: recordId,
      RecordIdentifier: `${table} #${recordId}`,
      FieldName: '',
      OldValue: '',
      NewValue: details,
      ReasonOrNotes: details,
      IpOrMachine: 'Web Browser Client'
    });
  }

  addNotification(title, message, type = 'info', targetModule = '') {
    if (!this.db.SystemNotifications) this.db.SystemNotifications = [];
    const now = new Date().toISOString().replace('T', ' ').substring(0, 19);
    this.db.SystemNotifications.unshift({
      Id: this.db.SystemNotifications.length + 1,
      Title: title,
      Message: message,
      Timestamp: now,
      Type: type,
      Category: 'Operational',
      IsRead: 0,
      TargetModule: targetModule,
      TargetRecordId: 0
    });
    this.save();
  }

  // --- Specialized Domain & Logistics Workflow Methods ---

  recommendTrucks(cargoType, requiredCapacity) {
    const trucks = this.get('Trucks');
    return trucks.map(truck => {
      const isAvailable = truck.Status === 'Available';
      const capFits = Number(truck.CapacityCrates) >= Number(requiredCapacity);
      const isColdCargo = cargoType.toLowerCase().includes('cold') || cargoType.toLowerCase().includes('chilled');
      const isColdTruck = truck.CargoType.toLowerCase().includes('cold') || truck.CargoType.toLowerCase().includes('chilled');
      const tempMatch = isColdCargo ? isColdTruck : true; // Dry cargo can be hauled in cold truck if needed, but cold needs cold

      let score = 0;
      if (isAvailable) score += 50;
      if (tempMatch) score += 30;
      if (capFits) score += 20;

      // Penalize excessive oversize capacity
      const excess = Number(truck.CapacityCrates) - Number(requiredCapacity);
      if (capFits && excess > 100) score -= 5;

      return {
        ...truck,
        isAvailable,
        capFits,
        tempMatch,
        score,
        badgeText: !isAvailable ? 'Unavailable' : (!tempMatch ? 'Temp Mismatch' : (!capFits ? 'Under Capacity' : 'Recommended'))
      };
    }).sort((a, b) => b.score - a.score);
  }

  dispatchLoadingSchedule(scheduleId, dispatchData, user) {
    const schedule = this.getById('LoadingSchedules', scheduleId);
    if (!schedule) throw new Error('Loading Schedule not found.');

    const now = new Date();
    const dspDate = dispatchData.DispatchDate || now.toISOString().substring(0, 10);
    const dspNum = `DSP-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(100000 + Math.random() * 900000))}`;

    const deliveryRecord = {
      DispatchNumber: dspNum,
      LoadingScheduleId: Number(schedule.Id),
      LoadingScheduleInfo: `${schedule.ClientName} (${schedule.Destination})`,
      TruckId: Number(dispatchData.TruckId),
      TruckInfo: dispatchData.TruckInfo,
      DriverId: Number(dispatchData.DriverId),
      DriverName: dispatchData.DriverName,
      AideId: Number(dispatchData.AideId || 0),
      AideName: dispatchData.AideName || 'None',
      AideIds: dispatchData.AideIds || '',
      AideNames: dispatchData.AideNames || '',
      DispatchDate: dspDate,
      DepartureTime: dispatchData.DepartureTime || schedule.DepartureTime || '08:00 AM',
      Status: 'In Transit',
      Remarks: dispatchData.Remarks || 'Dispatched Delivery Order',
      GCrate: Number(schedule.GCrate || 0),
      ECrate: Number(schedule.ECrate || 0),
      TotalCrates: Number(schedule.TotalCrates || 0),
      CancellationReason: '',
      CargoType: schedule.CargoType || 'Cold / Chilled',
      IsMultiDrop: dispatchData.IsMultiDrop ? 1 : 0,
      TotalStopsCount: 1
    };

    const insertedDelivery = this.insert('Deliveries', deliveryRecord, user);

    // Update Schedule to Dispatched
    this.update('LoadingSchedules', schedule.Id, { Status: 'Dispatched' }, user);

    // Mark Truck In Use
    this.update('Trucks', dispatchData.TruckId, { Status: 'In Use' }, user);

    // Mark Driver unavailable
    this.update('Crews', dispatchData.DriverId, { IsAvailable: 0 }, user);

    // Mark Aides unavailable
    if (dispatchData.AideIds) {
      const aideIdList = String(dispatchData.AideIds).split(',').map(s => s.trim()).filter(Boolean);
      for (const aId of aideIdList) {
        this.update('Crews', aId, { IsAvailable: 0 }, user);
      }
    } else if (dispatchData.AideId) {
      this.update('Crews', dispatchData.AideId, { IsAvailable: 0 }, user);
    }

    this.addNotification(
      'New Trip Dispatched',
      `Dispatch ${dspNum} has departed for ${schedule.Destination} with Truck ${dispatchData.TruckInfo}.`,
      'info',
      'DispatchManagement'
    );

    return insertedDelivery;
  }

  completeTrip(deliveryId, tripData, user) {
    const delivery = this.getById('Deliveries', deliveryId);
    if (!delivery) throw new Error('Delivery not found.');

    const now = new Date();
    const repNum = `TRIP-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(100000 + Math.random() * 900000))}`;

    const deliveredG = Number(tripData.DeliveredGCrates || 0);
    const deliveredE = Number(tripData.DeliveredECrates || 0);
    const totalDelivered = deliveredG + deliveredE;

    const returnedG = Number(tripData.ReturnedGCrates || 0);
    const returnedE = Number(tripData.ReturnedECrates || 0);
    const totalReturned = returnedG + returnedE;

    const tripReport = {
      TripReportNumber: repNum,
      DeliveryId: Number(delivery.Id),
      DispatchNumber: delivery.DispatchNumber,
      ArrivalTime: tripData.ArrivalTime || '10:00 AM',
      DepartureTime: tripData.DepartureTime || '11:30 AM',
      DeliveredCrates: totalDelivered,
      ReturnedCrates: totalReturned,
      StoreManagerName: tripData.StoreManagerName || '',
      StoreManagerLastName: tripData.StoreManagerLastName || '',
      StoreManagerFirstName: tripData.StoreManagerFirstName || '',
      StoreManagerMiddleName: tripData.StoreManagerMiddleName || '',
      Remarks: tripData.Remarks || 'Trip Completed Successfully',
      ReportDate: now.toISOString().substring(0, 10),
      DeliveredGCrates: deliveredG,
      DeliveredECrates: deliveredE,
      ReturnedGCrates: returnedG,
      ReturnedECrates: returnedE
    };

    const insertedReport = this.insert('TripReports', tripReport, user);

    // Update Delivery to Completed
    this.update('Deliveries', delivery.Id, { Status: 'Completed' }, user);

    // Update Loading Schedule to Completed
    if (delivery.LoadingScheduleId) {
      this.update('LoadingSchedules', delivery.LoadingScheduleId, { Status: 'Completed' }, user);
    }

    // Release Truck to Available
    if (delivery.TruckId) {
      this.update('Trucks', delivery.TruckId, { Status: 'Available' }, user);
    }

    // Release Driver to Available
    if (delivery.DriverId) {
      this.update('Crews', delivery.DriverId, { IsAvailable: 1 }, user);
    }

    // Release Aides
    if (delivery.AideIds) {
      const aideIdList = String(delivery.AideIds).split(',').map(s => s.trim()).filter(Boolean);
      for (const aId of aideIdList) {
        this.update('Crews', aId, { IsAvailable: 1 }, user);
      }
    } else if (delivery.AideId) {
      this.update('Crews', delivery.AideId, { IsAvailable: 1 }, user);
    }

    // Check for discrepancy between Dispatched Crates and Delivered Crates
    const dispatchedTotal = Number(delivery.TotalCrates || 0);
    if (dispatchedTotal > 0 && totalDelivered < dispatchedTotal) {
      const variance = dispatchedTotal - totalDelivered;
      const incidentDate = now.toISOString().substring(0, 10);
      const incidentRecord = {
        DeliveryId: Number(delivery.Id),
        DispatchNumber: delivery.DispatchNumber,
        IncidentDate: incidentDate,
        DamagedQuantity: variance,
        EstimatedCost: variance * 450, // Standard 450 PHP per crate liability
        Cause: 'Missing / Damaged Crates from Trip Delivery',
        ChargeableToCrew: 1,
        Description: `Automated variance log: ${variance} crate(s) discrepancy noted on delivery ${delivery.DispatchNumber} upon completion.`,
        Status: 'Logged',
        PhotoPath: ''
      };
      this.insert('IncidentReports', incidentRecord, user);
      this.addNotification(
        '⚠️ Cargo Discrepancy Logged',
        `Discrepancy of ${variance} crate(s) on ${delivery.DispatchNumber}. Auto-generated Incident Report.`,
        'warning',
        'IncidentReports'
      );
    }

    this.addNotification(
      'Trip Completed',
      `Trip Report ${repNum} created. Truck & crew released to Available.`,
      'success',
      'TripReports'
    );

    return insertedReport;
  }

  createBillingInvoice(invoiceData, user) {
    const now = new Date();
    const invNum = `INV-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(10000 + Math.random() * 90000))}`;
    
    // Default 15-day due date
    const dueDateObj = new Date(now.getTime() + 15 * 24 * 60 * 60 * 1000);
    const dueDate = dueDateObj.toISOString().substring(0, 10);

    const gross = Number(invoiceData.TotalAmount || 0);
    const deductions = Number(invoiceData.Deductions || 0);
    const net = Math.max(0, gross - deductions);

    const record = {
      InvoiceNumber: invNum,
      SoaId: Number(invoiceData.SoaId || 0),
      DeliveryId: Number(invoiceData.DeliveryId || 0),
      ClientName: invoiceData.ClientName || 'RED RIBBON BAKESHOP INC.',
      IssueDate: invoiceData.IssueDate || now.toISOString().substring(0, 10),
      DueDate: invoiceData.DueDate || dueDate,
      TotalAmount: gross,
      Deductions: deductions,
      NetAmount: net,
      Status: 'Unpaid'
    };

    return this.insert('Invoices', record, user);
  }

  recordCollectionPayment(invoiceId, paymentData, user) {
    const invoice = this.getById('Invoices', invoiceId);
    if (!invoice) throw new Error('Invoice not found.');

    const now = new Date();
    const rcNum = `CR-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(10000 + Math.random() * 90000))}`;

    const amountPaid = Number(paymentData.AmountPaid || 0);
    const receipt = {
      ReceiptNumber: rcNum,
      InvoiceId: Number(invoice.Id),
      InvoiceNumber: invoice.InvoiceNumber,
      PaymentDate: paymentData.PaymentDate || now.toISOString().substring(0, 10),
      AmountPaid: amountPaid,
      PaymentMethod: paymentData.PaymentMethod || 'CASH',
      ReferenceNumber: paymentData.ReferenceNumber || '',
      Notes: paymentData.Notes || ''
    };

    const insertedReceipt = this.insert('CollectionReceipts', receipt, user);

    // Calculate sum of payments for this invoice
    const allReceipts = this.get('CollectionReceipts').filter(r => Number(r.InvoiceId) === Number(invoice.Id));
    const totalCollected = allReceipts.reduce((sum, r) => sum + Number(r.AmountPaid || 0), 0);

    let newStatus = 'Unpaid';
    if (totalCollected >= Number(invoice.NetAmount)) {
      newStatus = 'Paid';
    } else if (totalCollected > 0) {
      newStatus = 'Partially Paid';
    }

    this.update('Invoices', invoice.Id, { Status: newStatus }, user);

    this.addNotification(
      'Payment Recorded',
      `Official Receipt ${rcNum} issued for ${invoice.InvoiceNumber}: PHP ${amountPaid.toLocaleString('en-US', { minimumFractionDigits: 2 })}.`,
      'success',
      'Collections'
    );

    return insertedReceipt;
  }

  getDashboardKPIs() {
    const trucks = this.get('Trucks');
    const crews = this.get('Crews');
    const schedules = this.get('LoadingSchedules');
    const deliveries = this.get('Deliveries');
    const invoices = this.get('Invoices');
    const receipts = this.get('CollectionReceipts');

    const availableTrucks = trucks.filter(t => t.Status === 'Available').length;
    const inUseTrucks = trucks.filter(t => t.Status === 'In Use').length;
    const maintenanceTrucks = trucks.filter(t => t.Status === 'Under Maintenance').length;

    const availableDrivers = crews.filter(c => c.Role.toUpperCase().includes('DRIVER') && c.IsAvailable && c.IsActive).length;
    const availableAides = crews.filter(c => c.Role.toUpperCase().includes('AIDE') && c.IsAvailable && c.IsActive).length;

    const activeDispatches = deliveries.filter(d => d.Status === 'In Transit' || d.Status === 'Dispatched').length;
    const completedTrips = deliveries.filter(d => d.Status === 'Completed').length;
    const pendingOrders = schedules.filter(s => s.Status === 'Pending').length;

    const totalRevenue = receipts.reduce((sum, r) => sum + Number(r.AmountPaid || 0), 0);
    const unpaidInvoices = invoices.filter(i => i.Status !== 'Paid');
    const outstandingReceivables = unpaidInvoices.reduce((sum, i) => sum + Number(i.NetAmount || 0), 0);

    return {
      availableTrucks,
      inUseTrucks,
      maintenanceTrucks,
      totalTrucks: trucks.length,
      availableDrivers,
      availableAides,
      activeDispatches,
      completedTrips,
      pendingOrders,
      totalRevenue,
      outstandingReceivables
    };
  }
}

window.appStore = new Store();
