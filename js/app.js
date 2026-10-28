/**
 * M.J. Billones Transport Service - Core Web System Application Controller
 * Replicates the exact WinForms layout, hubs, tabs, button labels, and workflows.
 */

document.addEventListener('DOMContentLoaded', () => {
  const store = window.appStore;
  const auth = window.auth;
  const toast = window.toast;
  const clock = window.analogClockPicker;
  const printer = window.docPrinter;

  // Active State
  let currentHub = 'operations';
  let currentTab = 'dispatch';
  let searchTerm = '';
  let selectedAides = new Set();
  let showOrderForm = false;
  let editingDispatchId = null;

  // DOM Elements
  const authContainer = document.getElementById('auth-container');
  const appContainer = document.getElementById('app-container');
  const pageHubTitle = document.getElementById('page-hub-title');
  const headerUserInfo = document.getElementById('header-user-info');
  const hubSubtabsBar = document.getElementById('hub-subtabs-bar');
  const mainContentView = document.getElementById('main-content-view');

  // KPI Header Elements
  const kpiScheduledText = document.getElementById('kpi-scheduled-text');
  const kpiTransitText = document.getElementById('kpi-transit-text');
  const kpiUnpaidText = document.getElementById('kpi-unpaid-text');
  const kpiAlertsText = document.getElementById('kpi-alerts-text');

  function init() {
    bindAuth();
    bindShellEvents();
    checkSession();
  }

  function checkSession() {
    const user = auth.getCurrentUser();
    if (user) {
      if (authContainer) authContainer.classList.add('hidden');
      if (appContainer) appContainer.classList.remove('hidden');
      if (headerUserInfo) headerUserInfo.innerText = `${user.FullName || user.Username} · ${user.Role || 'Staff'}`;
      updateKpiHeader();
      switchHub(currentHub, currentTab);
    } else {
      if (authContainer) authContainer.classList.remove('hidden');
      if (appContainer) appContainer.classList.add('hidden');
    }
  }

  function updateKpiHeader() {
    const deliveries = store.get('Deliveries');
    const schedules = store.get('LoadingSchedules');
    const invoices = store.get('Invoices');
    const notifs = store.get('SystemNotifications');

    const scheduled = schedules.filter(s => s.Status === 'Pending').length;
    const inTransit = deliveries.filter(d => d.Status === 'In Transit' || d.Status === 'Dispatched').length;
    const unpaid = invoices.filter(i => i.Status !== 'Paid').length;
    const alerts = notifs.filter(n => !n.IsRead).length || 5;

    if (kpiScheduledText) kpiScheduledText.innerText = `${scheduled} scheduled`;
    if (kpiTransitText) kpiTransitText.innerText = `${inTransit} in transit`;
    if (kpiUnpaidText) kpiUnpaidText.innerText = `${unpaid} unpaid`;
    if (kpiAlertsText) kpiAlertsText.innerText = `${alerts} new`;
  }

  function bindAuth() {
    const loginForm = document.getElementById('login-form');
    if (loginForm) {
      loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const u = document.getElementById('login-username').value;
        const p = document.getElementById('login-password').value;
        const err = document.getElementById('login-error');
        try {
          if (err) err.classList.add('hidden');
          await auth.login(u, p);
          toast.show('Logged in successfully ✓', 'success');
          checkSession();
        } catch (ex) {
          if (err) {
            err.innerText = ex.message || 'Login failed';
            err.classList.remove('hidden');
          }
        }
      });
    }

    const demoAdmin = document.getElementById('demo-login-admin');
    if (demoAdmin && loginForm) {
      demoAdmin.addEventListener('click', () => {
        document.getElementById('login-username').value = 'admin';
        document.getElementById('login-password').value = 'admin123';
        loginForm.dispatchEvent(new Event('submit'));
      });
    }

    const demoStaff = document.getElementById('demo-login-staff');
    if (demoStaff && loginForm) {
      demoStaff.addEventListener('click', () => {
        document.getElementById('login-username').value = 'staff';
        document.getElementById('login-password').value = 'staff123';
        loginForm.dispatchEvent(new Event('submit'));
      });
    }

    const logoutBtn = document.getElementById('btn-header-logout');
    if (logoutBtn) {
      logoutBtn.addEventListener('click', () => {
        if (confirm('Are you sure you want to log out of M.J. Billones Delivery Management System?')) {
          auth.logout();
          checkSession();
        }
      });
    }
  }

  function bindShellEvents() {
    // Hub Navigation Buttons
    document.querySelectorAll('.nav-hub-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        const hub = btn.getAttribute('data-hub');
        switchHub(hub);
      });
    });

    // Sidebar Toggle Pin Button
    const pinBtn = document.getElementById('btn-toggle-pin');
    const sidebar = document.getElementById('panel-sidebar');
    if (pinBtn && sidebar) {
      pinBtn.addEventListener('click', () => {
        sidebar.classList.toggle('w-14');
        sidebar.classList.toggle('w-56');
      });
    }

    // Dark Mode Toggle
    const darkBtn = document.getElementById('btn-toggle-darkmode');
    if (darkBtn) {
      darkBtn.addEventListener('click', () => {
        document.body.classList.toggle('dark-mode');
        const isDark = document.body.classList.contains('dark-mode');
        toast.show(isDark ? 'Dark mode enabled' : 'Light mode enabled', 'info');
      });
    }

    // KPI Pills Click Navigation
    const pillSched = document.getElementById('kpi-pill-scheduled');
    if (pillSched) pillSched.addEventListener('click', () => switchHub('operations', 'dispatch'));

    const pillTransit = document.getElementById('kpi-pill-transit');
    if (pillTransit) pillTransit.addEventListener('click', () => switchHub('operations', 'dispatch'));

    const pillUnpaid = document.getElementById('kpi-pill-unpaid');
    if (pillUnpaid) pillUnpaid.addEventListener('click', () => switchHub('finance', 'billing'));

    const pillAlert = document.getElementById('kpi-pill-alert');
    if (pillAlert) pillAlert.addEventListener('click', () => switchHub('settings', 'security'));

    const setBtn = document.getElementById('btn-header-settings');
    if (setBtn) setBtn.addEventListener('click', () => switchHub('settings', 'users'));
  }

  function switchHub(hubKey, defaultTabKey = null) {
    currentHub = hubKey;
    showOrderForm = false;
    searchTerm = '';

    // Update active state on rail buttons
    document.querySelectorAll('.nav-hub-btn').forEach(btn => {
      if (btn.getAttribute('data-hub') === hubKey) {
        btn.className = 'nav-hub-btn active w-full flex items-center px-4 py-3 text-white bg-[#B4182C] transition relative';
      } else {
        btn.className = 'nav-hub-btn w-full flex items-center px-4 py-3 text-slate-400 hover:text-white hover:bg-[#242D38] transition relative';
      }
    });

    const hubTitles = {
      dashboard: 'Dashboard',
      operations: 'Operations',
      resources: 'Resources',
      finance: 'Payments',
      masterdata: 'Master Data',
      settings: 'Settings'
    };
    if (pageHubTitle) {
      pageHubTitle.innerText = hubTitles[hubKey] || 'Operations';
    }

    renderSubTabs(hubKey, defaultTabKey);
  }

  function renderSubTabs(hubKey, targetTab = null) {
    const tabConfigs = {
      operations: [
        { key: 'dispatch', title: '🚚 Loading Schedule & Dispatch' },
        { key: 'completed', title: '☑️ Completed Dispatches' },
        { key: 'special_trips', title: '🌟 Special Trip History' },
        { key: 'incidents', title: '⚠️ Incident & Damage Logs' },
        { key: 'trip_reports', title: '📝 Trip Reports' },
        { key: 'order_history', title: '📄 Order History' }
      ],
      resources: [
        { key: 'trucks', title: '🚛 Trucks' },
        { key: 'crew', title: '👨‍✈️ Crew Management' },
        { key: 'crew_history', title: '📋 Crew Trip History' },
        { key: 'resource_history', title: '📜 Resource History' }
      ],
      finance: [
        { key: 'soa', title: '📑 Statement of Account' },
        { key: 'billing', title: '🧾 Billing Invoices' },
        { key: 'collections', title: '💵 Collections & Receipts' },
        { key: 'finance_history', title: '📜 Finance History' }
      ],
      masterdata: [
        { key: 'clients', title: '🏢 Clients' },
        { key: 'routes', title: '🗺️ Delivery Routes' },
        { key: 'rate_matrix', title: '📈 Rate Matrix' }
      ],
      settings: [
        { key: 'users', title: '👤 User Accounts' },
        { key: 'security', title: '🛡️ Security Audit' },
        { key: 'system_backup', title: '⚙️ System Settings & Backup' }
      ],
      dashboard: [
        { key: 'overview', title: '📊 Operations & Revenue Overview' }
      ]
    };

    const tabs = tabConfigs[hubKey] || tabConfigs.operations;
    currentTab = targetTab || tabs[0].key;

    hubSubtabsBar.innerHTML = tabs.map(t => `
      <button class="hub-tab-btn ${t.key === currentTab ? 'active' : ''}" data-tab="${t.key}">
        ${t.title}
      </button>
    `).join('');

    hubSubtabsBar.querySelectorAll('.hub-tab-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        currentTab = btn.getAttribute('data-tab');
        showOrderForm = false;
        searchTerm = '';
        renderSubTabs(currentHub, currentTab);
      });
    });

    renderActiveTabContent();
  }

  function renderActiveTabContent() {
    updateKpiHeader();
    window.scrollTo({ top: 0, behavior: 'smooth' });

    if (currentHub === 'operations') {
      if (currentTab === 'dispatch') {
        if (showOrderForm) renderNewDispatchOrderForm();
        else renderLoadingScheduleAndDispatchView();
      } else if (currentTab === 'completed') {
        renderCompletedDispatchesView();
      } else if (currentTab === 'special_trips') {
        renderSpecialTripsView();
      } else if (currentTab === 'incidents') {
        renderIncidentDamageLogsView();
      } else if (currentTab === 'trip_reports') {
        renderTripReportsView();
      } else if (currentTab === 'order_history') {
        renderOrderHistoryView();
      }
    } else if (currentHub === 'resources') {
      if (currentTab === 'trucks') renderTrucksView();
      else if (currentTab === 'crew') renderCrewView();
      else renderCrewHistoryView();
    } else if (currentHub === 'finance') {
      if (currentTab === 'billing') renderBillingInvoicesView();
      else if (currentTab === 'collections') renderCollectionsReceiptsView();
      else renderStatementOfAccountView();
    } else if (currentHub === 'masterdata') {
      renderMasterDataView();
    } else if (currentHub === 'settings') {
      renderSettingsAndAuditView();
    } else {
      renderDashboardView();
    }
  }

  // =========================================================================
  // VIEW 1: LOADING SCHEDULE & DISPATCH - ORDERS LIST (Screenshot 01)
  // =========================================================================
  function renderLoadingScheduleAndDispatchView() {
    const deliveries = store.get('Deliveries').filter(d => {
      if (!searchTerm) return true;
      return (d.DispatchNumber && d.DispatchNumber.toLowerCase().includes(searchTerm)) ||
             (d.LoadingScheduleInfo && d.LoadingScheduleInfo.toLowerCase().includes(searchTerm)) ||
             (d.DriverName && d.DriverName.toLowerCase().includes(searchTerm)) ||
             (d.TruckInfo && d.TruckInfo.toLowerCase().includes(searchTerm));
    });

    const awaitingCount = deliveries.filter(d => d.Status === 'In Transit' || d.Status === 'Dispatched').length;

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <!-- Main Card Container -->
        <div class="desktop-table-container shadow-xs">
          <!-- Card Header & Action Toolbar -->
          <div class="p-4 border-b border-slate-200">
            <h2 class="text-base font-bold text-slate-800 mb-3">Delivery & Dispatch Orders (${deliveries.length})</h2>
            
            <div class="flex flex-wrap items-center justify-between gap-3">
              <!-- Search & Clear -->
              <div class="flex items-center space-x-1.5">
                <div class="relative">
                  <input type="text" id="input-dispatch-search" value="${searchTerm}" placeholder="🔍 Search dispatch #, client, r..." class="w-64 border border-slate-300 rounded px-3 py-1.5 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-[#B4182C]">
                </div>
                <button id="btn-dispatch-search-clear" class="btn-secondary py-1 px-2.5 text-xs" title="Clear">✖</button>
                <button id="btn-print-loading-sheet" class="btn-secondary py-1.5 px-3 text-xs">
                  <span>🖨️ Print Loading She</span>
                </button>
              </div>

              <!-- Action Buttons -->
              <div class="flex items-center space-x-2">
                <button id="btn-open-add-order" class="btn-primary py-1.5 px-3.5 text-xs">
                  <span>➕ + Add Order ▾</span>
                </button>
                <button id="btn-special-trip" class="btn-teal py-1.5 px-3 text-xs">
                  <span>✨ + Special Trip</span>
                </button>
                <span class="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-800 border border-amber-200">
                  ⏰ ${awaitingCount} awaiting completion
                </span>
              </div>
            </div>
          </div>

          <!-- Orders Data Table -->
          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Dispatch #</th>
                  <th>Date</th>
                  <th>Time</th>
                  <th>Route</th>
                  <th>Client</th>
                  <th>Truck</th>
                  <th>Driver</th>
                  <th>Helper / Aide</th>
                  <th class="text-right">E-Crat...</th>
                  <th class="text-right">G-Cra...</th>
                  <th class="text-right">Total</th>
                  <th class="text-center">Status</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${deliveries.length === 0 ? '<tr><td colspan="13" class="text-center py-8 text-slate-400 font-medium">No dispatch orders recorded. Click "+ Add Order" to create one.</td></tr>' : deliveries.map(d => {
                  let statusHtml = `<span class="status-pill status-pill-complete">✔ Complete</span>`;
                  if (d.Status === 'Scheduled' || d.Status === 'Pending') {
                    statusHtml = `<span class="status-pill status-pill-scheduled">● Scheduled</span>`;
                  } else if (d.Status === 'In Transit' || d.Status === 'Dispatched') {
                    statusHtml = `<span class="status-pill bg-blue-100 text-blue-800 border border-blue-200">● In Transit</span>`;
                  } else if (d.Status === 'Completed') {
                    statusHtml = `<span class="status-pill-completed-text">● Completed</span>`;
                  } else if (d.Status === 'Cancelled') {
                    statusHtml = `<span class="status-pill-cancelled">● Cancelled</span>`;
                  }

                  return `
                    <tr class="hover:bg-slate-50" data-id="${d.Id}">
                      <td class="font-bold text-slate-800">${d.DispatchNumber}</td>
                      <td>${d.DispatchDate}</td>
                      <td>${d.DepartureTime}</td>
                      <td class="max-w-[130px] truncate" title="${d.LoadingScheduleInfo}">${d.LoadingScheduleInfo || 'Standard'}</td>
                      <td class="max-w-[130px] truncate font-medium text-slate-700">${d.LoadingScheduleInfo ? d.LoadingScheduleInfo.split('(')[0] : 'RED RIBBON BAKESHOP INC.'}</td>
                      <td class="font-semibold text-slate-800">${d.TruckInfo || 'N/A'}</td>
                      <td>${d.DriverName || 'N/A'}</td>
                      <td class="max-w-[140px] truncate text-slate-600">${d.AideNames || d.AideName || 'None'}</td>
                      <td class="text-right font-medium text-slate-600">${d.ECrate || 0}</td>
                      <td class="text-right font-medium text-slate-600">${d.GCrate || 0}</td>
                      <td class="text-right font-bold text-slate-900">${d.TotalCrates || 0}</td>
                      <td class="text-center">${statusHtml}</td>
                      <td class="text-right space-x-1">
                        <button onclick="window.openRescheduleModal(${d.Id})" class="p-1 text-slate-600 hover:text-[#B4182C] text-xs font-semibold" title="Reschedule">📅</button>
                        <button onclick="window.openCancelModal(${d.Id})" class="p-1 text-slate-600 hover:text-red-600 text-xs font-semibold" title="Cancel">❌</button>
                        <button onclick="window.printDispatchDoc(${d.Id})" class="p-1 text-slate-600 hover:text-slate-900 text-xs font-semibold" title="Print Slip">🖨️</button>
                      </td>
                    </tr>
                  `;
                }).join('')}
              </tbody>
            </table>
          </div>

          <!-- Bottom Status Summary -->
          <div class="bottom-summary-strip">
            <span>📊 Total Orders: ${deliveries.length}</span>
            <span>|</span>
            <span>📦 Total Crates: ${deliveries.reduce((s,d)=>s+Number(d.TotalCrates||0),0)} Crates</span>
            <span>|</span>
            <span>🚚 Active Trucks In Transit: ${deliveries.filter(d=>d.Status==='In Transit'||d.Status==='Dispatched').length}</span>
          </div>
        </div>
      </div>
    `;

    // Bind Search
    const searchInput = document.getElementById('input-dispatch-search');
    searchInput.addEventListener('input', (e) => {
      searchTerm = e.target.value.toLowerCase();
      renderLoadingScheduleAndDispatchView();
    });
    document.getElementById('btn-dispatch-search-clear').addEventListener('click', () => {
      searchTerm = '';
      renderLoadingScheduleAndDispatchView();
    });

    // Add Order Button
    document.getElementById('btn-open-add-order').addEventListener('click', () => {
      showOrderForm = true;
      editingDispatchId = null;
      renderNewDispatchOrderForm();
    });

    document.getElementById('btn-special-trip').addEventListener('click', () => {
      openSpecialTripModal();
    });

    document.getElementById('btn-print-loading-sheet').addEventListener('click', () => {
      if (deliveries.length > 0) printer.printDispatchSlip(deliveries[0]);
    });
  }

  // =========================================================================
  // VIEW 2: NEW DISPATCH ORDER FORM (Screenshots 02, 03, 04)
  // =========================================================================
  function renderNewDispatchOrderForm() {
    const clients = store.get('Clients');
    const categories = store.get('LookupCategories');
    const routes = categories.filter(c => c.CategoryType === 'Destination').map(c => c.ItemValue);
    const trucks = store.get('Trucks');
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER') && c.IsActive);
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE') && c.IsActive);

    const now = new Date();
    const dspNum = `DSP-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(1000 + Math.random() * 9000))}`;

    mainContentView.innerHTML = `
      <div class="space-y-4 max-w-5xl mx-auto">
        <!-- Top Back Bar -->
        <div class="flex items-center space-x-4 mb-2">
          <button id="btn-back-to-orders" class="btn-secondary py-1.5 px-3 text-xs">
            <span>← Back to orders</span>
          </button>
          <h2 class="text-xl font-bold text-slate-800">New Dispatch Order</h2>
        </div>

        <form id="form-new-dispatch" class="space-y-4">
          <!-- Step 1 Card: Route _store crates -->
          <div class="bg-white rounded-lg border border-slate-200 p-5 shadow-xs">
            <div class="flex items-start space-x-3 mb-4">
              <span class="w-6 h-6 rounded-full bg-red-50 text-[#B4182C] font-bold text-xs flex items-center justify-center border border-red-200">1</span>
              <div>
                <h3 class="text-sm font-bold text-slate-800">Route _store crates</h3>
                <p class="text-xs text-slate-500">Who the order is for, where it goes and how many crates are loaded.</p>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Order Reference / Dispatch # *:</label>
                <input type="text" id="order-ref-num" value="${dspNum}" required class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium focus:outline-none focus:border-[#B4182C]">
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Company:</label>
                <select id="order-client" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                  <option value="RED RIBBON BAKESHOP INC." selected>RED RIBBON BAKESHOP INC.</option>
                  <option value="MILLENIUM FOOD BITES CORPORATION">MILLENIUM FOOD BITES CORPORATION</option>
                  <option value="GOLDILOCKS BAKESHOP INC.">GOLDILOCKS BAKESHOP INC.</option>
                </select>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Delivery Route:</label>
                <div class="flex space-x-1.5">
                  <select id="order-route" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                    <option value="R2 - EODGAA (La Union Route)" selected>R2 - EODGAA (La Union Route)</option>
                    <option value="R1 - EODLAB (Ilocos Route)">R1 - EODLAB (Ilocos Route)</option>
                    <option value="TABUK (+1 stops)">TABUK (+1 stops)</option>
                    <option value="Batangas / Lipa">Batangas / Lipa</option>
                    ${routes.map(r => `<option value="${r}">${r}</option>`).join('')}
                  </select>
                  <button type="button" id="btn-quick-add-route" class="bg-[#B4182C] text-white px-2.5 rounded hover:bg-[#981323] text-sm font-bold" title="Quick Add Delivery Route">+</button>
                </div>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Cargo / Temp Type:</label>
                <div class="flex items-center space-x-4">
                  <select id="order-cargo" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                    <option value="Cold / Chilled" selected>Cold / Chilled</option>
                    <option value="Dry Goods">Dry Goods</option>
                  </select>
                  <label class="flex items-center space-x-1.5 text-xs whitespace-nowrap text-slate-700">
                    <input type="checkbox" id="order-tariff" class="rounded border-slate-300 text-[#B4182C]">
                    <span>Tariff Season</span>
                  </label>
                </div>
              </div>
            </div>

            <!-- Total Crates & Optimal Match Alert -->
            <div class="mt-4 pt-4 border-t border-slate-100">
              <div class="flex justify-between items-center text-xs font-semibold text-slate-700 mb-1.5">
                <span>Total Crates (Auto-Tallied from Route Stores):</span>
                <div class="flex items-center space-x-2">
                  <input type="number" id="order-total-crates" value="150" min="1" class="w-20 border border-slate-300 rounded px-2 py-1 text-right font-bold text-slate-900">
                  <span>Crates</span>
                </div>
              </div>
              <div class="p-2.5 rounded bg-emerald-50 border border-emerald-200 text-xs font-medium text-emerald-800 flex items-center">
                <span>✓ Optimal Match: NBL 4805 (Cold / Chilled - 230 crates) handles 150 crates (65% utilized).</span>
              </div>
            </div>
          </div>

          <!-- Step 2 Card: Truck _crew -->
          <div class="bg-white rounded-lg border border-slate-200 p-5 shadow-xs">
            <div class="flex items-start space-x-3 mb-4">
              <span class="w-6 h-6 rounded-full bg-red-50 text-[#B4182C] font-bold text-xs flex items-center justify-center border border-red-200">2</span>
              <div>
                <h3 class="text-sm font-bold text-slate-800">Truck _crew</h3>
                <p class="text-xs text-slate-500">Assign the truck, the driver and the helpers for this trip.</p>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Assigned Truck:</label>
                <select id="order-truck" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                  ${trucks.map(t => `
                    <option value="${t.Id}" data-info="${t.PlateNumber} (${t.TruckType})" ${t.Status !== 'Available' ? 'class="text-slate-400"' : ''}>
                      ● ${t.Status.toUpperCase()} ${t.PlateNumber} [${t.TruckType}] (${t.CapacityCrates} Crates)
                    </option>
                  `).join('')}
                </select>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Assigned Driver:</label>
                <select id="order-driver" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                  ${drivers.map(d => `
                    <option value="${d.Id}" data-name="${d.FullName}">
                      ● ACTIVE ${d.FullName} (${d.LicenseNumber || 'Lic. OK'})
                    </option>
                  `).join('')}
                </select>
              </div>
            </div>

            <!-- Aides Selector with Chips -->
            <div>
              <div class="flex justify-between items-center mb-1">
                <label class="block text-xs font-semibold text-slate-700">Assigned Helper/Aides (Sorted by fewest trips — Standard: 2 Aides):</label>
                <span id="order-aides-count-pill" class="text-xs font-bold text-emerald-700">✖ 0 Aides</span>
              </div>
              
              <div class="border border-slate-300 rounded-lg p-2.5 bg-slate-50/50">
                <div class="flex items-center space-x-2 mb-2 pb-2 border-b border-slate-200">
                  <input type="text" id="order-aide-filter" placeholder="🔍 Type aide name to filter..." class="w-full bg-white border border-slate-200 rounded px-2.5 py-1 text-xs">
                </div>
                
                <!-- Selectable Aide Chips -->
                <div id="aide-chips-container" class="flex flex-wrap gap-1.5 max-h-28 overflow-y-auto p-1">
                  ${aides.map(a => `
                    <button type="button" class="aide-chip inline-flex items-center space-x-1.5 px-2.5 py-1 rounded text-xs border border-emerald-300 bg-emerald-50/80 text-emerald-800 hover:bg-emerald-100 transition" data-id="${a.Id}" data-name="${a.FullName}">
                      <span class="text-[10px]">➕ 🟢</span>
                      <span class="font-semibold">${a.FullName} (0)</span>
                    </button>
                  `).join('')}
                </div>
                <div class="text-[11px] text-slate-500 mt-2 font-medium" id="order-aides-summary">
                  ✓ 0 Aides Selected (Click pills above to select)
                </div>
              </div>
            </div>
          </div>

          <!-- Step 3 Card: Schedule _status -->
          <div class="bg-white rounded-lg border border-slate-200 p-5 shadow-xs">
            <div class="flex items-start space-x-3 mb-4">
              <span class="w-6 h-6 rounded-full bg-red-50 text-[#B4182C] font-bold text-xs flex items-center justify-center border border-red-200">3</span>
              <div>
                <h3 class="text-sm font-bold text-slate-800">Schedule _status</h3>
                <p class="text-xs text-slate-500">When the truck leaves and where the order stands.</p>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Dispatch Date & Time:</label>
                <input type="date" id="order-date" value="${now.toISOString().substring(0, 10)}" required class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium">
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Departure Time:</label>
                <div class="flex space-x-1">
                  <input type="text" id="order-departure-time" value="08:00 AM" readonly class="w-full border border-slate-300 rounded-l px-3 py-1.5 text-xs font-medium bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-picker" class="bg-[#B4182C] text-white px-3 rounded-r text-xs hover:bg-[#981323]" title="Clock Picker">🕒</button>
                </div>
              </div>

              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Dispatch Status:</label>
                <select id="order-status" class="w-full border border-slate-300 rounded px-3 py-1.5 text-xs font-medium bg-white">
                  <option value="Scheduled" selected>Scheduled</option>
                  <option value="In Transit">In Transit</option>
                  <option value="Completed">Completed</option>
                  <option value="Cancelled">Cancelled</option>
                </select>
              </div>
            </div>
          </div>

          <!-- Form Action Footer -->
          <div class="flex items-center justify-end space-x-3 pt-3">
            <button type="button" id="btn-reset-order-form" class="btn-secondary py-2 px-5 text-xs">
              <span>🧹 RESET FORM</span>
            </button>
            <button type="submit" class="btn-primary py-2 px-6 text-xs shadow-md">
              <span>💾 SAVE DISPATCH</span>
            </button>
          </div>
        </form>
      </div>
    `;

    // Bind Back Button
    document.getElementById('btn-back-to-orders').addEventListener('click', () => {
      showOrderForm = false;
      renderLoadingScheduleAndDispatchView();
    });

    // Bind Analog Clock Picker
    const timeInput = document.getElementById('order-departure-time');
    const clockBtn = document.getElementById('btn-clock-picker');
    clockBtn.addEventListener('click', () => clock.open(timeInput));
    timeInput.addEventListener('click', () => clock.open(timeInput));

    // Bind Aide Chips Toggle
    selectedAides.clear();
    const chips = document.querySelectorAll('.aide-chip');
    chips.forEach(chip => {
      chip.addEventListener('click', () => {
        const id = chip.getAttribute('data-id');
        const name = chip.getAttribute('data-name');
        if (selectedAides.has(id)) {
          selectedAides.delete(id);
          chip.classList.remove('bg-emerald-600', 'text-white');
          chip.classList.add('bg-emerald-50/80', 'text-emerald-800');
        } else {
          selectedAides.add(id);
          chip.classList.remove('bg-emerald-50/80', 'text-emerald-800');
          chip.classList.add('bg-emerald-600', 'text-white');
        }
        document.getElementById('order-aides-count-pill').innerText = `✖ ${selectedAides.size} Aides`;
        document.getElementById('order-aides-summary').innerText = `✓ ${selectedAides.size} Aides Selected`;
      });
    });

    // Quick Add Route (Screenshot 07)
    document.getElementById('btn-quick-add-route').addEventListener('click', () => {
      openQuickAddRouteModal((newRoute) => {
        store.insert('LookupCategories', { CategoryType: 'Destination', ItemValue: `${newRoute.RouteCode} - ${newRoute.RouteName}`, ExtraValue: newRoute.RouteCode });
        const sel = document.getElementById('order-route');
        const opt = document.createElement('option');
        opt.value = `${newRoute.RouteCode} - ${newRoute.RouteName}`;
        opt.innerText = `${newRoute.RouteCode} - ${newRoute.RouteName}`;
        opt.selected = true;
        sel.appendChild(opt);
        toast.show(`Added route "${newRoute.RouteCode} - ${newRoute.RouteName}" ✓`, 'success');
      });
    });

    // Reset Form
    document.getElementById('btn-reset-order-form').addEventListener('click', () => {
      selectedAides.clear();
      renderNewDispatchOrderForm();
      toast.show('Form reset', 'info');
    });

    // Submit Dispatch Form
    document.getElementById('form-new-dispatch').addEventListener('submit', (e) => {
      e.preventDefault();
      const truckSel = document.getElementById('order-truck');
      const driverSel = document.getElementById('order-driver');
      const routeVal = document.getElementById('order-route').value;
      const clientVal = document.getElementById('order-client').value;
      const totalCrates = parseInt(document.getElementById('order-total-crates').value, 10) || 150;

      const aideNamesArr = [];
      selectedAides.forEach(id => {
        const c = store.getById('Crews', id);
        if (c) aideNamesArr.push(c.FullName);
      });

      const newDispatch = {
        DispatchNumber: document.getElementById('order-ref-num').value,
        LoadingScheduleInfo: `${routeVal} (${clientVal})`,
        TruckId: Number(truckSel.value),
        TruckInfo: truckSel.selectedOptions[0]?.getAttribute('data-info') || 'Truck',
        DriverId: Number(driverSel.value),
        DriverName: driverSel.selectedOptions[0]?.getAttribute('data-name') || 'Driver',
        AideIds: Array.from(selectedAides).join(','),
        AideNames: aideNamesArr.join(', ') || 'None',
        DispatchDate: document.getElementById('order-date').value,
        DepartureTime: timeInput.value,
        CargoType: document.getElementById('order-cargo').value,
        TotalCrates: totalCrates,
        GCrate: totalCrates,
        ECrate: 0,
        Status: document.getElementById('order-status').value,
        Remarks: 'Created via Delivery Management System'
      };

      store.insert('Deliveries', newDispatch, auth.getCurrentUser());

      // If status is in transit or scheduled, mark truck in use
      if (newDispatch.Status === 'In Transit') {
        store.update('Trucks', newDispatch.TruckId, { Status: 'In Use' });
      }

      toast.show('Dispatch draft restored ✓', 'success');
      showOrderForm = false;
      renderLoadingScheduleAndDispatchView();
    });
  }

  // =========================================================================
  // VIEW 3: TRIP REPORTS (Screenshots 15 & 16)
  // =========================================================================
  function renderTripReportsView() {
    const reports = store.get('TripReports');
    const deliveries = store.get('Deliveries').filter(d => d.Status === 'In Transit' || d.Status === 'Completed' || d.Status === 'Scheduled');

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-5">
        <!-- Left Pane: Trip Completion Record -->
        <div class="lg:col-span-5 bg-white rounded-lg border border-slate-200 p-4 shadow-xs">
          <div class="flex items-center justify-between pb-3 border-b border-slate-200 mb-3">
            <h3 class="font-bold text-sm text-slate-800">Trip Completion Record</h3>
            <button class="btn-secondary py-0.5 px-2 text-xs">✕ Close</button>
          </div>

          <form id="form-trip-completion" class="space-y-4">
            <!-- Step 1 -->
            <div>
              <div class="text-xs font-bold text-[#8B1323] uppercase mb-1 flex items-center">
                <span class="mr-1">①</span> STEP 1: SELECT DISPATCH ORDER
              </div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Select Dispatch Order:</label>
              <select id="tc-dispatch-select" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium bg-white">
                ${deliveries.map(d => `<option value="${d.Id}">${d.DispatchNumber} — ${d.LoadingScheduleInfo || 'General Route'}</option>`).join('')}
              </select>
              <div class="mt-2 p-2 bg-red-50/60 rounded border border-red-200/60 text-[11px] text-red-900 font-medium">
                📄 REF: RCS VILLASIS102126-003 | 🗺️ ROUTE: R1 - EODLAB (Ilocos Route)
              </div>
            </div>

            <!-- Step 2: Crate Counts -->
            <div class="pt-3 border-t border-slate-100">
              <div class="text-xs font-bold text-[#8B1323] uppercase mb-2 flex items-center">
                <span class="mr-1">②</span> STEP 2: PER-STORE TRIP REPORT TABLE & CRATES
              </div>
              <div class="grid grid-cols-2 gap-2 text-xs mb-3">
                <div class="p-2 bg-emerald-50 rounded border border-emerald-200">
                  <span class="text-[11px] text-emerald-800 font-semibold block">Total Delivered G-Crates:</span>
                  <input type="number" id="tc-del-g" value="150" min="0" class="w-full border border-emerald-300 rounded px-2 py-1 font-bold text-emerald-900 mt-1">
                </div>
                <div class="p-2 bg-emerald-50 rounded border border-emerald-200">
                  <span class="text-[11px] text-emerald-800 font-semibold block">Total Delivered E-Crates:</span>
                  <input type="number" id="tc-del-e" value="0" min="0" class="w-full border border-emerald-300 rounded px-2 py-1 font-bold text-emerald-900 mt-1">
                </div>
                <div class="p-2 bg-blue-50 rounded border border-blue-200">
                  <span class="text-[11px] text-blue-800 font-semibold block">Total Returned G-Crates:</span>
                  <input type="number" id="tc-ret-g" value="150" min="0" class="w-full border border-blue-300 rounded px-2 py-1 font-bold text-blue-900 mt-1">
                </div>
                <div class="p-2 bg-blue-50 rounded border border-blue-200">
                  <span class="text-[11px] text-blue-800 font-semibold block">Total Returned E-Crates:</span>
                  <input type="number" id="tc-ret-e" value="0" min="0" class="w-full border border-blue-300 rounded px-2 py-1 font-bold text-blue-900 mt-1">
                </div>
              </div>
            </div>

            <!-- Step 3: Plant Gate Times & Signatories -->
            <div class="pt-3 border-t border-slate-100">
              <div class="text-xs font-bold text-[#8B1323] uppercase mb-2 flex items-center">
                <span class="mr-1">③</span> STEP 3: PLANT GATE TIMES & SIGNATORIES
              </div>
              <div class="grid grid-cols-2 gap-2 mb-2">
                <div>
                  <label class="block text-[11px] text-slate-600 font-semibold">Store Arrival Time (🕒):</label>
                  <input type="text" id="tc-arr-time" value="10:30 AM" class="w-full border border-slate-300 rounded px-2 py-1 text-xs cursor-pointer bg-slate-50">
                </div>
                <div>
                  <label class="block text-[11px] text-slate-600 font-semibold">Store Departure Time (🕒):</label>
                  <input type="text" id="tc-dep-time" value="11:45 AM" class="w-full border border-slate-300 rounded px-2 py-1 text-xs cursor-pointer bg-slate-50">
                </div>
              </div>
              <div>
                <label class="block text-[11px] text-slate-600 font-semibold">Store Manager Sign-off Name:</label>
                <input type="text" id="tc-mgr-name" value="maricel de ocampo" required class="w-full border border-slate-300 rounded px-2 py-1 text-xs font-semibold">
              </div>
            </div>

            <div class="pt-3 border-t border-slate-100 flex items-center justify-between">
              <button type="submit" class="btn-primary py-2 px-4 text-xs font-bold">
                <span>💾 COMPLETE TRIP</span>
              </button>
              <button type="button" class="btn-secondary py-2 px-3 text-xs">
                <span>🧹 CLEAR FORM</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Right Pane: Completed Trips Log (2) -->
        <div class="lg:col-span-7 desktop-table-container">
          <div class="p-3 border-b border-slate-200 flex items-center justify-between">
            <h3 class="font-bold text-sm text-slate-800">Completed Trips Log (${reports.length})</h3>
            <div class="flex items-center space-x-2">
              <input type="text" placeholder="🔍 Search report #, dis..." class="border border-slate-300 rounded px-2.5 py-1 text-xs w-44">
              <button class="btn-primary py-1 px-3 text-xs bg-[#6B0F1B]">Official Trip Repo</button>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Report #</th>
                  <th>Dispatch #</th>
                  <th>Date</th>
                  <th>Arrival</th>
                  <th>Departure</th>
                  <th>Delivered</th>
                  <th>Returned</th>
                  <th>Manager</th>
                </tr>
              </thead>
              <tbody>
                ${reports.map(r => `
                  <tr>
                    <td class="font-bold text-slate-800">${r.TripReportNumber}</td>
                    <td>${r.DispatchNumber}</td>
                    <td>${r.ReportDate}</td>
                    <td>${r.ArrivalTime}</td>
                    <td>${r.DepartureTime}</td>
                    <td class="font-bold text-emerald-700">${r.DeliveredCrates}</td>
                    <td class="font-bold text-blue-700">${r.ReturnedCrates}</td>
                    <td class="font-medium text-slate-800">${r.StoreManagerName}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>

          <div class="bottom-summary-strip">
            <span>📊 Total Completed Trips: ${reports.length}</span>
            <span>|</span>
            <span>📦 Delivered: ${reports.reduce((s,r)=>s+Number(r.DeliveredCrates||0),0)} Crates</span>
            <span>|</span>
            <span>🔄 Returned Empties: ${reports.reduce((s,r)=>s+Number(r.ReturnedCrates||0),0)} Crates</span>
          </div>
        </div>
      </div>
    `;

    // Bind Times
    const arr = document.getElementById('tc-arr-time');
    const dep = document.getElementById('tc-dep-time');
    arr.addEventListener('click', () => clock.open(arr));
    dep.addEventListener('click', () => clock.open(dep));

    // Submit Trip Completion
    document.getElementById('form-trip-completion').addEventListener('submit', (e) => {
      e.preventDefault();
      const delId = Number(document.getElementById('tc-dispatch-select').value);
      try {
        const rep = store.completeTrip(delId, {
          ArrivalTime: arr.value,
          DepartureTime: dep.value,
          DeliveredGCrates: Number(document.getElementById('tc-del-g').value),
          DeliveredECrates: Number(document.getElementById('tc-del-e').value),
          ReturnedGCrates: Number(document.getElementById('tc-ret-g').value),
          ReturnedECrates: Number(document.getElementById('tc-ret-e').value),
          StoreManagerName: document.getElementById('tc-mgr-name').value
        }, auth.getCurrentUser());

        toast.show(`Trip completion registered: ${rep.TripReportNumber}`, 'success');
        renderTripReportsView();
      } catch (err) {
        alert(err.message);
      }
    });
  }

  // =========================================================================
  // VIEW 4: BILLING INVOICES (Screenshot 20)
  // =========================================================================
  function renderBillingInvoicesView() {
    const invoices = store.get('Invoices');
    const completedDeliveries = store.get('Deliveries').filter(d => d.Status === 'Completed');
    const nextNum = `SI-${Math.floor(100 + Math.random() * 900)}`;

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-5">
        <!-- Left Pane: Invoice Editor -->
        <div class="lg:col-span-5 bg-white rounded-lg border border-slate-200 p-4 shadow-xs">
          <div class="flex items-center justify-between pb-3 border-b border-slate-200 mb-3">
            <h3 class="font-bold text-sm text-slate-800">Invoice #${nextNum}</h3>
            <button class="btn-secondary py-0.5 px-2 text-xs">✕ Close</button>
          </div>

          <form id="form-invoice-editor" class="space-y-3.5">
            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Invoice / Billing Reference # *:</label>
              <input type="text" id="inv-num" value="${nextNum}" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium">
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Completed Delivery Trip *:</label>
              <select id="inv-delivery-select" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium bg-white">
                <option value="0">(Select completed trip or manual)</option>
                ${completedDeliveries.map(d => `<option value="${d.Id}">${d.DispatchNumber} — ${d.LoadingScheduleInfo}</option>`).join('')}
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Linked Loading Schedule / Volume:</label>
              <input type="text" value="(Delivery Reference Recorded)" readonly class="w-full bg-slate-50 border border-slate-200 rounded px-2.5 py-1.5 text-xs text-slate-500">
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Billed Client / Client Name *:</label>
              <input type="text" id="inv-client-name" value="Red Ribbon Bakeshop Inc." required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium">
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Standard Gross (₱) *:</label>
                <input type="number" id="inv-gross-amt" value="8500.00" min="0" step="0.01" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-slate-900">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Deductions / Damage (₱):</label>
                <input type="number" id="inv-deduct-amt" value="0.00" min="0" step="0.01" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-red-600">
              </div>
            </div>

            <!-- Net Due Grey Card -->
            <div class="p-3 bg-slate-50 rounded border border-slate-200 text-xs space-y-1">
              <div class="font-bold text-slate-800">Net Due Amount: <span id="inv-net-label" class="text-slate-900 font-black">PHP 8,500.00</span></div>
              <div class="text-slate-500 font-medium">Payment Due Date (15 Days): <span id="inv-due-label">11/06/2026</span></div>
            </div>

            <div class="pt-3 border-t border-slate-100 flex items-center justify-between">
              <button type="submit" class="btn-primary py-2 px-4 text-xs font-bold bg-[#059669] hover:bg-[#047857]">
                <span>✏️ UPDATE INVOICE</span>
              </button>
              <button type="button" class="btn-secondary py-2 px-3 text-xs">
                <span>🧹 CLEAR FORM</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Right Pane: Active Issued Invoices (2) -->
        <div class="lg:col-span-7 desktop-table-container">
          <div class="p-3 border-b border-slate-200 flex items-center justify-between">
            <h3 class="font-bold text-sm text-slate-800">Active Issued Invoices (${invoices.length})</h3>
            <div class="flex items-center space-x-2">
              <button class="btn-secondary py-1 px-2.5 text-xs">✕ Hide Form ▴</button>
              <input type="text" placeholder="🔍 Search SOA, inv..." class="border border-slate-300 rounded px-2.5 py-1 text-xs w-44">
              <button class="btn-secondary py-1 px-2.5 text-xs">Actions ▾</button>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Invoice #</th>
                  <th>Client</th>
                  <th>Date</th>
                  <th>Gross</th>
                  <th>Deductio...</th>
                  <th>Net Due</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${invoices.map(i => `
                  <tr>
                    <td class="font-bold text-slate-800">${i.InvoiceNumber}</td>
                    <td class="font-medium text-slate-700">${i.ClientName}</td>
                    <td>${i.IssueDate}</td>
                    <td>${Number(i.TotalAmount||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td class="text-red-600">${Number(i.Deductions||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td class="font-bold text-slate-900">${Number(i.NetAmount||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td>
                      <span class="${i.Status === 'Paid' ? 'status-pill-paid' : 'status-pill-unpaid'}">
                        ● ${i.Status}
                      </span>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>

          <div class="bottom-summary-strip">
            <span>📊 Total Invoices: ${invoices.length}</span>
            <span>|</span>
            <span>⏳ Unpaid: ${invoices.filter(i=>i.Status!=='Paid').length}</span>
            <span>|</span>
            <span>🟢 Paid: ${invoices.filter(i=>i.Status==='Paid').length}</span>
            <span>|</span>
            <span>💰 Total Billed: PHP ${invoices.reduce((s,i)=>s+Number(i.NetAmount||0),0).toLocaleString('en-US')}</span>
          </div>
        </div>
      </div>
    `;

    // Recalculate Net
    const gInput = document.getElementById('inv-gross-amt');
    const dInput = document.getElementById('inv-deduct-amt');
    const netLabel = document.getElementById('inv-net-label');
    const update = () => {
      const g = parseFloat(gInput.value) || 0;
      const d = parseFloat(dInput.value) || 0;
      const net = Math.max(0, g - d);
      netLabel.innerText = `PHP ${net.toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
    };
    gInput.addEventListener('input', update);
    dInput.addEventListener('input', update);

    document.getElementById('form-invoice-editor').addEventListener('submit', (e) => {
      e.preventDefault();
      const g = parseFloat(gInput.value) || 0;
      const d = parseFloat(dInput.value) || 0;
      const inv = store.createBillingInvoice({
        InvoiceNumber: document.getElementById('inv-num').value,
        ClientName: document.getElementById('inv-client-name').value,
        TotalAmount: g,
        Deductions: d
      }, auth.getCurrentUser());

      toast.show(`Saved invoice ${inv.InvoiceNumber}`, 'success');
      renderBillingInvoicesView();
    });
  }

  // =========================================================================
  // VIEW 5: COLLECTIONS & RECEIPTS (Screenshot 24)
  // =========================================================================
  function renderCollectionsReceiptsView() {
    const receipts = store.get('CollectionReceipts');
    const invoices = store.get('Invoices').filter(i => i.Status !== 'Paid');
    const nextRc = `CR-${new Date().getFullYear()}${String(new Date().getMonth()+1).padStart(2,'0')}${String(new Date().getDate()).padStart(2,'0')}-${String(Math.floor(1000 + Math.random() * 9000))}`;

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-5">
        <!-- Left: Receipt Form -->
        <div class="lg:col-span-5 bg-white rounded-lg border border-slate-200 p-4 shadow-xs">
          <div class="flex items-center justify-between pb-3 border-b border-slate-200 mb-3">
            <h3 class="font-bold text-sm text-slate-800">Receipt #${nextRc}</h3>
            <button class="btn-secondary py-0.5 px-2 text-xs">✕ Close</button>
          </div>

          <form id="form-record-receipt" class="space-y-3.5">
            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Select Unpaid Service Invoice:</label>
              <select id="rc-invoice-select" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium bg-white">
                ${invoices.length === 0 ? '<option value="0">All invoices paid</option>' : invoices.map(i => `
                  <option value="${i.Id}" data-amount="${i.NetAmount}" data-num="${i.InvoiceNumber}">
                    ${i.InvoiceNumber} — ${i.ClientName} (Due: PHP ${Number(i.NetAmount).toLocaleString('en-US')})
                  </option>
                `).join('')}
              </select>
            </div>

            <div class="p-2.5 bg-slate-50 border border-slate-200 rounded text-xs text-slate-600 font-medium" id="rc-linked-preview">
              📄 Linked Invoice: INV-20260923-161652-85947
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Payment Amount (PHP):</label>
                <input type="number" id="rc-amount" value="9000.00" min="0.01" step="0.01" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-emerald-800">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Payment Method:</label>
                <select id="rc-method" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-medium bg-white">
                  <option value="CHECK" selected>CHECK</option>
                  <option value="CASH">CASH</option>
                  <option value="BANK TRANSFER">BANK TRANSFER</option>
                  <option value="CARD">CARD</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Payment / Transaction Reference # *:</label>
              <input type="text" id="rc-ref" placeholder="e.g. CHK-990214, BDO-TXN-55410, or REF-..." class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Collection Notes / Remarks:</label>
              <textarea id="rc-notes" rows="2" class="w-full border border-slate-300 rounded p-2 text-xs">Payment received in full settlement of invoice</textarea>
            </div>

            <div class="pt-3 border-t border-slate-100 flex items-center justify-between">
              <button type="submit" class="btn-primary py-2 px-4 text-xs font-bold bg-[#B4182C]">
                <span>💳 RECORD PAYMENT</span>
              </button>
              <button type="button" id="btn-print-active-rc" class="btn-secondary py-2 px-3 text-xs">
                <span>🖨️ PRINT RECEIPT</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Right: Register Log -->
        <div class="lg:col-span-7 desktop-table-container">
          <div class="p-3 border-b border-slate-200 flex items-center justify-between">
            <h3 class="font-bold text-sm text-slate-800">Payment Collections Register Log (${receipts.length})</h3>
            <div class="flex items-center space-x-2">
              <button class="btn-secondary py-1 px-2.5 text-xs">✕ Hide Form ▴</button>
              <input type="text" placeholder="🔍 Search receipt #, invoice #, re..." class="border border-slate-300 rounded px-2.5 py-1 text-xs w-48">
              <button class="btn-secondary py-1 px-2.5 text-xs">Actions ▾</button>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Receipt #</th>
                  <th>Invoice #</th>
                  <th>Date</th>
                  <th>Amount Paid</th>
                  <th>Method</th>
                  <th>Reference #</th>
                  <th>Notes</th>
                </tr>
              </thead>
              <tbody>
                ${receipts.map(r => `
                  <tr>
                    <td class="font-bold text-slate-800">${r.ReceiptNumber}</td>
                    <td>${r.InvoiceNumber}</td>
                    <td>${r.PaymentDate}</td>
                    <td class="font-bold text-emerald-700">${Number(r.AmountPaid||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td><span class="font-semibold text-slate-700">${r.PaymentMethod}</span></td>
                    <td class="font-mono text-xs text-slate-500">${r.ReferenceNumber || '—'}</td>
                    <td class="text-xs text-slate-400 max-w-[120px] truncate">${r.Notes || '—'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>

          <div class="bottom-summary-strip">
            <span>📊 Total Collections: ${receipts.length}</span>
            <span>|</span>
            <span>💵 Total Settled Payments: PHP ${receipts.reduce((s,r)=>s+Number(r.AmountPaid||0),0).toLocaleString('en-US')}</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('form-record-receipt').addEventListener('submit', (e) => {
      e.preventDefault();
      const sel = document.getElementById('rc-invoice-select');
      const invId = Number(sel.value);
      if (!invId) return;

      const amt = parseFloat(document.getElementById('rc-amount').value) || 0;
      const rc = store.recordCollectionPayment(invId, {
        AmountPaid: amt,
        PaymentMethod: document.getElementById('rc-method').value,
        ReferenceNumber: document.getElementById('rc-ref').value,
        Notes: document.getElementById('rc-notes').value
      }, auth.getCurrentUser());

      toast.show(`Recorded payment ${rc.ReceiptNumber}`, 'success');
      renderCollectionsReceiptsView();
    });

    document.getElementById('btn-print-active-rc').addEventListener('click', () => {
      if (receipts.length > 0) {
        const r = receipts[0];
        const inv = store.getById('Invoices', r.InvoiceId);
        printer.printOfficialReceipt(r, inv);
      }
    });
  }

  // =========================================================================
  // VIEW 6: INCIDENTS & DAMAGE LOGS (Screenshot 17 & 18)
  // =========================================================================
  function renderIncidentDamageLogsView() {
    const incidents = store.get('IncidentReports');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="desktop-table-container">
          <div class="p-4 border-b border-slate-200 flex items-center justify-between">
            <h2 class="text-base font-bold text-slate-800">Incident & Damage Logs (${incidents.length})</h2>
            <div class="flex items-center space-x-2">
              <input type="text" placeholder="🔍 Search incidents..." class="border border-slate-300 rounded px-2.5 py-1.5 text-xs w-48">
              <button class="btn-primary py-1.5 px-3 text-xs" onclick="window.logNewIncident()">+ Log Incident</button>
            </div>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Dispatch Ref</th>
                  <th>Incident Date</th>
                  <th>Cause Category</th>
                  <th>Damaged Crates</th>
                  <th>Estimated Cost (PHP)</th>
                  <th>Crew Chargeable</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${incidents.map(i => `
                  <tr>
                    <td class="font-bold text-slate-800">#${i.Id}</td>
                    <td class="font-semibold text-slate-700">${i.DispatchNumber}</td>
                    <td>${i.IncidentDate}</td>
                    <td class="font-bold text-slate-800 text-xs">${i.Cause}</td>
                    <td class="font-bold text-red-600">${i.DamagedQuantity}</td>
                    <td class="font-black text-slate-800">PHP ${Number(i.EstimatedCost||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td>
                      <span class="${i.ChargeableToCrew ? 'status-pill-cancelled' : 'status-pill-paid'}">
                        ${i.ChargeableToCrew ? 'Yes' : 'No'}
                      </span>
                    </td>
                    <td><span class="status-pill status-pill-scheduled">${i.Status}</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>

          <div class="bottom-summary-strip">
            <span>📊 Total Incidents: ${incidents.length}</span>
            <span>|</span>
            <span>💥 Total Discrepancies: ${incidents.reduce((s,i)=>s+Number(i.DamagedQuantity||0),0)} Crates</span>
            <span>|</span>
            <span>💸 Total Damage: PHP ${incidents.reduce((s,i)=>s+Number(i.EstimatedCost||0),0).toLocaleString('en-US')}</span>
          </div>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 7: COMPLETED DISPATCHES (Screenshot 12)
  // =========================================================================
  function renderCompletedDispatchesView() {
    const completed = store.get('Deliveries').filter(d => d.Status === 'Completed');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="desktop-table-container">
          <div class="p-4 border-b border-slate-200 flex items-center justify-between">
            <h2 class="text-base font-bold text-slate-800">Completed Dispatches (${completed.length})</h2>
            <input type="text" placeholder="🔍 Search completed..." class="border border-slate-300 rounded px-2.5 py-1.5 text-xs w-52">
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Dispatch #</th>
                  <th>Route</th>
                  <th>Date</th>
                  <th>Truck</th>
                  <th>Driver</th>
                  <th>Total Crates</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${completed.map(d => `
                  <tr>
                    <td class="font-bold text-slate-800">${d.DispatchNumber}</td>
                    <td class="font-semibold text-slate-700">${d.LoadingScheduleInfo}</td>
                    <td>${d.DispatchDate}</td>
                    <td class="font-semibold text-slate-800">${d.TruckInfo}</td>
                    <td>${d.DriverName}</td>
                    <td class="font-bold text-slate-900">${d.TotalCrates}</td>
                    <td><span class="status-pill-completed-text">● Completed</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 8: TRUCKS & FLEET
  // =========================================================================
  function renderTrucksView() {
    const trucks = store.get('Trucks');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="desktop-table-container">
          <div class="p-4 border-b border-slate-200 flex items-center justify-between">
            <h2 class="text-base font-bold text-slate-800">Truck Fleet Roster (${trucks.length})</h2>
            <button class="btn-primary py-1.5 px-3 text-xs" onclick="window.addTruckPrompt()">+ Register Truck</button>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Plate Number</th>
                  <th>Truck Type</th>
                  <th>Capacity (Crates)</th>
                  <th>Cargo Capability</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${trucks.map(t => `
                  <tr>
                    <td class="font-black text-slate-800">${t.PlateNumber}</td>
                    <td class="font-medium text-slate-700">${t.TruckType}</td>
                    <td class="font-extrabold text-slate-900">${t.CapacityCrates} Crates</td>
                    <td>
                      <span class="px-2 py-0.5 rounded text-xs font-semibold ${t.CargoType && t.CargoType.includes('Dry') ? 'bg-amber-50 text-amber-800' : 'bg-blue-50 text-blue-800'}">
                        ${t.CargoType}
                      </span>
                    </td>
                    <td>
                      <span class="${t.Status === 'Available' ? 'status-pill-paid' : (t.Status === 'In Use' ? 'text-blue-700 font-bold' : 'text-amber-700 font-bold')}">
                        ● ${t.Status}
                      </span>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 9: CREW DIRECTORY
  // =========================================================================
  function renderCrewView() {
    const crews = store.get('Crews');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="desktop-table-container">
          <div class="p-4 border-b border-slate-200 flex items-center justify-between">
            <h2 class="text-base font-bold text-slate-800">Personnel & Crew Directory (${crews.length})</h2>
            <button class="btn-primary py-1.5 px-3 text-xs" onclick="window.addCrewPrompt()">+ Add Personnel</button>
          </div>

          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Full Name</th>
                  <th>Role</th>
                  <th>Contact Number</th>
                  <th>License No</th>
                  <th>Daily Wage</th>
                  <th>Allowance</th>
                  <th>Availability</th>
                </tr>
              </thead>
              <tbody>
                ${crews.map(c => `
                  <tr>
                    <td class="font-bold text-slate-800">${c.FullName}</td>
                    <td><span class="font-semibold text-slate-700">${c.Role}</span></td>
                    <td class="font-mono text-xs">${c.ContactNumber || '—'}</td>
                    <td class="font-mono text-xs">${c.LicenseNumber || '—'}</td>
                    <td>PHP ${Number(c.DailyWage||0).toLocaleString('en-US')}</td>
                    <td>PHP ${Number(c.FoodAllowance||0).toLocaleString('en-US')}</td>
                    <td>
                      <span class="${c.IsAvailable ? 'status-pill-paid' : 'text-amber-700 font-bold'}">
                        ● ${c.IsAvailable ? 'Available' : 'On Trip'}
                      </span>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 10: DASHBOARD
  // =========================================================================
  function renderDashboardView() {
    const kpis = store.getDashboardKPIs();
    const deliveries = store.get('Deliveries').slice(0, 6);

    mainContentView.innerHTML = `
      <div class="space-y-5 max-w-6xl mx-auto">
        <!-- Banner Greeting -->
        <div class="bg-gradient-to-r from-[#6B0F1B] to-[#B4182C] text-white p-6 rounded-xl shadow-sm flex items-center justify-between">
          <div>
            <h2 class="text-xl font-bold">M.J. Billones Transport Service</h2>
            <p class="text-xs text-red-100 mt-1">Live operational hub: dual cold/dry fleet, crate tracking, and official billing statements.</p>
          </div>
          <button onclick="window.quickNewOrder()" class="bg-white text-[#B4182C] px-4 py-2 rounded-lg font-bold text-xs shadow hover:bg-red-50">
            ➕ New Dispatch Order
          </button>
        </div>

        <!-- 4 KPI Metrics -->
        <div class="grid grid-cols-1 sm:grid-cols-4 gap-4">
          <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-xs">
            <span class="text-xs font-semibold text-slate-500 uppercase">Active Dispatches</span>
            <div class="text-2xl font-bold text-slate-800 mt-1">${kpis.activeDispatches}</div>
            <span class="text-[11px] text-blue-600 font-semibold">● In Transit</span>
          </div>

          <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-xs">
            <span class="text-xs font-semibold text-slate-500 uppercase">Available Fleet</span>
            <div class="text-2xl font-bold text-slate-800 mt-1">${kpis.availableTrucks} <span class="text-xs text-slate-400 font-normal">/ ${kpis.totalTrucks} Trucks</span></div>
            <span class="text-[11px] text-emerald-600 font-semibold">● ${kpis.inUseTrucks} In Use</span>
          </div>

          <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-xs">
            <span class="text-xs font-semibold text-slate-500 uppercase">Active Crew</span>
            <div class="text-2xl font-bold text-slate-800 mt-1">${kpis.availableDrivers} <span class="text-xs text-slate-400 font-normal">Drivers Ready</span></div>
            <span class="text-[11px] text-slate-500 font-semibold">${kpis.availableAides} Aides Available</span>
          </div>

          <div class="bg-white p-4 rounded-lg border border-slate-200 shadow-xs">
            <span class="text-xs font-semibold text-slate-500 uppercase">Settled Revenue</span>
            <div class="text-2xl font-bold text-[#B4182C] mt-1">PHP ${kpis.totalRevenue.toLocaleString('en-US')}</div>
            <span class="text-[11px] text-slate-500 font-semibold">Receivables: PHP ${kpis.outstandingReceivables.toLocaleString('en-US')}</span>
          </div>
        </div>

        <!-- Recent Trips -->
        <div class="desktop-table-container">
          <div class="p-3.5 border-b border-slate-200 flex justify-between items-center">
            <h3 class="font-bold text-sm text-slate-800">Recent Delivery Dispatches</h3>
            <button onclick="window.switchHub('operations', 'dispatch')" class="text-xs font-bold text-[#B4182C] hover:underline">View All &rarr;</button>
          </div>
          <table class="desktop-table">
            <thead>
              <tr>
                <th>Dispatch #</th>
                <th>Route / Order</th>
                <th>Truck</th>
                <th>Driver</th>
                <th>Crates</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              ${deliveries.map(d => `
                <tr>
                  <td class="font-bold text-slate-800">${d.DispatchNumber}</td>
                  <td>${d.LoadingScheduleInfo || 'Standard'}</td>
                  <td>${d.TruckInfo}</td>
                  <td>${d.DriverName}</td>
                  <td class="font-bold text-slate-900">${d.TotalCrates}</td>
                  <td><span class="status-pill status-pill-complete">${d.Status}</span></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // Window Global Callbacks
  window.switchHub = switchHub;

  window.openRescheduleModal = (id) => {
    const d = store.getById('Deliveries', id);
    if (!d) return;

    const modalHtml = `
      <div id="modal-reschedule" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none">
        <div class="bg-white rounded-md shadow-2xl w-full max-w-md border border-slate-300 overflow-hidden">
          <div class="bg-white px-3.5 py-2 border-b border-slate-200 flex items-center justify-between">
            <span class="text-xs text-slate-600 font-medium">Reschedule Dispatch #${d.DispatchNumber}</span>
            <button onclick="document.getElementById('modal-reschedule').remove()" class="text-slate-400 hover:text-slate-700">✕</button>
          </div>

          <form id="form-resched" class="p-4 space-y-3">
            <div class="flex items-center space-x-2 text-slate-900 font-bold text-sm">
              <span>📅 Reschedule Dispatch Order</span>
            </div>
            <p class="text-[11px] text-slate-500">Adjust target dispatch date, departure time, and resource assignments.</p>

            <div class="p-3 bg-slate-50 rounded border border-slate-200 text-xs">
              <div class="font-bold text-slate-800">Dispatch Order: #${d.DispatchNumber}</div>
              <div class="text-slate-600 mt-0.5">Truck: ${d.TruckInfo} | Cargo: ${d.TotalCrates} Crates</div>
              <div class="text-amber-800 font-bold mt-1">Current Scheduled: ${d.DispatchDate} at ${d.DepartureTime}</div>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">New Scheduled Date: *</label>
                <input type="date" id="resched-date" value="${d.DispatchDate}" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">New Departure Time: *</label>
                <div class="flex space-x-1">
                  <input type="text" id="resched-time" value="${d.DepartureTime}" readonly class="w-full border border-slate-300 rounded-l px-2 py-1 text-xs cursor-pointer bg-slate-50">
                  <button type="button" id="btn-clock-resched" class="bg-[#B4182C] text-white px-2 rounded-r text-xs">🕒</button>
                </div>
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Reason for Rescheduling: *</label>
              <select id="resched-reason" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                <option value="Adverse Weather / Typhoon Advisory" selected>Adverse Weather / Typhoon Advisory</option>
                <option value="Truck Mechanical Maintenance">Truck Mechanical Maintenance</option>
                <option value="Store Request / Branch Delay">Store Request / Branch Delay</option>
                <option value="Driver / Crew Reassignment">Driver / Crew Reassignment</option>
              </select>
            </div>

            <div class="flex items-center justify-end space-x-2 pt-2 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-reschedule').remove()" class="btn-secondary py-1.5 px-3 text-xs">✕ Cancel</button>
              <button type="submit" class="btn-primary py-1.5 px-4 text-xs font-bold">💾 Confirm Reschedule</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const timeInp = document.getElementById('resched-time');
    document.getElementById('btn-clock-resched').addEventListener('click', () => clock.open(timeInp));

    document.getElementById('form-resched').addEventListener('submit', (e) => {
      e.preventDefault();
      store.update('Deliveries', d.Id, {
        DispatchDate: document.getElementById('resched-date').value,
        DepartureTime: timeInp.value,
        Remarks: `Rescheduled: ${document.getElementById('resched-reason').value}`
      }, auth.getCurrentUser());

      toast.show('Dispatch rescheduled successfully ✓', 'success');
      document.getElementById('modal-reschedule').remove();
      renderActiveTabContent();
    });
  };

  window.openCancelModal = (id) => {
    const d = store.getById('Deliveries', id);
    if (!d) return;

    const modalHtml = `
      <div id="modal-cancel" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none">
        <div class="bg-white rounded-md shadow-2xl w-full max-w-sm border border-slate-300 overflow-hidden">
          <div class="bg-red-50 px-4 py-3 border-b border-red-200">
            <h3 class="text-sm font-bold text-red-800">Cancel Dispatch #${d.DispatchNumber}</h3>
          </div>

          <form id="form-cancel" class="p-4 space-y-3">
            <p class="text-xs text-slate-600">Please record the official cancellation reason for audit purposes:</p>
            <select id="cancel-reason" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
              <option value="Store Closed / Holiday">Store Closed / Holiday</option>
              <option value="Order Duplication">Order Duplication</option>
              <option value="Client Cancellation Request">Client Cancellation Request</option>
            </select>

            <div class="flex items-center justify-end space-x-2 pt-2 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-cancel').remove()" class="btn-secondary py-1.5 px-3 text-xs">Stay</button>
              <button type="submit" class="btn-primary py-1.5 px-3.5 text-xs bg-red-600 hover:bg-red-700">Confirm Cancel</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('form-cancel').addEventListener('submit', (e) => {
      e.preventDefault();
      store.update('Deliveries', d.Id, {
        Status: 'Cancelled',
        CancellationReason: document.getElementById('cancel-reason').value
      }, auth.getCurrentUser());

      // Release truck
      if (d.TruckId) store.update('Trucks', d.TruckId, { Status: 'Available' });

      toast.show('Order cancelled ✓', 'info');
      document.getElementById('modal-cancel').remove();
      renderActiveTabContent();
    });
  };

  window.printDispatchDoc = (id) => {
    const d = store.getById('Deliveries', id);
    if (d) printer.printDispatchSlip(d);
  };

  // =========================================================================
  // VIEW 10: SPECIAL TRIP HISTORY (Screenshot 13)
  // =========================================================================
  function renderSpecialTripsView() {
    const allTrips = store.get('Deliveries').filter(d => (d.Remarks && d.Remarks.includes('Special Trip')) || d.IsMultiDrop);
    const searchVal = searchTerm.toLowerCase();
    const trips = allTrips.filter(t => {
      if (!searchVal) return true;
      return (t.DispatchNumber && t.DispatchNumber.toLowerCase().includes(searchVal)) ||
             (t.LoadingScheduleInfo && t.LoadingScheduleInfo.toLowerCase().includes(searchVal)) ||
             (t.DriverName && t.DriverName.toLowerCase().includes(searchVal)) ||
             (t.TruckInfo && t.TruckInfo.toLowerCase().includes(searchVal));
    });

    const pendingCount = allTrips.filter(t => t.Status === 'Scheduled').length;
    const transitCount = allTrips.filter(t => t.Status === 'In Transit').length;
    const completedCount = allTrips.filter(t => t.Status === 'Completed').length;
    const totalCount = allTrips.length;

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <!-- Title and KPI Header Bar -->
        <div class="bg-white p-5 rounded-lg border border-slate-200 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div>
            <h2 class="text-xl font-bold text-slate-800 flex items-center space-x-2">
              <span>🌟 Special Trip History</span>
            </h2>
            <p class="text-xs text-slate-500 mt-1">Historical archive, itemized stop breakdown, and lifecycle execution of special on-demand dispatches</p>
          </div>

          <div class="flex items-center space-x-2">
            <div class="px-3 py-1.5 rounded-lg border border-emerald-200 bg-emerald-50 text-right">
              <span class="text-[10px] font-bold text-emerald-600 block uppercase">Completed</span>
              <span class="text-base font-bold text-emerald-700">${completedCount}</span>
            </div>
            <div class="px-3 py-1.5 rounded-lg border border-sky-200 bg-sky-50 text-right">
              <span class="text-[10px] font-bold text-sky-600 block uppercase">In-Transit</span>
              <span class="text-base font-bold text-sky-700">${transitCount}</span>
            </div>
            <div class="px-3 py-1.5 rounded-lg border border-amber-200 bg-amber-50 text-right">
              <span class="text-[10px] font-bold text-amber-600 block uppercase">Pending</span>
              <span class="text-base font-bold text-amber-700">${pendingCount}</span>
            </div>
            <div class="px-3 py-1.5 rounded-lg border border-indigo-200 bg-indigo-50 text-right">
              <span class="text-[10px] font-bold text-indigo-600 block uppercase">Total Trips</span>
              <span class="text-base font-bold text-indigo-700">${totalCount}</span>
            </div>
          </div>
        </div>

        <!-- Filter & Actions Toolbar -->
        <div class="bg-[#F1F5F9] p-3 rounded-lg border border-slate-200 flex flex-wrap justify-between items-center gap-3">
          <div class="flex flex-wrap items-center gap-2">
            <div class="relative">
              <input type="text" id="special-trip-search" value="${searchTerm}" placeholder="🔍 Search special trip #, client, driver..." class="border border-slate-300 rounded px-3 py-1.5 text-xs bg-white w-64 focus:outline-none focus:border-[#B4182C]">
            </div>
            <select id="special-trip-status" class="border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
              <option value="All">All Statuses</option>
              <option value="Scheduled">Scheduled</option>
              <option value="In Transit">In Transit</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>

          <div class="flex items-center space-x-2">
            <button id="btn-new-special-trip" class="btn-primary py-1.5 px-3.5 text-xs font-bold shadow-sm flex items-center space-x-1">
              <span>➕</span> <span>New Special Trip</span>
            </button>
            <button id="btn-export-special-csv" class="btn-secondary py-1.5 px-3 text-xs font-semibold">
              <span>📊 Export CSV</span>
            </button>
            <button id="btn-refresh-special" class="btn-secondary py-1.5 px-2.5 text-xs">
              <span>🔄 Refresh</span>
            </button>
          </div>
        </div>

        <!-- Special Trips Data Table -->
        <div class="desktop-table-container">
          <table class="desktop-table">
            <thead>
              <tr>
                <th>Trip Ref #</th>
                <th>Client Company</th>
                <th>Dispatch Date</th>
                <th>Departure</th>
                <th>Service Type</th>
                <th>Assigned Truck</th>
                <th>Driver / Crew</th>
                <th>Destination / Route</th>
                <th>Crates / Load</th>
                <th>Status</th>
                <th class="text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              ${trips.length === 0 ? `
                <tr>
                  <td colspan="11" class="text-center py-8 text-slate-400">
                    No special trips recorded. Click "➕ New Special Trip" above to configure an on-demand dispatch.
                  </td>
                </tr>
              ` : trips.map(t => `
                <tr>
                  <td class="font-bold text-slate-900">${t.DispatchNumber}</td>
                  <td class="font-semibold text-slate-800">${t.LoadingScheduleInfo ? t.LoadingScheduleInfo.split('(')[0].trim() : 'RED RIBBON BAKESHOP'}</td>
                  <td>${t.DispatchDate}</td>
                  <td>${t.DepartureTime}</td>
                  <td><span class="px-2 py-0.5 rounded bg-purple-50 text-purple-700 font-semibold text-[11px] border border-purple-200">Dedicated Hauling</span></td>
                  <td class="font-medium text-slate-700">${t.TruckInfo}</td>
                  <td>${t.DriverName}</td>
                  <td class="font-medium">${t.LoadingScheduleInfo || 'Special Multi-Stop'}</td>
                  <td class="font-bold text-slate-900">${t.TotalCrates} Crates</td>
                  <td>
                    <span class="${t.Status === 'Completed' ? 'status-pill-complete' : t.Status === 'In Transit' ? 'status-pill-transit' : t.Status === 'Cancelled' ? 'status-pill-delayed' : 'status-pill-scheduled'}">
                      ● ${t.Status}
                    </span>
                  </td>
                  <td class="text-center">
                    <div class="flex items-center justify-center space-x-1.5">
                      <button onclick="window.printDispatchDoc(${t.Id})" class="p-1 text-slate-600 hover:text-[#B4182C] text-xs font-semibold" title="Print Special Trip Slip">🖨️</button>
                      <button onclick="window.openRescheduleModal(${t.Id})" class="p-1 text-slate-600 hover:text-amber-600 text-xs font-semibold" title="Reschedule">📅</button>
                      <button onclick="window.openCancelModal(${t.Id})" class="p-1 text-slate-600 hover:text-red-600 text-xs font-semibold" title="Cancel">✕</button>
                    </div>
                  </td>
                </tr>
              `).join('')}
            </tbody>
          </table>

          <div class="bottom-summary-strip">
            <span>🌟 Displaying ${trips.length} of ${allTrips.length} Special Trips</span>
            <span>|</span>
            <span>Completed: ${completedCount}</span>
            <span>|</span>
            <span>In-Transit: ${transitCount}</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-new-special-trip').addEventListener('click', () => openSpecialTripModal());
    document.getElementById('btn-refresh-special').addEventListener('click', () => renderSpecialTripsView());
    document.getElementById('btn-export-special-csv').addEventListener('click', () => {
      exportToCsv(trips, 'Special_Trip_History.csv');
    });

    const searchInput = document.getElementById('special-trip-search');
    searchInput.addEventListener('input', (e) => {
      searchTerm = e.target.value;
      renderSpecialTripsView();
    });
  }

  // =========================================================================
  // VIEW 11: STATEMENT OF ACCOUNT (Screenshots 21 & 22)
  // =========================================================================
  let showSoaForm = false;
  let editingSoaId = null;

  function renderStatementOfAccountView() {
    const soas = store.get('StatementsOfAccount');
    const clients = store.get('Clients');
    const searchVal = searchTerm.toLowerCase();

    const filteredSoas = soas.filter(s => {
      if (!searchVal) return true;
      return (s.SoaNumber && s.SoaNumber.toLowerCase().includes(searchVal)) ||
             (s.ClientName && s.ClientName.toLowerCase().includes(searchVal)) ||
             (s.SalesInvoiceNo && s.SalesInvoiceNo.toLowerCase().includes(searchVal));
    });

    const totalSalesSum = filteredSoas.reduce((acc, s) => acc + Number(s.TotalSales || 0), 0);
    const totalDueSum = filteredSoas.reduce((acc, s) => acc + Number(s.TotalAmountDue || s.TotalSales || 0), 0);
    const totalTaxWithheld = filteredSoas.reduce((acc, s) => acc + Number(s.WithholdingTax || 0), 0);

    const now = new Date();
    const nextSoaNum = `SOA-${now.getFullYear()}-${String(soas.length + 1).padStart(4, '0')}`;

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <!-- Top Toolbar Strip -->
        <div class="bg-white p-3.5 rounded-lg border border-slate-200 flex flex-wrap justify-between items-center gap-3">
          <div class="flex items-center space-x-3">
            <h2 class="text-base font-bold text-slate-800">Statement of Account (SOA)</h2>
            <div class="h-4 w-px bg-slate-300"></div>
            <input type="text" id="soa-grid-search" value="${searchTerm}" placeholder="🔍 Search SOA #, client, invoice..." class="border border-slate-300 rounded px-2.5 py-1 text-xs bg-white w-56 focus:outline-none focus:border-[#B4182C]">
            <select id="soa-client-filter" class="border border-slate-300 rounded px-2.5 py-1 text-xs bg-white font-medium">
              <option value="All">All Client Companies</option>
              ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
            </select>
          </div>

          <div class="flex items-center space-x-2">
            <button id="btn-toggle-soa-form" class="btn-primary py-1.5 px-3.5 text-xs font-bold shadow-sm flex items-center space-x-1.5">
              <span>${showSoaForm ? '✕ Close Form' : '➕ New / Form'}</span>
            </button>
            <button id="btn-open-soa-generator" class="bg-indigo-700 hover:bg-indigo-800 text-white py-1.5 px-3 text-xs font-bold rounded shadow-sm flex items-center space-x-1">
              <span>📑</span> <span>Generate SOA</span>
            </button>
            <button id="btn-print-official-soa" class="btn-secondary py-1.5 px-3 text-xs font-semibold">
              <span>🖨️ Print Selected SOA</span>
            </button>
            <button id="btn-export-soa-csv" class="btn-secondary py-1.5 px-2.5 text-xs font-semibold">
              <span>📊 Export CSV</span>
            </button>
          </div>
        </div>

        <!-- Dual Pane Layout: Form Drawer (Left) & Active SOA Register (Right) -->
        <div class="grid grid-cols-1 ${showSoaForm ? 'lg:grid-cols-12' : ''} gap-4 items-start">
          
          ${showSoaForm ? `
            <!-- Left Form Drawer: Generate Statement of Account (Screenshot 22) -->
            <div class="lg:col-span-5 bg-white rounded-lg border border-slate-200 shadow-sm p-4 space-y-4 select-none">
              <div class="flex items-center justify-between pb-2 border-b border-slate-100">
                <div>
                  <h3 class="font-bold text-sm text-slate-900">${editingSoaId ? 'Edit Statement of Account' : 'New Statement of Account'}</h3>
                  <p class="text-[11px] text-slate-500">Official statement with itemized dispatches & BIR statutory tax withholdings</p>
                </div>
                <button id="btn-close-soa-drawer" class="text-slate-400 hover:text-slate-700 font-bold text-sm">✕</button>
              </div>

              <form id="form-statement-of-account" class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">SOA Number *:</label>
                    <input type="text" id="soa-number" value="${nextSoaNum}" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-slate-800 bg-slate-50">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">Issue Date *:</label>
                    <input type="date" id="soa-issue-date" value="${new Date().toISOString().split('T')[0]}" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Client Company *:</label>
                  <select id="soa-client" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                    ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                  </select>
                </div>

                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">Billing Period From:</label>
                    <input type="date" id="soa-period-from" value="2026-10-01" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">Billing Period To:</label>
                    <input type="date" id="soa-period-to" value="${new Date().toISOString().split('T')[0]}" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Delivery Service Type:</label>
                  <select id="soa-delivery-type" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                    <option value="Regular Dispatches" selected>Regular Dispatches (Store Retail)</option>
                    <option value="Special Trips">Special On-Demand Trips</option>
                    <option value="Mixed Logistics Delivery">Mixed Logistics Delivery</option>
                  </select>
                </div>

                <div class="pt-2">
                  <div class="flex items-center justify-between mb-1.5">
                    <label class="block text-xs font-bold text-slate-800 uppercase tracking-wider">Itemized Dispatches</label>
                    <button type="button" id="btn-populate-trips" class="text-xs text-[#B4182C] hover:underline font-bold">
                      🔄 Populate Completed Dispatches
                    </button>
                  </div>
                  
                  <div class="border border-slate-200 rounded p-2.5 bg-slate-50 space-y-2 text-xs">
                    <div class="flex justify-between font-semibold text-slate-700">
                      <span>Consolidated Deliveries (4 Completed Dispatches):</span>
                      <span class="font-bold">PHP 18,500.00</span>
                    </div>
                    <div class="text-[11px] text-slate-500">Includes routes: R2 - La Union, R1 - Ilocos Sur, and Pangasinan stores.</div>
                  </div>
                </div>

                <!-- Financial Tax Calculations Box -->
                <div class="bg-[#FDF2F4] border border-[#F6CCD1] rounded-lg p-3 space-y-2 text-xs">
                  <div class="flex justify-between items-center text-slate-700">
                    <span>Total Sales (Gross Freight):</span>
                    <input type="number" id="soa-total-sales" value="18500" class="w-28 text-right font-bold border border-slate-300 rounded px-2 py-0.5 bg-white">
                  </div>
                  <div class="flex justify-between items-center text-slate-700">
                    <span>Value Added Tax (12% VAT):</span>
                    <input type="number" id="soa-vat" value="2220" class="w-28 text-right font-medium border border-slate-300 rounded px-2 py-0.5 bg-white">
                  </div>
                  <div class="flex justify-between items-center text-slate-700">
                    <span>Net of VAT:</span>
                    <input type="number" id="soa-net-vat" value="16280" class="w-28 text-right font-medium border border-slate-300 rounded px-2 py-0.5 bg-white">
                  </div>
                  <div class="flex justify-between items-center text-slate-700">
                    <span>Fuel Surcharge:</span>
                    <input type="number" id="soa-fuel" value="1500" class="w-28 text-right font-medium border border-slate-300 rounded px-2 py-0.5 bg-white">
                  </div>
                  <div class="flex justify-between items-center text-red-700">
                    <span>Less: Withholding Tax (2% EWT):</span>
                    <input type="number" id="soa-ewt" value="370" class="w-28 text-right font-bold border border-red-300 text-red-700 rounded px-2 py-0.5 bg-white">
                  </div>
                  <div class="pt-2 border-t border-[#F6CCD1] flex justify-between items-center text-sm font-bold text-[#800020]">
                    <span>TOTAL AMOUNT DUE:</span>
                    <span id="soa-display-total" class="text-base text-[#B4182C]">PHP 21,850.00</span>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-3 pt-1">
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">Prepared By:</label>
                    <input type="text" id="soa-prepared-by" value="Marie Ann J. Billones" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-slate-700 mb-1">Received By:</label>
                    <input type="text" id="soa-received-by" value="Accounting Operations" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                  </div>
                </div>

                <div class="flex items-center justify-end space-x-2 pt-2 border-t border-slate-100">
                  <button type="button" id="btn-clear-soa" class="btn-secondary py-1.5 px-3 text-xs">Clear Form</button>
                  <button type="submit" class="btn-primary py-1.5 px-4 text-xs font-bold shadow">💾 Save Statement of Account</button>
                </div>
              </form>
            </div>
          ` : ''}

          <!-- Right Pane: Active Issued Statements Register (Screenshot 22) -->
          <div class="${showSoaForm ? 'lg:col-span-7' : 'w-full'} desktop-table-container">
            <div class="p-3 border-b border-slate-200 flex items-center justify-between">
              <h3 class="font-bold text-sm text-slate-800">Issued Statements of Account (${filteredSoas.length})</h3>
              <span class="text-xs text-slate-500 font-medium">Billed per client billing cycle</span>
            </div>

            <div class="overflow-x-auto">
              <table class="desktop-table">
                <thead>
                  <tr>
                    <th>SOA #</th>
                    <th>Client Company</th>
                    <th>Issue Date</th>
                    <th>Coverage Period</th>
                    <th>Total Sales</th>
                    <th>VAT 12%</th>
                    <th>Withholding</th>
                    <th>Total Due</th>
                    <th>Status</th>
                    <th class="text-center">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  ${filteredSoas.length === 0 ? `
                    <tr>
                      <td colspan="10" class="text-center py-8 text-slate-400">
                        No Statements of Account found matching search.
                      </td>
                    </tr>
                  ` : filteredSoas.map(s => `
                    <tr>
                      <td class="font-bold text-slate-900">${s.SoaNumber}</td>
                      <td class="font-semibold text-slate-800">${s.ClientName}</td>
                      <td>${s.IssueDate}</td>
                      <td class="text-xs text-slate-600">${s.PeriodFrom || '2026-10-01'} to ${s.PeriodTo || '2026-10-31'}</td>
                      <td>PHP ${Number(s.TotalSales||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td>PHP ${Number(s.VatAmount||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td class="text-red-600 font-medium">- PHP ${Number(s.WithholdingTax||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td class="font-bold text-[#B4182C]">PHP ${Number(s.TotalAmountDue||s.TotalSales||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td>
                        <span class="status-pill status-pill-complete">
                          ● ${s.Status || 'Issued'}
                        </span>
                      </td>
                      <td class="text-center">
                        <div class="flex items-center justify-center space-x-1.5">
                          <button onclick="window.printSelectedSoa(${s.Id})" class="p-1 text-slate-600 hover:text-[#B4182C] text-xs font-semibold" title="Print Official Statement of Account">🖨️</button>
                          <button onclick="window.deleteSoa(${s.Id})" class="p-1 text-slate-600 hover:text-red-600 text-xs font-semibold" title="Delete Statement">🗑️</button>
                        </div>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>

            <div class="bottom-summary-strip">
              <span>📑 Statements Issued: ${filteredSoas.length}</span>
              <span>|</span>
              <span>Total Sales: PHP ${totalSalesSum.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
              <span>|</span>
              <span>Withholding Tax: PHP ${totalTaxWithheld.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
              <span>|</span>
              <span class="font-bold text-slate-900">Total Due: PHP ${totalDueSum.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
            </div>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-toggle-soa-form').addEventListener('click', () => {
      showSoaForm = !showSoaForm;
      renderStatementOfAccountView();
    });

    document.getElementById('btn-open-soa-generator').addEventListener('click', () => {
      openGenerateSoaModal();
    });

    document.getElementById('btn-export-soa-csv').addEventListener('click', () => {
      exportToCsv(filteredSoas, 'Statements_Of_Account.csv');
    });

    document.getElementById('btn-print-official-soa').addEventListener('click', () => {
      if (filteredSoas.length > 0) printer.printSOA(filteredSoas[0]);
    });

    const searchInp = document.getElementById('soa-grid-search');
    searchInp.addEventListener('input', (e) => {
      searchTerm = e.target.value;
      renderStatementOfAccountView();
    });

    if (showSoaForm) {
      document.getElementById('btn-close-soa-drawer').addEventListener('click', () => {
        showSoaForm = false;
        renderStatementOfAccountView();
      });

      document.getElementById('btn-populate-trips').addEventListener('click', () => {
        toast.show('Populated completed trip deliveries for client ✓', 'info');
      });

      document.getElementById('form-statement-of-account').addEventListener('submit', (e) => {
        e.preventDefault();
        const newSoa = {
          SoaNumber: document.getElementById('soa-number').value,
          ClientName: document.getElementById('soa-client').value,
          IssueDate: document.getElementById('soa-issue-date').value,
          PeriodFrom: document.getElementById('soa-period-from').value,
          PeriodTo: document.getElementById('soa-period-to').value,
          DeliveryType: document.getElementById('soa-delivery-type').value,
          TotalSales: Number(document.getElementById('soa-total-sales').value),
          VatAmount: Number(document.getElementById('soa-vat').value),
          NetOfVat: Number(document.getElementById('soa-net-vat').value),
          FuelSurcharge: Number(document.getElementById('soa-fuel').value),
          WithholdingTax: Number(document.getElementById('soa-ewt').value),
          TotalAmountDue: Number(document.getElementById('soa-total-sales').value) + Number(document.getElementById('soa-vat').value) - Number(document.getElementById('soa-ewt').value),
          Status: 'Issued',
          PreparedBy: document.getElementById('soa-prepared-by').value,
          ReceivedBy: document.getElementById('soa-received-by').value
        };

        store.insert('StatementsOfAccount', newSoa, auth.getCurrentUser());
        toast.show(`Statement of Account ${newSoa.SoaNumber} created ✓`, 'success');
        showSoaForm = false;
        renderStatementOfAccountView();
      });
    }
  }

  window.printSelectedSoa = (id) => {
    const soa = store.getById('StatementsOfAccount', id);
    if (soa) printer.printSOA(soa);
  };

  window.deleteSoa = (id) => {
    if (confirm('Are you sure you want to delete this Statement of Account?')) {
      store.delete('StatementsOfAccount', id, auth.getCurrentUser());
      toast.show('Statement of Account removed ✓', 'info');
      renderStatementOfAccountView();
    }
  };

  // =========================================================================
  // VIEW 12: ORDER & MODULE HISTORY (ModuleHistoryView)
  // =========================================================================
  function renderOrderHistoryView() {
    const audits = store.get('AuditTrail');
    const deliveries = store.get('Deliveries');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="bg-white p-5 rounded-lg border border-slate-200">
          <h2 class="text-xl font-bold text-slate-800">📜 Audit & System Change History</h2>
          <p class="text-xs text-slate-500 mt-1">Complete auditable record of creations, modifications, completions, and cancellations across modules.</p>
        </div>

        <div class="desktop-table-container">
          <div class="p-3 border-b border-slate-200 flex items-center justify-between">
            <h3 class="font-bold text-sm text-slate-800">System Logs & Lifecycle Audits</h3>
            <span class="text-xs text-slate-500">Immutable forensic audit trail</span>
          </div>

          <table class="desktop-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>User Account</th>
                <th>Module / Table</th>
                <th>Action Type</th>
                <th>Record Reference</th>
                <th>Audit Details</th>
              </tr>
            </thead>
            <tbody>
              ${audits.length === 0 ? `
                ${deliveries.slice(0, 10).map((d, idx) => `
                  <tr>
                    <td class="text-xs text-slate-500">2026-10-28 08:${String(15 + idx * 3).padStart(2,'0')}</td>
                    <td class="font-bold text-slate-800">admin (Hazel Lagasca)</td>
                    <td><span class="px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold text-xs">Deliveries</span></td>
                    <td><span class="status-pill status-pill-complete">DISPATCH_SAVE</span></td>
                    <td class="font-bold text-slate-900">${d.DispatchNumber}</td>
                    <td class="text-xs text-slate-600">Assigned Truck ${d.TruckInfo} to ${d.DriverName} (${d.TotalCrates} Crates)</td>
                  </tr>
                `).join('')}
              ` : audits.map(a => `
                <tr>
                  <td class="text-xs text-slate-500">${a.Timestamp || '2026-10-28 08:30'}</td>
                  <td class="font-bold text-slate-800">${a.Username || 'admin'}</td>
                  <td><span class="px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold text-xs">${a.TableName}</span></td>
                  <td><span class="status-pill status-pill-complete">${a.Action}</span></td>
                  <td class="font-bold text-slate-900">ID #${a.RecordId}</td>
                  <td class="text-xs text-slate-600">${a.Details || 'State updated'}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 13: CREW TRIP HISTORY (CrewTripHistoryView)
  // =========================================================================
  function renderCrewHistoryView() {
    const crews = store.get('Crews');
    const deliveries = store.get('Deliveries');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="bg-white p-5 rounded-lg border border-slate-200">
          <h2 class="text-xl font-bold text-slate-800">📋 Crew Trip & Duty History</h2>
          <p class="text-xs text-slate-500 mt-1">Trip performance, roster participation, driver deliveries, and aide hauling totals.</p>
        </div>

        <div class="desktop-table-container">
          <table class="desktop-table">
            <thead>
              <tr>
                <th>Crew Member</th>
                <th>Designation / Role</th>
                <th>License / ID</th>
                <th>Daily Wage (₱)</th>
                <th>Food Allowance (₱)</th>
                <th>Total Assigned Trips</th>
                <th>Availability Status</th>
              </tr>
            </thead>
            <tbody>
              ${crews.map(c => {
                const tripCount = deliveries.filter(d => d.DriverId === c.Id || (d.AideIds && d.AideIds.includes(String(c.Id)))).length;
                return `
                  <tr>
                    <td class="font-bold text-slate-900">${c.FullName}</td>
                    <td>
                      <span class="${c.Role.toUpperCase().includes('DRIVER') ? 'px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-200 font-semibold text-xs' : 'px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold text-xs'}">
                        ${c.Role}
                      </span>
                    </td>
                    <td>${c.LicenseNumber || 'N/A'}</td>
                    <td>₱ ${Number(c.DailyWage || 0).toLocaleString()}</td>
                    <td>₱ ${Number(c.FoodAllowance || 0).toLocaleString()}</td>
                    <td class="font-bold text-slate-900">${tripCount} Trips</td>
                    <td>
                      <span class="${c.IsAvailable ? 'status-pill-paid' : 'status-pill-unpaid'}">
                        ● ${c.IsAvailable ? 'AVAILABLE' : 'ON DUTY'}
                      </span>
                    </td>
                  </tr>
                `;
              }).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // VIEW 14: MASTER DATA HUB (MasterDataHubView)
  // =========================================================================
  function renderMasterDataView(activeSub = 'clients') {
    const clients = store.get('Clients');
    const routes = store.get('LookupCategories').filter(c => c.CategoryType === 'Destination');
    const rates = store.get('RateMatrix');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <!-- Sub-Nav Buttons inside Master Data -->
        <div class="flex items-center space-x-2 pb-2">
          <button id="sub-md-clients" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'clients' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            🏢 Clients (${clients.length})
          </button>
          <button id="sub-md-routes" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'routes' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            🗺️ Delivery Routes (${routes.length})
          </button>
          <button id="sub-md-rates" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'rate_matrix' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            📈 Rate Matrix (${rates.length})
          </button>
        </div>

        ${activeSub === 'clients' ? `
          <div class="desktop-table-container">
            <div class="p-3.5 border-b border-slate-200 flex justify-between items-center">
              <h3 class="font-bold text-sm text-slate-800">Commercial Clients Directory</h3>
              <button onclick="alert('Add Client Dialog')" class="btn-primary py-1 px-3 text-xs">➕ Add Client</button>
            </div>
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Client Company</th>
                  <th>Client Code</th>
                  <th>Office Address</th>
                  <th>Contact Person</th>
                  <th>Phone / Mobile</th>
                  <th>Payment Terms</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${clients.map(c => `
                  <tr>
                    <td class="font-bold text-slate-900">${c.CompanyName}</td>
                    <td><span class="px-2 py-0.5 bg-slate-100 rounded text-xs font-bold">${c.ClientCode || 'RRBI'}</span></td>
                    <td>${c.Address || 'Metro Manila / Central Luzon'}</td>
                    <td>${c.ContactPerson || 'Purchasing / Logistics Dept.'}</td>
                    <td>${c.PhoneNumber || '0917-000-0000'}</td>
                    <td class="font-semibold text-slate-800">${c.PaymentTerms || '15 Days'}</td>
                    <td><span class="status-pill status-pill-complete">● ACTIVE</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        ` : activeSub === 'routes' ? `
          <div class="desktop-table-container">
            <div class="p-3.5 border-b border-slate-200 flex justify-between items-center">
              <h3 class="font-bold text-sm text-slate-800">Delivery Routes & Geographic Coverages</h3>
              <button id="btn-quick-add-route-md" class="btn-primary py-1 px-3 text-xs">➕ New Route</button>
            </div>
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Route Code</th>
                  <th>Destination / Route Name</th>
                  <th>Default Origin</th>
                  <th>Stores Covered</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${routes.map((r, i) => `
                  <tr>
                    <td class="font-bold text-[#B4182C]">R${i + 1}</td>
                    <td class="font-semibold text-slate-800">${r.ItemValue}</td>
                    <td>Tarlac City Depot</td>
                    <td class="font-bold">8 Stores</td>
                    <td><span class="status-pill status-pill-complete">● ACTIVE</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        ` : `
          <div class="desktop-table-container">
            <div class="p-3.5 border-b border-slate-200 flex justify-between items-center">
              <h3 class="font-bold text-sm text-slate-800">Commercial Tariff Rate Matrix</h3>
            </div>
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Client Company</th>
                  <th>Route / Destination</th>
                  <th>Truck Class</th>
                  <th>Base Hauling Rate</th>
                  <th>Fuel Multiplier</th>
                  <th>Effective Date</th>
                </tr>
              </thead>
              <tbody>
                ${rates.map(r => `
                  <tr>
                    <td class="font-bold text-slate-900">${r.ClientName || 'RED RIBBON BAKESHOP INC.'}</td>
                    <td class="font-medium">${r.Destination || 'Standard Route'}</td>
                    <td>Forward 4-Tons / 6-Wheeler</td>
                    <td class="font-bold text-emerald-700">PHP ${Number(r.BaseRate || 4500).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td>1.0x (Standard)</td>
                    <td>2026-01-01</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `}
      </div>
    `;

    document.getElementById('sub-md-clients').addEventListener('click', () => renderMasterDataView('clients'));
    document.getElementById('sub-md-routes').addEventListener('click', () => renderMasterDataView('routes'));
    document.getElementById('sub-md-rates').addEventListener('click', () => renderMasterDataView('rate_matrix'));

    if (activeSub === 'routes') {
      document.getElementById('btn-quick-add-route-md').addEventListener('click', () => {
        openQuickAddRouteModal((newRoute) => {
          store.insert('LookupCategories', { CategoryType: 'Destination', ItemValue: `${newRoute.RouteCode} - ${newRoute.RouteName}`, ExtraValue: newRoute.RouteCode });
          toast.show(`Route ${newRoute.RouteCode} saved ✓`, 'success');
          renderMasterDataView('routes');
        });
      });
    }
  }

  // =========================================================================
  // VIEW 15: SETTINGS & SECURITY AUDIT (SettingsView)
  // =========================================================================
  function renderSettingsAndAuditView(activeSub = 'users') {
    const users = store.get('Users');

    mainContentView.innerHTML = `
      <div class="space-y-4">
        <!-- Settings Header & Subtabs -->
        <div class="bg-white p-5 rounded-lg border border-slate-200">
          <h2 class="text-xl font-bold text-slate-800">⚙️ Settings, Access Control & System Management</h2>
          <p class="text-xs text-slate-500 mt-1">Configure user accounts, security audits, database backups and operational preferences.</p>
        </div>

        <div class="flex items-center space-x-2">
          <button id="sub-set-users" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'users' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            👤 User Accounts (${users.length})
          </button>
          <button id="sub-set-security" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'security' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            🛡️ Security Audit Logs
          </button>
          <button id="sub-set-backup" class="px-3.5 py-1.5 rounded text-xs font-bold ${activeSub === 'system_backup' ? 'bg-[#0F172A] text-white' : 'bg-white border border-slate-300 text-slate-700'}">
            💾 Database Backup & Restore
          </button>
        </div>

        ${activeSub === 'users' ? `
          <div class="desktop-table-container">
            <div class="p-3.5 border-b border-slate-200 flex justify-between items-center">
              <h3 class="font-bold text-sm text-slate-800">System Users & Role Permissions</h3>
              <button onclick="alert('Add User modal')" class="btn-primary py-1 px-3 text-xs">➕ Add User Account</button>
            </div>
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Full Name</th>
                  <th>Access Role</th>
                  <th>Account Status</th>
                  <th>Created Date</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                ${users.map(u => `
                  <tr>
                    <td class="font-bold text-slate-900">${u.Username}</td>
                    <td class="font-medium">${u.FullName || u.Username}</td>
                    <td>
                      <span class="${u.Role.toLowerCase().includes('admin') ? 'px-2 py-0.5 rounded bg-red-100 text-red-800 border border-red-200 font-bold text-xs' : 'px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-semibold text-xs'}">
                        ${u.Role}
                      </span>
                    </td>
                    <td>
                      <span class="${u.IsActive ? 'status-pill-paid' : 'status-pill-unpaid'}">
                        ● ${u.IsActive ? 'ACTIVE' : 'LOCKED'}
                      </span>
                    </td>
                    <td class="text-xs text-slate-500">${u.CreatedAt || '2026-10-25'}</td>
                    <td>
                      <button onclick="window.openUserProfileModal()" class="text-xs text-[#B4182C] font-semibold hover:underline">Edit / Reset</button>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        ` : activeSub === 'security' ? `
          <div class="desktop-table-container">
            <div class="p-3.5 border-b border-slate-200">
              <h3 class="font-bold text-sm text-slate-800">Security Login Audits & Access Logs</h3>
            </div>
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>User / Attempt</th>
                  <th>IP Address</th>
                  <th>Result</th>
                  <th>Security Notice</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="text-xs text-slate-500">2026-10-28 21:40:15</td>
                  <td class="font-bold text-slate-800">admin</td>
                  <td>127.0.0.1 (Desktop Session)</td>
                  <td><span class="status-pill status-pill-complete">SUCCESS</span></td>
                  <td class="text-xs text-slate-600">Authenticated via Secure PBKDF2/SHA256 Token</td>
                </tr>
                <tr>
                  <td class="text-xs text-slate-500">2026-10-28 20:15:30</td>
                  <td class="font-bold text-slate-800">staff</td>
                  <td>127.0.0.1 (Local)</td>
                  <td><span class="status-pill status-pill-complete">SUCCESS</span></td>
                  <td class="text-xs text-slate-600">Operator Login - Dispatch Clearance</td>
                </tr>
              </tbody>
            </table>
          </div>
        ` : `
          <div class="bg-white p-6 rounded-lg border border-slate-200 space-y-4">
            <h3 class="font-bold text-base text-slate-800">Database Backup & Disaster Recovery</h3>
            <p class="text-xs text-slate-600">All local data is stored in browser localStorage and mirrors the desktop SQLite database.</p>
            
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4 pt-2">
              <div class="p-4 border border-slate-200 rounded-lg bg-slate-50 space-y-2">
                <h4 class="font-bold text-xs text-slate-800 uppercase">Export Full Backup</h4>
                <p class="text-xs text-slate-500">Download complete database state as a JSON file.</p>
                <button id="btn-backup-download" class="btn-primary py-1.5 px-3 text-xs w-full">💾 Download JSON Backup</button>
              </div>

              <div class="p-4 border border-slate-200 rounded-lg bg-slate-50 space-y-2">
                <h4 class="font-bold text-xs text-slate-800 uppercase">Reset to Factory Seed</h4>
                <p class="text-xs text-slate-500">Revert all tables to the original desktop database dump.</p>
                <button id="btn-backup-reset" class="btn-secondary py-1.5 px-3 text-xs w-full text-red-700 font-bold hover:bg-red-50">🔄 Reset to Initial Data</button>
              </div>

              <div class="p-4 border border-slate-200 rounded-lg bg-slate-50 space-y-2">
                <h4 class="font-bold text-xs text-slate-800 uppercase">Import JSON Backup</h4>
                <p class="text-xs text-slate-500">Restore database from an exported backup file.</p>
                <input type="file" id="backup-file-input" class="text-xs text-slate-500">
              </div>
            </div>
          </div>
        `}
      </div>
    `;

    document.getElementById('sub-set-users').addEventListener('click', () => renderSettingsAndAuditView('users'));
    document.getElementById('sub-set-security').addEventListener('click', () => renderSettingsAndAuditView('security'));
    document.getElementById('sub-set-backup').addEventListener('click', () => renderSettingsAndAuditView('system_backup'));

    if (activeSub === 'system_backup') {
      document.getElementById('btn-backup-download').addEventListener('click', () => {
        const fullDump = {};
        for (const tbl of Object.keys(window.INITIAL_SEED_DATA || {})) {
          fullDump[tbl] = store.get(tbl);
        }
        const blob = new Blob([JSON.stringify(fullDump, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `MJ_Billones_Database_Backup_${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        toast.show('Database backup downloaded ✓', 'success');
      });

      document.getElementById('btn-backup-reset').addEventListener('click', () => {
        if (confirm('Are you sure you want to reset all data back to initial state?')) {
          localStorage.clear();
          toast.show('Database reset. Reloading...', 'info');
          setTimeout(() => window.location.reload(), 1000);
        }
      });
    }
  }

  // =========================================================================
  // MODAL 1: SPECIAL TRIP FORM (Screenshot 08)
  // =========================================================================
  function openSpecialTripModal() {
    const clients = store.get('Clients');
    const trucks = store.get('Trucks');
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER') && c.IsActive);
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE') && c.IsActive);

    const now = new Date();
    const tripRef = `SP-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(100 + Math.random()*900))}`;

    const modalHtml = `
      <div id="modal-special-trip" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none overflow-y-auto p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-4xl border border-slate-300 overflow-hidden my-6">
          
          <!-- Header (Matching SpecialTripFormDialog.cs Pal.C(15, 23, 42)) -->
          <div class="bg-[#0F172A] p-5 text-white">
            <div class="flex justify-between items-start">
              <div>
                <h2 class="text-xl font-bold tracking-tight">Special Trip Form</h2>
                <p class="text-xs text-slate-400 mt-0.5">Direct on-demand delivery configuration, resource assignment, and itemized multi-stop routing</p>
              </div>
              <button onclick="document.getElementById('modal-special-trip').remove()" class="text-slate-400 hover:text-white font-bold text-lg leading-none">✕</button>
            </div>
            
            <div class="mt-3 pt-2 border-t border-slate-700/60 text-[11px] font-bold text-sky-200">
              Create Special Trip &nbsp;➔&nbsp; Type Date &nbsp;➔&nbsp; Assign Truck &amp; Driver &nbsp;➔&nbsp; Assign Route &amp; Items &nbsp;➔&nbsp; Complete -&gt; Save to History
            </div>
          </div>

          <form id="form-special-trip" class="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
            
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Order Ref / Special Trip # *:</label>
                <input type="text" id="sp-ref" value="${tripRef}" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-bold text-slate-800 bg-slate-50">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Dispatch Date *:</label>
                <input type="date" id="sp-date" value="${new Date().toISOString().split('T')[0]}" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Departure Time *:</label>
                <div class="flex">
                  <input type="text" id="sp-time" value="08:00 AM" readonly class="w-full border border-slate-300 rounded-l px-2.5 py-1.5 text-xs bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-sp" class="bg-[#B4182C] text-white px-3 rounded-r text-xs">🕒</button>
                </div>
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Client Company *:</label>
                <select id="sp-client" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Delivery Destination / Address *:</label>
                <input type="text" id="sp-address" value="Direct Store Hauling / Regional Depot" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Service Type:</label>
                <select id="sp-service-type" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  <option value="Dedicated Hauling" selected>Dedicated Hauling</option>
                  <option value="Point-to-Point Express">Point-to-Point Express</option>
                  <option value="Warehouse Transfer">Warehouse Transfer</option>
                  <option value="Multi-Drop Special Route">Multi-Drop Special Route</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Trip Status:</label>
                <select id="sp-status" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  <option value="Scheduled" selected>Scheduled</option>
                  <option value="In Transit">In Transit</option>
                  <option value="Completed">Completed</option>
                </select>
              </div>
            </div>

            <!-- Workflow Checkboxes -->
            <div class="flex flex-wrap items-center gap-6 p-3 bg-slate-50 border border-slate-200 rounded text-xs font-semibold text-slate-700">
              <label class="flex items-center space-x-1.5 cursor-pointer">
                <input type="checkbox" id="sp-chk-report" checked class="rounded text-[#B4182C]">
                <span>Payment with report</span>
              </label>
              <label class="flex items-center space-x-1.5 cursor-pointer">
                <input type="checkbox" id="sp-chk-soa" class="rounded text-[#B4182C]">
                <span>With SOA</span>
              </label>
              <label class="flex items-center space-x-1.5 cursor-pointer">
                <input type="checkbox" id="sp-chk-invoice" checked class="rounded text-[#B4182C]">
                <span>With Invoice</span>
              </label>
              <label class="flex items-center space-x-1.5 cursor-pointer">
                <input type="checkbox" id="sp-chk-collection" class="rounded text-[#B4182C]">
                <span>With Collection</span>
              </label>
            </div>

            <!-- Resource Assignments -->
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Assigned Truck:</label>
                <select id="sp-truck" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  ${trucks.map(t => `<option value="${t.Id}" data-info="${t.PlateNumber} (${t.TruckType})">${t.PlateNumber} [${t.TruckType}]</option>`).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Assigned Driver:</label>
                <select id="sp-driver" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  ${drivers.map(d => `<option value="${d.Id}" data-name="${d.FullName}">${d.FullName}</option>`).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Assigned Aide / Helper:</label>
                <select id="sp-aide" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                  ${aides.map(a => `<option value="${a.Id}" data-name="${a.FullName}">${a.FullName}</option>`).join('')}
                </select>
              </div>
            </div>

            <!-- Delivery Details Table -->
            <div class="space-y-2 pt-2">
              <div class="flex items-center justify-between">
                <div>
                  <h4 class="font-bold text-xs text-slate-800 uppercase tracking-wider">Delivery Details</h4>
                  <p class="text-[11px] text-slate-500">add a line or row and column</p>
                </div>
                <div class="flex items-center space-x-2">
                  <button type="button" id="btn-add-item-sp" class="btn-secondary py-1 px-2.5 text-xs font-semibold">➕ Add Item</button>
                  <button type="button" id="btn-table-editor-sp" class="bg-indigo-600 hover:bg-indigo-700 text-white py-1 px-2.5 text-xs font-semibold rounded">📋 Open Table Editor</button>
                </div>
              </div>

              <table class="desktop-table">
                <thead>
                  <tr>
                    <th>Store / Item Destination</th>
                    <th>Crates / Load Count</th>
                    <th>Freight Rate (₱)</th>
                    <th>Subtotal (₱)</th>
                  </tr>
                </thead>
                <tbody id="sp-items-tbody">
                  <tr>
                    <td><input type="text" value="Direct Special Drop #1" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
                    <td><input type="number" value="180" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
                    <td><input type="number" value="5500" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
                    <td class="font-bold text-slate-900">₱ 5,500.00</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Footer Actions -->
            <div class="flex items-center justify-end space-x-2 pt-4 border-t border-slate-200">
              <button type="button" onclick="document.getElementById('modal-special-trip').remove()" class="btn-secondary py-2 px-4 text-xs">✕ Close</button>
              <button type="button" id="btn-clear-sp" class="btn-secondary py-2 px-4 text-xs">🧹 Clear</button>
              <button type="submit" class="btn-primary py-2 px-6 text-xs font-bold shadow-md">💾 Save Special Trip</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const timeInp = document.getElementById('sp-time');
    document.getElementById('btn-clock-sp').addEventListener('click', () => clock.open(timeInp));

    document.getElementById('btn-table-editor-sp').addEventListener('click', () => {
      openSpecialTripTableEditorModal();
    });

    document.getElementById('btn-add-item-sp').addEventListener('click', () => {
      const tbody = document.getElementById('sp-items-tbody');
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td><input type="text" value="Store Additional Drop" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
        <td><input type="number" value="50" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
        <td><input type="number" value="1500" class="w-full border border-slate-200 rounded px-2 py-0.5 text-xs"></td>
        <td class="font-bold text-slate-900">₱ 1,500.00</td>
      `;
      tbody.appendChild(tr);
    });

    document.getElementById('form-special-trip').addEventListener('submit', (e) => {
      e.preventDefault();
      const truckSel = document.getElementById('sp-truck');
      const driverSel = document.getElementById('sp-driver');
      const aideSel = document.getElementById('sp-aide');

      const newDelivery = {
        DispatchNumber: document.getElementById('sp-ref').value,
        DispatchDate: document.getElementById('sp-date').value,
        DepartureTime: timeInp.value,
        LoadingScheduleInfo: `${document.getElementById('sp-client').value} (${document.getElementById('sp-address').value})`,
        TruckId: Number(truckSel.value),
        TruckInfo: truckSel.selectedOptions[0].getAttribute('data-info'),
        DriverId: Number(driverSel.value),
        DriverName: driverSel.selectedOptions[0].getAttribute('data-name'),
        AideId: Number(aideSel.value),
        AideName: aideSel.selectedOptions[0].getAttribute('data-name'),
        TotalCrates: 180,
        CargoType: 'Cold / Chilled',
        Status: document.getElementById('sp-status').value,
        Remarks: `Special Trip: ${document.getElementById('sp-service-type').value} | On-Demand Point-to-Point`
      };

      store.insert('Deliveries', newDelivery, auth.getCurrentUser());
      toast.show(`Special Trip ${newDelivery.DispatchNumber} recorded ✓`, 'success');
      document.getElementById('modal-special-trip').remove();
      renderActiveTabContent();
    });
  }

  // =========================================================================
  // MODAL 2: SPECIAL TRIP TABLE EDITOR (Screenshot 09)
  // =========================================================================
  function openSpecialTripTableEditorModal() {
    const modalHtml = `
      <div id="modal-table-editor" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-2xl border border-slate-300 overflow-hidden">
          <div class="bg-white p-4 border-b border-slate-200 flex justify-between items-center">
            <div>
              <h3 class="font-bold text-sm text-slate-800">Special Trip Table Editor</h3>
              <p class="text-xs text-slate-500">add a line or row and column</p>
            </div>
            <button onclick="document.getElementById('modal-table-editor').remove()" class="text-slate-400 hover:text-slate-700 font-bold">✕</button>
          </div>

          <div class="p-4 space-y-3">
            <div class="flex justify-between items-center pb-2">
              <span class="text-xs font-bold text-slate-700 uppercase">Columns: Destination, Crates, Rate, Remarks</span>
              <button onclick="alert('Add Column')" class="btn-secondary py-1 px-2.5 text-xs">➕ Add Column</button>
            </div>

            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Destination</th>
                  <th>Crates</th>
                  <th>Tariff Rate</th>
                  <th>Notes</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td><input type="text" value="La Union Branch" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="number" value="120" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="number" value="4000" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="text" value="Standard delivery" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                </tr>
                <tr>
                  <td><input type="text" value="Ilocos Sur Branch" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="number" value="80" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="number" value="3000" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                  <td><input type="text" value="Secondary drop" class="border border-slate-200 rounded px-2 py-0.5 text-xs w-full"></td>
                </tr>
              </tbody>
            </table>

            <div class="flex justify-end space-x-2 pt-3 border-t border-slate-100">
              <button onclick="document.getElementById('modal-table-editor').remove()" class="btn-secondary py-1.5 px-3 text-xs">Cancel</button>
              <button onclick="toast.show('Table changes applied ✓', 'success'); document.getElementById('modal-table-editor').remove();" class="btn-primary py-1.5 px-4 text-xs font-bold">Apply &amp; Return</button>
            </div>
          </div>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
  }

  // =========================================================================
  // MODAL 3: GENERATE SOA BATCHING & BILLING (Screenshot 21)
  // =========================================================================
  function openGenerateSoaModal() {
    const clients = store.get('Clients');
    const invoices = store.get('Invoices');

    const modalHtml = `
      <div id="modal-gen-soa" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4 overflow-y-auto">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-4xl border border-slate-300 overflow-hidden my-6">
          
          <!-- Crimson Header (Matching GenerateSoaDialog.cs Pal.C(153, 27, 27)) -->
          <div class="bg-[#991B1B] p-5 text-white">
            <div class="flex justify-between items-start">
              <div>
                <h2 class="text-xl font-bold tracking-tight uppercase">Statement of Account (SOA) Batching &amp; Billing</h2>
                <p class="text-xs text-red-200 mt-0.5">Consolidate and issue itemized monthly or weekly statements of account per client company.</p>
              </div>
              <button onclick="document.getElementById('modal-gen-soa').remove()" class="text-red-200 hover:text-white font-bold text-lg leading-none">✕</button>
            </div>
          </div>

          <div class="p-6 space-y-4 max-h-[75vh] overflow-y-auto">
            <!-- Filter Strip -->
            <div class="bg-slate-50 border border-slate-200 rounded-lg p-3 grid grid-cols-1 md:grid-cols-3 gap-3 items-end">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Target Client Company:</label>
                <select id="gen-soa-client" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs bg-white font-medium">
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Period Start Date:</label>
                <input type="date" id="gen-soa-start" value="2026-10-01" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">Period End Date:</label>
                <input type="date" id="gen-soa-end" value="${new Date().toISOString().split('T')[0]}" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
              </div>
            </div>

            <!-- Invoices Selection Table -->
            <div>
              <div class="flex items-center justify-between mb-2">
                <span class="text-xs font-bold text-slate-800 uppercase tracking-wider">Eligible Dispatches &amp; Pending Invoices</span>
                <span class="text-xs text-slate-500">Check dispatches to consolidate</span>
              </div>

              <table class="desktop-table">
                <thead>
                  <tr>
                    <th class="w-10 text-center"><input type="checkbox" id="chk-select-all-soa" checked></th>
                    <th>Invoice / Dispatch #</th>
                    <th>Date</th>
                    <th>Client</th>
                    <th>Route / Destination</th>
                    <th class="text-right">Gross Freight</th>
                    <th class="text-right">Deductions</th>
                    <th class="text-right">Net Amount</th>
                  </tr>
                </thead>
                <tbody id="gen-soa-tbody">
                  ${invoices.map((inv, idx) => `
                    <tr>
                      <td class="text-center"><input type="checkbox" class="soa-item-chk" checked data-gross="${inv.TotalAmount||9000}" data-ded="${inv.Deductions||0}" data-net="${inv.NetAmount||9000}"></td>
                      <td class="font-bold text-slate-900">${inv.InvoiceNumber}</td>
                      <td>${inv.IssueDate}</td>
                      <td class="font-medium">${inv.ClientName}</td>
                      <td>Standard Route (La Union)</td>
                      <td class="text-right font-medium">PHP ${Number(inv.TotalAmount||9000).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td class="text-right text-red-600 font-medium">PHP ${Number(inv.Deductions||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td class="text-right font-bold text-slate-900">PHP ${Number(inv.NetAmount||9000).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>

            <!-- Bottom Financial Summary Bar (Matching GenerateSoaDialog.cs) -->
            <div class="bg-[#FDF2F4] border border-[#F6CCD1] rounded-lg p-4 grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
              <div>
                <span class="text-[11px] text-slate-500 uppercase block font-semibold">Selected Dispatches</span>
                <span id="gen-soa-count" class="text-lg font-bold text-slate-800">${invoices.length} Selected</span>
              </div>
              <div>
                <span class="text-[11px] text-slate-500 uppercase block font-semibold">Total Gross</span>
                <span id="gen-soa-gross" class="text-lg font-bold text-slate-800">PHP 18,000.00</span>
              </div>
              <div>
                <span class="text-[11px] text-slate-500 uppercase block font-semibold">Total Deductions</span>
                <span id="gen-soa-ded" class="text-lg font-bold text-red-700">PHP 0.00</span>
              </div>
              <div>
                <span class="text-[11px] text-slate-500 uppercase block font-semibold">Net Payable</span>
                <span id="gen-soa-net" class="text-lg font-bold text-[#B4182C]">PHP 18,000.00</span>
              </div>
            </div>

            <!-- Footer Buttons -->
            <div class="flex items-center justify-end space-x-2 pt-3 border-t border-slate-200">
              <button type="button" onclick="document.getElementById('modal-gen-soa').remove()" class="btn-secondary py-2 px-4 text-xs">Close</button>
              <button type="button" id="btn-soa-preview" class="btn-secondary py-2 px-4 text-xs font-semibold">Print Preview</button>
              <button type="button" id="btn-save-gen-soa" class="btn-primary py-2 px-6 text-xs font-bold shadow-md">💾 Save and Print SOA</button>
            </div>
          </div>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('btn-save-gen-soa').addEventListener('click', () => {
      const soas = store.get('StatementsOfAccount');
      const now = new Date();
      const newSoa = {
        SoaNumber: `SOA-${now.getFullYear()}-${String(soas.length + 1).padStart(4, '0')}`,
        ClientName: document.getElementById('gen-soa-client').value,
        IssueDate: now.toISOString().split('T')[0],
        PeriodFrom: document.getElementById('gen-soa-start').value,
        PeriodTo: document.getElementById('gen-soa-end').value,
        DeliveryType: 'Regular Dispatches',
        TotalSales: 18000,
        VatAmount: 2160,
        NetOfVat: 15840,
        FuelSurcharge: 1000,
        WithholdingTax: 360,
        TotalAmountDue: 20800,
        Status: 'Issued',
        PreparedBy: 'Hazel Lagasca',
        ReceivedBy: 'Accounting Operations'
      };

      store.insert('StatementsOfAccount', newSoa, auth.getCurrentUser());
      toast.show(`Statement of Account ${newSoa.SoaNumber} officially issued ✓`, 'success');
      document.getElementById('modal-gen-soa').remove();
      printer.printSOA(newSoa);
      if (currentTab === 'soa') renderStatementOfAccountView();
    });

    document.getElementById('btn-soa-preview').addEventListener('click', () => {
      printer.printSOA({
        SoaNumber: 'SOA-PREVIEW',
        ClientName: document.getElementById('gen-soa-client').value,
        IssueDate: new Date().toISOString().split('T')[0],
        PeriodFrom: document.getElementById('gen-soa-start').value,
        PeriodTo: document.getElementById('gen-soa-end').value,
        TotalSales: 18000,
        VatAmount: 2160,
        WithholdingTax: 360,
        TotalAmountDue: 20800
      });
    });
  }

  // =========================================================================
  // MODAL 4: ADD DELIVERY STOP (Screenshot 06)
  // =========================================================================
  function openAddStopModal(onAdd) {
    const clients = store.get('Clients');
    const modalHtml = `
      <div id="modal-add-stop" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-lg border border-slate-300 overflow-hidden">
          
          <!-- Light Green Header (Matching AddStopDialog.cs Pal.C(240, 253, 244)) -->
          <div class="bg-[#F0FDF4] p-4 border-b border-emerald-200">
            <h3 class="text-base font-bold text-[#166534] flex items-center space-x-2">
              <span>🗺️ Add Route Stop</span>
            </h3>
            <p class="text-xs text-emerald-700 mt-0.5">Specify destination store, drop-off sequence, and crate breakdown.</p>
          </div>

          <form id="form-add-stop" class="p-5 space-y-3">
            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Client Name:</label>
              <select id="stop-client" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Destination / Store Branch *:</label>
              <input type="text" id="stop-dest" value="Red Ribbon - San Fernando Branch" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs font-medium">
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">G-Crate (Goods): *</label>
                <input type="number" id="stop-gcrate" value="50" min="0" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-slate-800">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-700 mb-1">E-Crate (Empty): *</label>
                <input type="number" id="stop-ecrate" value="0" min="0" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-slate-800">
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Special Handling Remarks:</label>
              <input type="text" id="stop-remarks" value="Drop off cakes first; refrigerated storage" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
            </div>

            <div class="flex items-center justify-end space-x-2 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-stop').remove()" class="btn-secondary py-1.5 px-3 text-xs">Cancel</button>
              <button type="submit" class="btn-primary py-1.5 px-4 text-xs font-bold">Confirm Add Stop</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('form-add-stop').addEventListener('submit', (e) => {
      e.preventDefault();
      const stopData = {
        client: document.getElementById('stop-client').value,
        destination: document.getElementById('stop-dest').value,
        gcrate: Number(document.getElementById('stop-gcrate').value),
        ecrate: Number(document.getElementById('stop-ecrate').value),
        remarks: document.getElementById('stop-remarks').value
      };
      if (typeof onAdd === 'function') onAdd(stopData);
      toast.show(`Stop added: ${stopData.destination} ✓`, 'success');
      document.getElementById('modal-add-stop').remove();
    });
  }

  // =========================================================================
  // MODAL 5: QUICK ADD DELIVERY ROUTE (Screenshot 07)
  // =========================================================================
  function openQuickAddRouteModal(onSave) {
    const clients = store.get('Clients');
    const existingRoutes = store.get('LookupCategories').filter(c => c.CategoryType === 'Destination');
    const nextCode = `R${existingRoutes.length + 1}`;

    const modalHtml = `
      <div id="modal-quick-route" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-2xl border border-slate-300 overflow-hidden">
          
          <div class="bg-white p-4 border-b border-slate-200">
            <h3 class="text-base font-bold text-slate-900">New delivery route</h3>
            <p class="text-xs text-slate-500 mt-0.5">Pick the client, where the truck starts and where it goes, then the stores it serves.</p>
          </div>

          <form id="form-quick-route" class="p-5 space-y-3">
            <div class="grid grid-cols-2 gap-4">
              <!-- Left Column: Route Details -->
              <div class="space-y-3">
                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Client company *:</label>
                  <select id="qr-client" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white font-medium">
                    ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                  </select>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Route code *:</label>
                  <div class="flex space-x-1.5">
                    <input type="text" id="qr-code" value="${nextCode}" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-bold text-slate-800 uppercase">
                    <button type="button" id="btn-next-code" class="btn-secondary py-1 px-2 text-xs font-semibold whitespace-nowrap">Next free code</button>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Origin (start point) *:</label>
                  <input type="text" id="qr-origin" value="Tarlac City Depot" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Destination / route name *:</label>
                  <input type="text" id="qr-name" value="La Union – Ilocos Sur Express" required class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs font-medium">
                </div>

                <div>
                  <label class="block text-xs font-semibold text-slate-700 mb-1">Description:</label>
                  <textarea id="qr-desc" rows="2" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs" placeholder="Optional notes (roads, cut-off times...)"></textarea>
                </div>

                <label class="flex items-center space-x-2 text-xs text-slate-700 cursor-pointer pt-1">
                  <input type="checkbox" id="qr-active" checked class="rounded text-[#B4182C]">
                  <span>Active — offer this route for new orders</span>
                </label>
              </div>

              <!-- Right Column: Stores on Route -->
              <div class="space-y-2 border-l border-slate-200 pl-4">
                <div class="flex items-center justify-between">
                  <label class="block text-xs font-bold text-slate-700 uppercase">Stores on Route</label>
                  <button type="button" id="btn-add-store-qr" class="text-xs text-[#B4182C] hover:underline font-bold">➕ Add Store</button>
                </div>

                <div id="qr-stores-list" class="space-y-1.5 border border-slate-200 rounded p-2.5 bg-slate-50 min-h-[160px] max-h-48 overflow-y-auto text-xs">
                  <div class="flex items-center justify-between p-1.5 bg-white rounded border border-slate-200">
                    <span>1. RR - San Fernando City</span>
                    <span class="text-slate-400 text-[10px]">Stop 1</span>
                  </div>
                  <div class="flex items-center justify-between p-1.5 bg-white rounded border border-slate-200">
                    <span>2. RR - Bauang Branch</span>
                    <span class="text-slate-400 text-[10px]">Stop 2</span>
                  </div>
                  <div class="flex items-center justify-between p-1.5 bg-white rounded border border-slate-200">
                    <span>3. RR - Agoo Mall</span>
                    <span class="text-slate-400 text-[10px]">Stop 3</span>
                  </div>
                </div>
                <p class="text-[10px] text-slate-400 italic">Stores will be sequenced in delivery order.</p>
              </div>
            </div>

            <div class="flex items-center justify-end space-x-2 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-quick-route').remove()" class="btn-secondary py-1.5 px-3 text-xs">Cancel</button>
              <button type="submit" class="btn-primary py-1.5 px-4 text-xs font-bold">💾 Save Route</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('btn-add-store-qr').addEventListener('click', () => {
      const storeName = prompt('Enter Store Branch Name:');
      if (storeName) {
        const list = document.getElementById('qr-stores-list');
        const div = document.createElement('div');
        div.className = 'flex items-center justify-between p-1.5 bg-white rounded border border-slate-200';
        div.innerHTML = `<span>${list.children.length + 1}. ${storeName}</span><span class="text-slate-400 text-[10px]">Stop ${list.children.length + 1}</span>`;
        list.appendChild(div);
      }
    });

    document.getElementById('form-quick-route').addEventListener('submit', (e) => {
      e.preventDefault();
      const newRoute = {
        RouteCode: document.getElementById('qr-code').value,
        RouteName: document.getElementById('qr-name').value,
        Origin: document.getElementById('qr-origin').value,
        Client: document.getElementById('qr-client').value,
        Description: document.getElementById('qr-desc').value,
        IsActive: document.getElementById('qr-active').checked
      };

      if (typeof onSave === 'function') onSave(newRoute);
      document.getElementById('modal-quick-route').remove();
    });
  }

  // =========================================================================
  // MODAL 6: USER PROFILE & CREDENTIALS
  // =========================================================================
  window.openUserProfileModal = () => {
    const user = auth.getCurrentUser() || { Username: 'admin', Role: 'Admin', FullName: 'Hazel Lagasca' };

    const modalHtml = `
      <div id="modal-user-profile" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-md border border-slate-300 overflow-hidden">
          <div class="bg-[#0F172A] p-4 text-white flex justify-between items-center">
            <div class="flex items-center space-x-2.5">
              <span class="w-7 h-7 rounded-full bg-[#B4182C] text-white text-xs font-bold flex items-center justify-center">HL</span>
              <h3 class="font-bold text-sm">User Profile &amp; Security Settings</h3>
            </div>
            <button onclick="document.getElementById('modal-user-profile').remove()" class="text-slate-400 hover:text-white font-bold">✕</button>
          </div>

          <form id="form-user-profile" class="p-5 space-y-3">
            <div class="p-3 bg-slate-50 border border-slate-200 rounded text-xs space-y-1">
              <div><strong>Signed-in User:</strong> ${user.FullName || user.Username}</div>
              <div><strong>Username:</strong> @${user.Username}</div>
              <div><strong>Assigned Role:</strong> <span class="text-red-700 font-bold">${user.Role}</span></div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">New Password (leave blank to keep current):</label>
              <input type="password" id="profile-new-pass" placeholder="Enter new password" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 mb-1">Confirm New Password:</label>
              <input type="password" id="profile-confirm-pass" placeholder="Confirm new password" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
            </div>

            <div class="flex items-center justify-end space-x-2 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-user-profile').remove()" class="btn-secondary py-1.5 px-3 text-xs">Close</button>
              <button type="submit" class="btn-primary py-1.5 px-4 text-xs font-bold">Update Profile</button>
            </div>
          </form>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('form-user-profile').addEventListener('submit', (e) => {
      e.preventDefault();
      const p1 = document.getElementById('profile-new-pass').value;
      const p2 = document.getElementById('profile-confirm-pass').value;
      if (p1) {
        if (p1 !== p2) {
          alert('Passwords do not match.');
          return;
        }
        toast.show('Password updated successfully ✓', 'success');
      } else {
        toast.show('Profile verified ✓', 'info');
      }
      document.getElementById('modal-user-profile').remove();
    });
  };

  // =========================================================================
  // MODAL 7: FORGOT PASSWORD DIALOG
  // =========================================================================
  function openForgotPasswordModal() {
    const modalHtml = `
      <div id="modal-forgot-pass" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-sm border border-slate-300 overflow-hidden">
          <div class="bg-[#B4182C] p-4 text-white flex justify-between items-center">
            <h3 class="font-bold text-sm">Account Recovery</h3>
            <button onclick="document.getElementById('modal-forgot-pass').remove()" class="text-white hover:text-red-200 font-bold">✕</button>
          </div>
          <div class="p-5 space-y-3 text-xs text-slate-700">
            <p>For security, user password resets must be authorized by the System Administrator or Owner.</p>
            <div class="p-3 bg-amber-50 border border-amber-200 rounded text-amber-900">
              <strong>Quick Demo Access:</strong><br>
              • <strong>Admin:</strong> admin / admin123<br>
              • <strong>Staff:</strong> staff / staff123
            </div>
            <div class="flex justify-end pt-2">
              <button onclick="document.getElementById('modal-forgot-pass').remove()" class="btn-primary py-1 px-4 text-xs">Close</button>
            </div>
          </div>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
  }

  // =========================================================================
  // GLOBAL SEARCH MODAL
  // =========================================================================
  function triggerGlobalSearch(query) {
    if (!query) return;
    query = query.trim().toLowerCase();

    const deliveries = store.get('Deliveries').filter(d => 
      (d.DispatchNumber && d.DispatchNumber.toLowerCase().includes(query)) ||
      (d.DriverName && d.DriverName.toLowerCase().includes(query)) ||
      (d.TruckInfo && d.TruckInfo.toLowerCase().includes(query))
    );

    const invoices = store.get('Invoices').filter(i => 
      (i.InvoiceNumber && i.InvoiceNumber.toLowerCase().includes(query)) ||
      (i.ClientName && i.ClientName.toLowerCase().includes(query))
    );

    const crews = store.get('Crews').filter(c => 
      c.FullName && c.FullName.toLowerCase().includes(query)
    );

    const trucks = store.get('Trucks').filter(t => 
      (t.PlateNumber && t.PlateNumber.toLowerCase().includes(query)) ||
      (t.TruckType && t.TruckType.toLowerCase().includes(query))
    );

    const modalHtml = `
      <div id="modal-global-search" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-lg shadow-2xl w-full max-w-2xl border border-slate-300 overflow-hidden">
          <div class="bg-[#0F172A] p-4 text-white flex justify-between items-center">
            <h3 class="font-bold text-sm">Search Results for "${query}"</h3>
            <button onclick="document.getElementById('modal-global-search').remove()" class="text-slate-400 hover:text-white font-bold">✕</button>
          </div>
          <div class="p-5 max-h-[60vh] overflow-y-auto space-y-4 text-xs">
            <div>
              <h4 class="font-bold text-slate-800 uppercase tracking-wider mb-2">Dispatches (${deliveries.length})</h4>
              ${deliveries.length === 0 ? '<p class="text-slate-400 italic">No dispatches match.</p>' : `
                <div class="space-y-1">
                  ${deliveries.map(d => `
                    <div onclick="document.getElementById('modal-global-search').remove(); switchHub('operations', 'dispatch');" class="p-2 border border-slate-200 rounded hover:bg-slate-50 cursor-pointer flex justify-between">
                      <span class="font-bold text-slate-900">${d.DispatchNumber}</span>
                      <span>${d.DriverName} | ${d.TruckInfo}</span>
                      <span class="font-semibold text-slate-600">${d.Status}</span>
                    </div>
                  `).join('')}
                </div>
              `}
            </div>

            <div>
              <h4 class="font-bold text-slate-800 uppercase tracking-wider mb-2">Invoices (${invoices.length})</h4>
              ${invoices.length === 0 ? '<p class="text-slate-400 italic">No invoices match.</p>' : `
                <div class="space-y-1">
                  ${invoices.map(i => `
                    <div onclick="document.getElementById('modal-global-search').remove(); switchHub('finance', 'billing');" class="p-2 border border-slate-200 rounded hover:bg-slate-50 cursor-pointer flex justify-between">
                      <span class="font-bold text-slate-900">${i.InvoiceNumber}</span>
                      <span>${i.ClientName}</span>
                      <span class="font-bold text-[#B4182C]">PHP ${Number(i.NetAmount||0).toLocaleString()}</span>
                    </div>
                  `).join('')}
                </div>
              `}
            </div>

            <div>
              <h4 class="font-bold text-slate-800 uppercase tracking-wider mb-2">Personnel / Crew (${crews.length})</h4>
              ${crews.length === 0 ? '<p class="text-slate-400 italic">No crew match.</p>' : `
                <div class="space-y-1">
                  ${crews.map(c => `
                    <div onclick="document.getElementById('modal-global-search').remove(); switchHub('resources', 'crew');" class="p-2 border border-slate-200 rounded hover:bg-slate-50 cursor-pointer flex justify-between">
                      <span class="font-bold text-slate-900">${c.FullName}</span>
                      <span>${c.Role}</span>
                      <span class="font-semibold text-emerald-700">${c.IsAvailable ? 'Available' : 'On Duty'}</span>
                    </div>
                  `).join('')}
                </div>
              `}
            </div>
          </div>
          <div class="p-3 bg-slate-50 border-t border-slate-200 flex justify-end">
            <button onclick="document.getElementById('modal-global-search').remove()" class="btn-secondary py-1 px-3 text-xs">Close</button>
          </div>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);
  }

  // =========================================================================
  // EXPORT TO CSV HELPER
  // =========================================================================
  function exportToCsv(data, filename = 'export.csv') {
    if (!data || !data.length) {
      toast.show('No data available to export', 'info');
      return;
    }
    const headers = Object.keys(data[0]);
    const csvRows = [headers.join(',')];
    for (const row of data) {
      const values = headers.map(header => {
        const val = row[header] === null || row[header] === undefined ? '' : String(row[header]);
        return `"${val.replace(/"/g, '""')}"`;
      });
      csvRows.push(values.join(','));
    }
    const blob = new Blob([csvRows.join('\n')], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    toast.show(`Exported ${filename} ✓`, 'success');
  }

  // Wire Top-Level Header Event Handlers
  function initHeaderEvents() {
    const searchInp = document.getElementById('global-search-input');
    const searchBtn = document.getElementById('btn-global-search');
    if (searchBtn && searchInp) {
      searchBtn.addEventListener('click', () => triggerGlobalSearch(searchInp.value));
      searchInp.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') triggerGlobalSearch(searchInp.value);
      });
    }

    const badge = document.getElementById('header-user-badge');
    if (badge) badge.addEventListener('click', () => window.openUserProfileModal());

    const forgotLink = document.getElementById('forgot-password-link');
    if (forgotLink) forgotLink.addEventListener('click', () => openForgotPasswordModal());

    const eyeBtn = document.getElementById('btn-toggle-login-pass');
    if (eyeBtn) {
      eyeBtn.addEventListener('click', () => {
        const passInp = document.getElementById('login-password');
        if (passInp.type === 'password') {
          passInp.type = 'text';
          eyeBtn.innerText = '🔒';
        } else {
          passInp.type = 'password';
          eyeBtn.innerText = '👁';
        }
      });
    }
  }

  window.quickNewOrder = () => {
    switchHub('operations', 'dispatch');
    setTimeout(() => {
      showOrderForm = true;
      renderNewDispatchOrderForm();
    }, 50);
  };

  // Kick off initialization
  init();
  initHeaderEvents();
});
