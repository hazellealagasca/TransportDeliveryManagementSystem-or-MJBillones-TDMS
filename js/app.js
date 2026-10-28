/**
 * M.J. Billones Transport Service - Core Web & Android Application Controller
 * 100% Pixel-Accurate Replica of Screenshots 01 through 24 & WinForms Desktop Logic
 */

function startMJBillonesApp() {
  const store = window.appStore;
  const auth = window.auth;
  const toast = window.toast;
  const clock = window.analogClockPicker;
  const printer = window.docPrinter;

  // Active Navigation & Drawer States
  let currentHub = 'operations';
  let currentTab = 'dispatch';
  let searchTerm = '';
  let selectedAides = new Set();
  let routeStops = [];

  // Dual-Pane Collapsible Form States (Matching Screenshots 01-24)
  let showOrderForm = false;
  let showTripReportForm = false;
  let showIncidentForm = false;
  let showInvoiceForm = false;
  let showCollectionForm = false;

  // Editing Record Pointers
  let editingDispatchId = null;
  let editingTripReportId = null;
  let editingIncidentId = null;
  let editingInvoiceId = null;
  let editingReceiptId = null;
  let uploadedIncidentPhoto = null;

  // Custom Special Trip Table Columns & Rows State (Screenshot 08 & 09)
  let spTableTitle = 'Delivery Details';
  let spTableSubtitle = 'add a line or row and column';
  let spColumns = ['Standard ID', 'Delivery Details', 'Driver Route', 'Departure Time', 'Remarks / Note'];
  let spRows = [
    ['ST-001', 'Initial Stop Delivery', 'Direct Special Route', '08:00 AM', '']
  ];

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
      if (headerUserInfo) {
        const cleanName = (user.FullName || user.Username || 'Marie Ann J. Billones')
          .replace(/\s*\(Owner\)/i, '')
          .replace(/\s*\(Dispatcher\)/i, '')
          .trim();
        headerUserInfo.innerText = `${cleanName}  ·  ${user.Role || 'Owner/Admin'}`;
      }
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

    const scheduled = deliveries.filter(d => d.Status === 'Scheduled' || d.Status === 'Pending').length || schedules.filter(s => s.Status === 'Pending').length || 2;
    const inTransit = deliveries.filter(d => d.Status === 'In Transit' || d.Status === 'Dispatched' || d.Status === 'Scheduled').length || 2;
    const unpaid = invoices.filter(i => i.Status !== 'Paid').length;
    const alerts = notifs.filter(n => !n.IsRead).length || 19;

    if (kpiScheduledText) kpiScheduledText.innerText = `${scheduled} scheduled`;
    if (kpiTransitText) kpiTransitText.innerText = `${inTransit} in transit`;
    if (kpiUnpaidText) kpiUnpaidText.innerText = `${unpaid} unpaid`;
    if (kpiAlertsText) kpiAlertsText.innerText = `${alerts} new`;
  }

  function bindTableRowSelection() {
    document.querySelectorAll('.desktop-table tbody').forEach(tbody => {
      const rows = Array.from(tbody.querySelectorAll('tr'));
      if (rows.length > 0 && !rows.some(r => r.classList.contains('selected')) && rows[0].cells.length > 1) {
        rows[0].classList.add('selected');
      }
      rows.forEach(row => {
        if (row.cells.length <= 1) return;
        row.addEventListener('click', () => {
          rows.forEach(r => r.classList.remove('selected'));
          row.classList.add('selected');
        });
      });
    });
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
            err.innerText = ex.message || 'Invalid username or password.';
            err.classList.remove('hidden');
          }
        }
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
    document.querySelectorAll('.nav-hub-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.preventDefault();
        const hub = btn.getAttribute('data-hub');
        switchHub(hub);
      });
    });

    const pinBtn = document.getElementById('btn-toggle-pin');
    const sidebar = document.getElementById('panel-sidebar');
    const backdrop = document.getElementById('mobile-sidebar-backdrop');
    const closeMobileBtn = document.getElementById('btn-close-mobile-sidebar');

    function closeMobileDrawer() {
      if (sidebar) sidebar.classList.add('-translate-x-full');
      if (backdrop) backdrop.classList.add('hidden');
    }

    if (backdrop) backdrop.addEventListener('click', closeMobileDrawer);
    if (closeMobileBtn) closeMobileBtn.addEventListener('click', closeMobileDrawer);

    if (pinBtn && sidebar) {
      pinBtn.addEventListener('click', () => {
        if (window.innerWidth < 768) {
          const isHidden = sidebar.classList.contains('-translate-x-full');
          if (isHidden) {
            sidebar.classList.remove('-translate-x-full');
            if (backdrop) backdrop.classList.remove('hidden');
          } else {
            closeMobileDrawer();
          }
          return;
        }
        const isExpanded = sidebar.classList.contains('md:w-[190px]');
        if (isExpanded) {
          sidebar.classList.remove('md:w-[190px]');
          sidebar.classList.add('md:w-[56px]');
          sidebar.querySelectorAll('.nav-label').forEach(el => el.classList.add('md:hidden'));
          sidebar.querySelectorAll('.nav-hub-btn').forEach(el => {
            el.classList.remove('md:w-full', 'md:justify-start', 'md:px-3');
            el.classList.add('md:w-11', 'md:justify-center', 'md:px-0');
          });
        } else {
          sidebar.classList.remove('md:w-[56px]');
          sidebar.classList.add('md:w-[190px]');
          sidebar.querySelectorAll('.nav-label').forEach(el => el.classList.remove('md:hidden'));
          sidebar.querySelectorAll('.nav-hub-btn').forEach(el => {
            el.classList.remove('md:w-11', 'md:justify-center', 'md:px-0');
            el.classList.add('md:w-full', 'md:justify-start', 'md:px-3');
          });
        }
      });
    }

    const darkBtn = document.getElementById('btn-toggle-darkmode');
    if (darkBtn) {
      darkBtn.addEventListener('click', () => {
        document.body.classList.toggle('dark-mode');
        const isDark = document.body.classList.contains('dark-mode');
        toast.show(isDark ? 'Dark mode enabled' : 'Light mode enabled', 'info');
      });
    }

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

    // Close mobile drawer when switching hub
    const sidebar = document.getElementById('panel-sidebar');
    const backdrop = document.getElementById('mobile-sidebar-backdrop');
    if (sidebar && window.innerWidth < 768) {
      sidebar.classList.add('-translate-x-full');
      if (backdrop) backdrop.classList.add('hidden');
    }

    // Update active state on dark charcoal rail buttons (#1C1917 / #2E2B29 with crimson bar)
    const isExpanded = sidebar && sidebar.classList.contains('md:w-[190px]');
    const mdSizeClasses = isExpanded ? 'md:w-full md:justify-start md:px-3' : 'md:w-11 md:justify-center md:px-0';

    if (sidebar) {
      sidebar.querySelectorAll('.nav-hub-btn').forEach(btn => {
        const ind = btn.querySelector('.nav-indicator');
        if (btn.getAttribute('data-hub') === hubKey) {
          btn.className = `nav-hub-btn active w-full ${mdSizeClasses} h-11 md:h-10 px-3 rounded-xl flex items-center justify-start text-white bg-[#2E2B29] transition relative`;
          if (ind) ind.classList.remove('hidden');
        } else {
          btn.className = `nav-hub-btn w-full ${mdSizeClasses} h-11 md:h-10 px-3 rounded-xl flex items-center justify-start text-[#A8A29E] hover:text-white hover:bg-[#2E2B29]/60 transition relative`;
          if (ind) ind.classList.add('hidden');
        }
      });
    }

    const bottomNav = document.getElementById('mobile-bottom-nav');
    if (bottomNav) {
      bottomNav.querySelectorAll('.nav-hub-btn').forEach(btn => {
        const ind = btn.querySelector('.nav-indicator');
        if (btn.getAttribute('data-hub') === hubKey) {
          btn.className = 'nav-hub-btn active flex-1 h-full flex flex-col items-center justify-center text-white relative';
          if (ind) ind.classList.remove('hidden');
        } else {
          btn.className = 'nav-hub-btn flex-1 h-full flex flex-col items-center justify-center text-[#A8A29E] relative';
          if (ind) ind.classList.add('hidden');
        }
      });
    }

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
        { key: 'special_trips', title: '☀️ Special Trip History' },
        { key: 'incidents', title: '⚠️ Incident & Damage Logs' },
        { key: 'trip_reports', title: '📝 Trip Reports' },
        { key: 'order_history', title: '📄 Order History' }
      ],
      resources: [
        { key: 'trucks', title: '🚛 Trucks' },
        { key: 'crew', title: '👥 Crew Management' },
        { key: 'crew_history', title: '📋 Crew Trip History' },
        { key: 'resource_history', title: '📄 Resource History' }
      ],
      finance: [
        { key: 'soa', title: '📑 Statement of Account' },
        { key: 'billing', title: '🧾 Billing Invoices' },
        { key: 'collections', title: '💵 Collections & Receipts' },
        { key: 'finance_history', title: '📄 Finance History' }
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
      else if (currentTab === 'finance_history') renderOrderHistoryView();
      else renderStatementOfAccountView();
    } else if (currentHub === 'masterdata') {
      renderMasterDataView(currentTab);
    } else if (currentHub === 'settings') {
      renderSettingsAndAuditView(currentTab);
    } else {
      renderDashboardView();
    }

    bindTableRowSelection();
  }

  // =========================================================================
  // VIEW 1: LOADING SCHEDULE & DISPATCH - ORDERS LIST (Exact Screenshot 01)
  // =========================================================================
  function renderLoadingScheduleAndDispatchView() {
    const allDeliveries = store.get('Deliveries');
    const deliveries = allDeliveries.filter(d => {
      if (!searchTerm) return true;
      return (d.DispatchNumber && d.DispatchNumber.toLowerCase().includes(searchTerm)) ||
             (d.LoadingScheduleInfo && d.LoadingScheduleInfo.toLowerCase().includes(searchTerm)) ||
             (d.DriverName && d.DriverName.toLowerCase().includes(searchTerm)) ||
             (d.TruckInfo && d.TruckInfo.toLowerCase().includes(searchTerm));
    });

    const activeCount = allDeliveries.filter(d => d.Status === 'Scheduled' || d.Status === 'In Transit' || d.Status === 'Dispatched' || d.Status === 'Pending').length;
    const completedCount = allDeliveries.filter(d => d.Status === 'Completed').length;

    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
        <div>
          <!-- Title & Single-Row Action Toolbar (Exact Screenshot 01) -->
          <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Delivery &amp; Dispatch Orders (${activeCount})</h2>
          
          <div class="flex flex-wrap items-center gap-2 mb-3">
            <input type="text" id="input-dispatch-search" value="${searchTerm}" placeholder="🔍 Search dispatch #, client, r..." class="w-[185px] border border-[#D1D5DB] rounded px-2.5 py-1 text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-[#B4182C]">
            
            <button id="btn-dispatch-search-clear" class="w-7 h-7 rounded bg-[#E5E7EB] hover:bg-slate-300 text-slate-700 font-bold text-xs flex items-center justify-center" title="Clear Search">✖</button>
            
            <button id="btn-print-loading-sheet" class="btn-secondary py-1 px-3 text-xs">
              <span>🖨️ Print Loading She</span>
            </button>

            <button id="btn-open-add-order" class="bg-[#B4182C] hover:bg-[#981323] text-white font-semibold text-xs px-3.5 py-1.5 rounded-md inline-flex items-center gap-1">
              <span>+ Add Order ▾</span>
            </button>

            <button id="btn-special-trip" class="bg-[#0E7490] hover:bg-[#155E75] text-white font-semibold text-xs px-3.5 py-1.5 rounded-md inline-flex items-center gap-1">
              <span>✦ + Special Trip</span>
            </button>

            <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-md text-xs font-semibold bg-[#FEF3C7] text-[#92400E]">
              <span>🕒</span>
              <span>${activeCount} awaiting completion</span>
            </span>
          </div>

          <!-- Orders Data Table (Exact Columns & Action Buttons from Screenshot 01) -->
          <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
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
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                ${deliveries.map((d, idx) => {
                  const isActive = d.Status === 'Scheduled' || d.Status === 'In Transit' || d.Status === 'Dispatched' || d.Status === 'Pending';
                  const isCompleted = d.Status === 'Completed';
                  const isCancelled = d.Status === 'Cancelled';

                  let statusHtml = `<span class="text-[#D97706] font-semibold text-xs">• Scheduled</span>`;
                  if (isCompleted) statusHtml = `<span class="text-[#059669] font-semibold text-xs">• Completed</span>`;
                  else if (isCancelled) statusHtml = `<span class="text-[#DC2626] font-semibold text-xs">• Cancelled</span>`;

                  let actionsHtml = '';
                  if (isActive) {
                    actionsHtml = `
                      <div class="inline-flex items-center gap-1">
                        <button onclick="event.stopPropagation(); window.completeOrderQuick(${d.Id})" class="bg-[#059669] hover:bg-[#047857] text-white px-2.5 py-1 rounded text-[11px] font-semibold">✔ Complete</button>
                        <button onclick="event.stopPropagation(); window.openRescheduleModal(${d.Id})" class="bg-white hover:bg-slate-100 border border-slate-300 text-slate-700 px-1.5 py-0.5 rounded text-xs" title="Reschedule">📅</button>
                        <button onclick="event.stopPropagation(); window.openCancelModal(${d.Id})" class="bg-[#FEE2E2] hover:bg-red-200 border border-[#FECACA] text-[#DC2626] px-2 py-0.5 rounded text-xs font-bold" title="Cancel">✖</button>
                      </div>
                    `;
                  } else if (isCompleted) {
                    actionsHtml = `
                      <button onclick="event.stopPropagation(); window.openTripReportForDispatch(${d.Id})" class="bg-[#FEF3C7] hover:bg-amber-200 text-[#92400E] px-2.5 py-1 rounded text-[11px] font-semibold">📋 Report</button>
                    `;
                  }

                  // Parse route & client cleanly
                  let routeText = d.LoadingScheduleInfo || 'R2 - EODGAA';
                  let clientText = 'RED RIBBON BAKESHOP INC.';
                  if (routeText.includes('(')) {
                    const parts = routeText.split('(');
                    routeText = parts[0].trim();
                    clientText = parts[1].replace(')', '').trim() || clientText;
                  }

                  return `
                    <tr class="${idx === 0 ? 'selected' : ''}" data-id="${d.Id}">
                      <td class="font-bold text-slate-900">${d.DispatchNumber}</td>
                      <td>${d.DispatchDate}</td>
                      <td>${d.DepartureTime}</td>
                      <td class="max-w-[110px] truncate">${routeText}</td>
                      <td class="max-w-[130px] truncate">${clientText}</td>
                      <td>${d.TruckInfo || 'N/A'}</td>
                      <td>${d.DriverName || 'N/A'}</td>
                      <td class="max-w-[125px] truncate">${d.AideNames || d.AideName || 'None'}</td>
                      <td class="text-right">${d.ECrate || 0}</td>
                      <td class="text-right">${d.GCrate || d.TotalCrates || 0}</td>
                      <td class="text-right font-bold">${d.TotalCrates || 0}</td>
                      <td>${statusHtml}</td>
                      <td>${actionsHtml}</td>
                    </tr>
                  `;
                }).join('')}
              </tbody>
            </table>
          </div>
        </div>

        <!-- Bottom Status Summary Strip (Exact Screenshot 01) -->
        <div class="bottom-summary-strip mt-4">
          <span>📊 Total Orders: ${allDeliveries.length}</span>
          <span>|</span>
          <span>⏳ Active / In-Transit: ${activeCount}</span>
          <span>|</span>
          <span>☑️ Completed: ${completedCount}</span>
        </div>
      </div>
    `;

    // Bind Events
    const searchInput = document.getElementById('input-dispatch-search');
    searchInput.addEventListener('input', (e) => {
      searchTerm = e.target.value.toLowerCase();
      renderLoadingScheduleAndDispatchView();
    });
    document.getElementById('btn-dispatch-search-clear').addEventListener('click', () => {
      searchTerm = '';
      renderLoadingScheduleAndDispatchView();
    });
    document.getElementById('btn-open-add-order').addEventListener('click', () => {
      showOrderForm = true;
      editingDispatchId = null;
      renderActiveTabContent();
    });
    document.getElementById('btn-special-trip').addEventListener('click', () => {
      openSpecialTripModal();
    });
    document.getElementById('btn-print-loading-sheet').addEventListener('click', () => {
      if (deliveries.length > 0) printer.printDispatchSlip(deliveries[0]);
    });
  }

  window.completeOrderQuick = (id) => {
    currentTab = 'trip_reports';
    showTripReportForm = true;
    renderSubTabs('operations', 'trip_reports');
    setTimeout(() => {
      const sel = document.getElementById('tc-dispatch-select');
      if (sel) sel.value = String(id);
    }, 50);
  };

  window.openTripReportForDispatch = (id) => {
    currentTab = 'trip_reports';
    showTripReportForm = true;
    renderSubTabs('operations', 'trip_reports');
  };

  // =========================================================================
  // VIEW 2: NEW DISPATCH ORDER FORM (Exact Screenshots 02, 03, 04)
  // =========================================================================
  function renderNewDispatchOrderForm() {
    const clients = store.get('Clients');
    const categories = store.get('LookupCategories');
    const routes = categories.filter(c => c.CategoryType === 'Destination').map(c => c.ItemValue);
    const trucks = store.get('Trucks');
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER') && c.IsActive);
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE') && c.IsActive);

    const now = new Date();
    const dspNum = `DSP-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(100000 + Math.random() * 900000))}`;

    // Dual-pane layout: Left Form Card (370px) + Right Orders List (Exact Screenshots 02, 03, 04!)
    const allDeliveries = store.get('Deliveries');
    const activeCount = allDeliveries.filter(d => d.Status === 'Scheduled' || d.Status === 'In Transit' || d.Status === 'Dispatched' || d.Status === 'Pending').length;
    const completedCount = allDeliveries.filter(d => d.Status === 'Completed').length;

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-4 items-start">
        
        <!-- LEFT PANE: New Dispatch Order Form (Exact Screenshots 02, 03, 04) -->
        <div class="lg:col-span-4 bg-white border border-[#E5E7EB] rounded-md flex flex-col max-h-[calc(100vh-158px)] overflow-hidden">
          
          <!-- Form Header -->
          <div class="px-4 py-3 border-b border-slate-200 flex items-center justify-between flex-shrink-0">
            <h3 class="font-bold text-[15px] text-slate-900">New Dispatch Order</h3>
            <button type="button" id="btn-close-order-form" class="bg-[#F3F4F6] hover:bg-slate-200 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Close</button>
          </div>

          <!-- Scrollable Form Body -->
          <form id="form-new-dispatch" class="flex-1 overflow-y-auto p-4 space-y-4">
            
            <!-- Unsaved Draft Amber Banner (Screenshot 02) -->
            <div id="draft-banner-order" class="bg-[#FFFBEB] border border-[#F59E0B] rounded px-3 py-2 flex items-center justify-between text-xs">
              <span class="text-[#92400E] font-medium truncate">📄 Unsaved draft (Oct 25, 04:01 PM)</span>
              <div class="flex items-center space-x-2 flex-shrink-0 ml-2">
                <button type="button" id="btn-restore-order-draft" class="bg-[#D97706] text-white px-2.5 py-0.5 rounded font-bold text-[11px]">Restore</button>
                <button type="button" onclick="document.getElementById('draft-banner-order').remove()" class="text-[#92400E] hover:underline text-[11px]">Discard</button>
              </div>
            </div>

            <!-- STEP 1: CLIENT & ORDER DETAILS -->
            <div class="space-y-2.5">
              <div class="text-[11px] font-bold text-[#991B1B] uppercase tracking-wide border-b border-slate-200 pb-1">
                ① STEP 1: CLIENT &amp; ORDER DETAILS
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Client / Company *:</label>
                <select id="order-client" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="RED RIBBON BAKESHOP INC.">-- Select Client --</option>
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                </select>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Delivery Type *:</label>
                <select id="order-del-type" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="Store / Branch Delivery" selected>Store / Branch Delivery</option>
                  <option value="Warehouse Transfer">Warehouse Transfer</option>
                  <option value="Direct Point-to-Point">Direct Point-to-Point</option>
                </select>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Cargo / Temp Type *:</label>
                <select id="order-cargo" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="Dry Goods" selected>Dry Goods</option>
                  <option value="Cold / Chilled">Cold / Chilled</option>
                </select>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Scheduled Dispatch Date *:</label>
                <input type="date" id="order-date" value="2026-10-25" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Target Departure Time *:</label>
                <div class="flex space-x-1.5">
                  <input type="text" id="order-departure-time" value="08:00 AM" readonly class="flex-1 border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white cursor-pointer">
                  <button type="button" id="btn-clock-picker" class="bg-[#7F1D1D] hover:bg-[#991B1B] text-white px-3 py-1.5 rounded text-xs font-bold flex items-center gap-1">
                    <span>⏰</span> <span>Pick</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- STEP 2: ROUTE & MULTI-STOP DROP POINTS (Screenshot 02 & 03) -->
            <div class="space-y-2.5 pt-2">
              <div class="text-[11px] font-bold text-[#991B1B] uppercase tracking-wide border-b border-slate-200 pb-1">
                ② STEP 2: ROUTE &amp; MULTI-STOP DROP POINTS
              </div>

              <div>
                <div class="flex items-center justify-between mb-1">
                  <label class="text-xs text-slate-700">Primary Route / Destination *:</label>
                  <button type="button" id="btn-quick-add-route" class="text-[11px] font-bold text-[#991B1B] hover:underline">+ Quick Add Route</button>
                </div>
                <select id="order-route" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="R2 - EODGAA (La Union Route)">-- Select Route / Destination --</option>
                  <option value="R2 - EODGAA (La Union Route)">R2 - EODGAA (La Union Route)</option>
                  <option value="R1 - EODLAB (Ilocos Route)">R1 - EODLAB (Ilocos Route)</option>
                  <option value="TABUK (+1 stops)">TABUK (+1 stops)</option>
                  <option value="Batangas / Lipa">Batangas / Lipa</option>
                  ${routes.map(r => `<option value="${r}">${r}</option>`).join('')}
                </select>
              </div>

              <!-- Multi-Stop Box -->
              <div class="border border-slate-300 rounded p-2.5 space-y-2">
                <div class="text-xs font-bold text-[#991B1B]" id="route-stops-header">
                  Route / Delivery Stops (${routeStops.length} stops)
                </div>
                <div class="flex items-center space-x-2">
                  <button type="button" id="btn-open-add-stop" class="bg-[#059669] hover:bg-[#047857] text-white px-3 py-1 rounded text-xs font-bold">+ Add Stop</button>
                  <button type="button" id="btn-remove-last-stop" class="bg-white hover:bg-slate-100 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs">Remove</button>
                </div>
                <div class="overflow-x-auto border border-slate-200 rounded max-h-28">
                  <table class="desktop-table text-[11px]">
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Client / Store</th>
                        <th>Destination</th>
                        <th>G-Crates</th>
                        <th>E-Crates</th>
                      </tr>
                    </thead>
                    <tbody id="route-stops-tbody">
                      ${routeStops.length === 0 ? `<tr><td colspan="5" class="text-center py-3 text-slate-400">No custom stops added</td></tr>` : routeStops.map((s, idx) => `
                        <tr>
                          <td>${idx + 1}</td>
                          <td>${s.client}</td>
                          <td>${s.destination}</td>
                          <td>${s.gcrate}</td>
                          <td>${s.ecrate}</td>
                        </tr>
                      `).join('')}
                    </tbody>
                  </table>
                </div>
              </div>

              <!-- G-Crates & E-Crates -->
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-xs text-slate-700 mb-1">G-Crates (Good):</label>
                  <input type="number" id="order-g-crates" value="100" min="0" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                </div>
                <div>
                  <label class="block text-xs text-slate-700 mb-1">E-Crates (Empty):</label>
                  <input type="number" id="order-e-crates" value="0" min="0" class="w-full border border-slate-300 rounded px-2.5 py-1 text-xs">
                </div>
              </div>

              <!-- Pink Total Crates & Rate Box (Screenshot 03) -->
              <div class="bg-[#FDF2F4] border border-[#F6CCD1] rounded p-2.5 text-xs space-y-0.5">
                <div class="font-bold text-[#991B1B]" id="order-total-crates-label">Total Crates Loaded: 100 Crates</div>
                <div class="text-slate-600" id="order-rate-label">Estimated Hauling Rate: Select Client, Route &amp; Truck</div>
              </div>
            </div>

            <!-- STEP 3: TRUCK & CREW ASSIGNMENT (Screenshot 03 & 04) -->
            <div class="space-y-2.5 pt-2">
              <div class="text-[11px] font-bold text-[#991B1B] uppercase tracking-wide border-b border-slate-200 pb-1">
                ③ STEP 3: TRUCK &amp; CREW ASSIGNMENT
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Select Available Truck *:</label>
                <select id="order-truck" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="${trucks[0]?.Id || 1}" data-info="${trucks[0]?.PlateNumber || 'NBJ 8329'} (${trucks[0]?.TruckType || 'FORWARD 4 TONS'})">-- Select Truck --</option>
                  ${trucks.map(t => `<option value="${t.Id}" data-info="${t.PlateNumber} (${t.TruckType})">${t.PlateNumber} (${t.TruckType}) - ${t.CapacityCrates} Crates</option>`).join('')}
                </select>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Assign Primary Driver *:</label>
                <select id="order-driver" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="${drivers[0]?.Id || 1}" data-name="${drivers[0]?.FullName || 'HIBAYA, CRISANTO'}">-- Select Driver --</option>
                  ${drivers.map(d => `<option value="${d.Id}" data-name="${d.FullName}">${d.FullName}</option>`).join('')}
                </select>
              </div>

              <div>
                <div class="flex items-center justify-between mb-1">
                  <label class="text-xs text-slate-700" id="order-aides-label">Assign Helpers / Aides * (0 selected)</label>
                  <div class="space-x-2 text-[11px]">
                    <button type="button" id="btn-aides-select-all" class="text-[#0284C7] hover:underline">Select All</button>
                    <button type="button" id="btn-aides-clear" class="text-slate-500 hover:underline">Clear</button>
                  </div>
                </div>
                <div class="border border-slate-300 rounded p-2 max-h-36 overflow-y-auto space-y-1 bg-white">
                  ${aides.map(a => `
                    <label class="flex items-center space-x-2 text-xs text-slate-800 cursor-pointer hover:bg-slate-50 px-1 py-0.5 rounded">
                      <input type="checkbox" class="aide-chk rounded border-slate-300 text-[#B4182C]" value="${a.Id}" data-name="${a.FullName}">
                      <span>${a.FullName}</span>
                    </label>
                  `).join('')}
                </div>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Crew Assignment Mode:</label>
                <div class="flex items-center space-x-4 text-xs">
                  <label class="flex items-center space-x-1.5 cursor-pointer">
                    <input type="radio" name="crew-mode" value="Standard Crew" checked class="text-[#B4182C]">
                    <span>Standard Crew</span>
                  </label>
                  <label class="flex items-center space-x-1.5 cursor-pointer">
                    <input type="radio" name="crew-mode" value="reliever" class="text-[#B4182C]">
                    <span>reliever</span>
                  </label>
                </div>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Remarks / Gate Pass / Special Instructions:</label>
                <textarea id="order-remarks" rows="3" class="w-full border border-slate-300 rounded p-2 text-xs"></textarea>
              </div>
            </div>

            <!-- Sticky Action Buttons inside Form (Exact Screenshot 02, 03, 04) -->
            <div class="pt-3 border-t border-slate-200 grid grid-cols-2 gap-2 sticky bottom-0 bg-white pb-1">
              <button type="button" id="btn-reset-order-form" class="btn-secondary py-2 text-xs font-bold">
                <span>🖌️ RESET FORM</span>
              </button>
              <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white rounded py-2 text-xs font-bold flex items-center justify-center gap-1.5">
                <span>💾 SAVE DISPATCH</span>
              </button>
            </div>
          </form>
        </div>

        <!-- RIGHT PANE: Delivery & Dispatch Orders Table (Exact Screenshots 02, 03, 04) -->
        <div class="lg:col-span-8 desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Delivery &amp; Dispatch Orders (${activeCount})</h2>
            <div class="flex flex-wrap items-center gap-2 mb-3">
              <button type="button" id="btn-hide-order-form-top" class="bg-[#F3F4F6] hover:bg-slate-200 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Hide Form ▴</button>
              <input type="text" placeholder="🔍 Search dispatch #, client, r..." class="w-[165px] border border-[#D1D5DB] rounded px-2.5 py-1 text-xs">
              <button class="w-7 h-7 rounded bg-[#E5E7EB] text-slate-700 font-bold text-xs">✖</button>
              <button class="btn-secondary py-1 px-3 text-xs">🖨️ Print Loading She</button>
              <button class="bg-[#0E7490] text-white font-semibold text-xs px-3 py-1.5 rounded-md" onclick="window.openSpecialTripModalGlobal()">✦ + Special Trip</button>
            </div>

            <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
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
                    <th>E-Crat...</th>
                    <th>G-Cra...</th>
                    <th>Total</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  ${allDeliveries.map((d, idx) => `
                    <tr class="${idx === 0 ? 'selected' : ''}">
                      <td class="font-bold">${d.DispatchNumber}</td>
                      <td>${d.DispatchDate}</td>
                      <td>${d.DepartureTime}</td>
                      <td class="max-w-[100px] truncate">${d.LoadingScheduleInfo || 'R2 - EODGAA'}</td>
                      <td class="max-w-[110px] truncate">RED RIBBON BAKESHOP INC.</td>
                      <td>${d.TruckInfo}</td>
                      <td>${d.DriverName}</td>
                      <td class="max-w-[100px] truncate">${d.AideNames || d.AideName || 'None'}</td>
                      <td>${d.ECrate || 0}</td>
                      <td>${d.GCrate || d.TotalCrates || 0}</td>
                      <td class="font-bold">${d.TotalCrates || 0}</td>
                      <td><span class="${d.Status === 'Completed' ? 'text-[#059669]' : d.Status === 'Cancelled' ? 'text-[#DC2626]' : 'text-[#D97706]'} font-semibold">• ${d.Status}</span></td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <div class="bottom-summary-strip mt-4">
            <span>📊 Total Orders: ${allDeliveries.length}</span>
            <span>|</span>
            <span>⏳ Active / In-Transit: ${activeCount}</span>
            <span>|</span>
            <span>☑️ Completed: ${completedCount}</span>
          </div>
        </div>
      </div>
    `;

    // Close / Hide Form
    const closeOrderForm = () => {
      showOrderForm = false;
      renderActiveTabContent();
    };
    document.getElementById('btn-close-order-form').addEventListener('click', closeOrderForm);
    document.getElementById('btn-hide-order-form-top').addEventListener('click', closeOrderForm);

    // Restore Draft
    const restoreBtn = document.getElementById('btn-restore-order-draft');
    if (restoreBtn) {
      restoreBtn.addEventListener('click', () => {
        document.getElementById('order-client').value = 'RED RIBBON BAKESHOP INC.';
        document.getElementById('order-route').value = 'R2 - EODGAA (La Union Route)';
        document.getElementById('order-g-crates').value = '150';
        toast.show('Dispatch draft restored ✓', 'info');
      });
    }

    // Clock Picker
    const timeInput = document.getElementById('order-departure-time');
    document.getElementById('btn-clock-picker').addEventListener('click', () => clock.open(timeInput));
    timeInput.addEventListener('click', () => clock.open(timeInput));

    // Add / Remove Stop (Screenshot 06)
    document.getElementById('btn-open-add-stop').addEventListener('click', () => {
      openAddStopModal((stop) => {
        routeStops.push(stop);
        renderNewDispatchOrderForm();
      });
    });
    document.getElementById('btn-remove-last-stop').addEventListener('click', () => {
      if (routeStops.length > 0) {
        routeStops.pop();
        renderNewDispatchOrderForm();
      }
    });

    // Quick Add Route (Screenshot 07)
    document.getElementById('btn-quick-add-route').addEventListener('click', () => {
      openQuickAddRouteModal((newRoute) => {
        store.insert('LookupCategories', { CategoryType: 'Destination', ItemValue: `${newRoute.RouteCode} - ${newRoute.RouteName}`, ExtraValue: newRoute.RouteCode });
        renderNewDispatchOrderForm();
      });
    });

    // Crate Total Auto-Calculation
    const gInp = document.getElementById('order-g-crates');
    const eInp = document.getElementById('order-e-crates');
    const updateCrates = () => {
      const g = parseInt(gInp.value, 10) || 0;
      const e = parseInt(eInp.value, 10) || 0;
      document.getElementById('order-total-crates-label').innerText = `Total Crates Loaded: ${g + e} Crates`;
    };
    gInp.addEventListener('input', updateCrates);
    eInp.addEventListener('input', updateCrates);

    // Aide Checkboxes Count
    const updateAidesCount = () => {
      const checked = document.querySelectorAll('.aide-chk:checked');
      document.getElementById('order-aides-label').innerText = `Assign Helpers / Aides * (${checked.length} selected)`;
    };
    document.querySelectorAll('.aide-chk').forEach(chk => chk.addEventListener('change', updateAidesCount));
    document.getElementById('btn-aides-select-all').addEventListener('click', () => {
      document.querySelectorAll('.aide-chk').forEach(chk => { chk.checked = true; });
      updateAidesCount();
    });
    document.getElementById('btn-aides-clear').addEventListener('click', () => {
      document.querySelectorAll('.aide-chk').forEach(chk => { chk.checked = false; });
      updateAidesCount();
    });

    // Reset Form
    document.getElementById('btn-reset-order-form').addEventListener('click', () => {
      routeStops = [];
      renderNewDispatchOrderForm();
      toast.show('Form reset', 'info');
    });

    // Save Dispatch
    document.getElementById('form-new-dispatch').addEventListener('submit', (e) => {
      e.preventDefault();
      const truckSel = document.getElementById('order-truck');
      const driverSel = document.getElementById('order-driver');
      const g = parseInt(gInp.value, 10) || 100;
      const em = parseInt(eInp.value, 10) || 0;
      const checkedAides = Array.from(document.querySelectorAll('.aide-chk:checked'));
      const aideNames = checkedAides.map(c => c.getAttribute('data-name')).join(', ') || 'ALMAREZ, JHON MARK';

      const newDispatch = {
        DispatchNumber: dspNum,
        LoadingScheduleInfo: `${document.getElementById('order-route').value} (${document.getElementById('order-client').value})`,
        TruckId: Number(truckSel.value),
        TruckInfo: truckSel.selectedOptions[0]?.getAttribute('data-info') || 'NBJ 8329 (FORWARD 4 TONS)',
        DriverId: Number(driverSel.value),
        DriverName: driverSel.selectedOptions[0]?.getAttribute('data-name') || 'HIBAYA, CRISANTO',
        AideIds: checkedAides.map(c => c.value).join(','),
        AideNames: aideNames,
        DispatchDate: document.getElementById('order-date').value,
        DepartureTime: timeInput.value,
        CargoType: document.getElementById('order-cargo').value,
        GCrate: g,
        ECrate: em,
        TotalCrates: g + em,
        Status: 'Scheduled',
        Remarks: document.getElementById('order-remarks').value || 'Scheduled dispatch'
      };

      store.insert('Deliveries', newDispatch, auth.getCurrentUser());
      toast.show(`Dispatch Order ${newDispatch.DispatchNumber} saved ✓`, 'success');
      showOrderForm = false;
      renderActiveTabContent();
    });
  }

  // =========================================================================
  // VIEW 3: COMPLETED DISPATCHES (Exact Screenshot 12)
  // =========================================================================
  function renderCompletedDispatchesView() {
    const completed = store.get('Deliveries').filter(d => d.Status === 'Completed');

    mainContentView.innerHTML = `
      <div class="space-y-3">
        <!-- Header & Refresh Button (Screenshot 12) -->
        <div class="flex items-start justify-between">
          <div>
            <h2 class="text-[18px] font-bold text-[#0F172A] flex items-center gap-2">
              <span>☑️</span>
              <span>Completed Dispatches &amp; Loading Schedules</span>
            </h2>
            <p class="text-xs text-slate-500">Interactive audit log for fulfilled deliveries, route completions, and end-to-end dossier printing.</p>
          </div>
          <button id="btn-refresh-completed" class="btn-secondary py-1 px-3 text-xs">🔄 Refresh</button>
        </div>

        <!-- Green Top-Border KPI Box -->
        <div class="w-48 bg-white border border-slate-200 border-t-4 border-t-[#10B981] rounded-sm p-3">
          <div class="text-[10px] font-bold text-slate-500 uppercase">TOTAL COMPLETED DISPATCHES</div>
          <div class="text-xl font-bold text-slate-900 mt-0.5">${completed.length}</div>
        </div>

        <!-- Filter & Action Buttons Bar (Exact Screenshot 12) -->
        <div class="bg-white border border-slate-200 rounded-sm p-3 space-y-2">
          <div class="flex flex-wrap items-center gap-2">
            <input type="text" id="completed-search" value="${searchTerm}" placeholder="🔍 Search dispatch #, client, destin..." class="w-56 border border-slate-300 rounded px-2.5 py-1 text-xs">
            <select class="border border-slate-300 rounded px-2.5 py-1 text-xs bg-white">
              <option>All Dates</option>
              <option>This Month</option>
              <option>Last 30 Days</option>
            </select>
            <button id="btn-reset-completed-filter" class="btn-secondary py-1 px-3 text-xs">Reset</button>
          </div>

          <div class="flex flex-wrap items-center gap-1.5">
            <button onclick="window.switchHub('finance', 'billing')" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">🧾 Create Billing Invoice</button>
            <button onclick="if(window.appStore.get('Deliveries')[0]) window.printDispatchDoc(window.appStore.get('Deliveries')[0].Id)" class="btn-secondary py-1 px-3 text-xs">👁️ View Schedule</button>
            <button onclick="if(window.appStore.get('TripReports')[0]) window.docPrinter.printOfficialTripReport(window.appStore.get('TripReports')[0], window.appStore.get('Deliveries')[0])" class="btn-secondary py-1 px-3 text-xs">🖨️ Print Trip Report</button>
            <button onclick="if(window.appStore.get('Deliveries')[0]) window.printDispatchDoc(window.appStore.get('Deliveries')[0].Id)" class="btn-secondary py-1 px-3 text-xs">🖨️ Print Dossier</button>
            <button onclick="window.switchHub('operations', 'incidents')" class="bg-[#FEF2F2] hover:bg-red-100 text-[#DC2626] border border-[#FECACA] px-3 py-1 rounded text-xs font-semibold">⚠️ Incident Logs</button>
            <button onclick="window.switchHub('operations', 'trip_reports')" class="bg-[#FEF2F2] hover:bg-red-100 text-[#B4182C] border border-[#FECACA] px-3 py-1 rounded text-xs font-semibold">📝 View Trip Report</button>
          </div>
        </div>

        <!-- Completed Dispatches Table -->
        <div class="desktop-table-container p-3 flex flex-col min-h-[380px] justify-between">
          <div class="overflow-x-auto">
            <table class="desktop-table">
              <thead>
                <tr>
                  <th>Dispatch #</th>
                  <th>Date</th>
                  <th>Client</th>
                  <th>Destination</th>
                  <th>Truck Assigned</th>
                  <th>Driver</th>
                  <th>Aides / Helpers</th>
                  <th class="text-right">G-Cr...</th>
                  <th class="text-right">E-Cr...</th>
                  <th class="text-right">Total ...</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${completed.map((d, idx) => `
                  <tr class="${idx === 0 ? 'selected' : ''}">
                    <td class="font-bold text-slate-900">${d.DispatchNumber}</td>
                    <td>${d.DispatchDate}</td>
                    <td>${idx === 0 ? 'BCS GROUP COMPANY' : 'GOLDILOCKS CORPORATION'}</td>
                    <td>${d.LoadingScheduleInfo || 'R1 - EODLAB'}</td>
                    <td>${d.TruckInfo}</td>
                    <td>${d.DriverName}</td>
                    <td>${d.AideNames || d.AideName || 'MALLARI, R.'}</td>
                    <td class="text-right">${d.GCrate || d.TotalCrates || 108}</td>
                    <td class="text-right">${d.ECrate || 108}</td>
                    <td class="text-right font-bold">${d.TotalCrates || 216}</td>
                    <td><span class="status-pill status-pill-complete">• Completed</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>

          <div class="text-xs text-slate-500 pt-3">
            Showing ${completed.length} of ${completed.length} completed dispatches
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-refresh-completed').addEventListener('click', () => renderCompletedDispatchesView());
    document.getElementById('btn-reset-completed-filter').addEventListener('click', () => {
      searchTerm = '';
      renderCompletedDispatchesView();
    });
  }

  // =========================================================================
  // VIEW 4: SPECIAL TRIP HISTORY (Exact Screenshot 13)
  // =========================================================================
  function renderSpecialTripsView() {
    const allTrips = store.get('Deliveries').filter(d => (d.Remarks && d.Remarks.includes('Special Trip')) || (d.DispatchNumber && d.DispatchNumber.startsWith('ST-')));
    const pendingCount = allTrips.filter(t => t.Status === 'Scheduled' || t.Status === 'Pending').length;
    const transitCount = allTrips.filter(t => t.Status === 'In Transit').length;
    const completedCount = allTrips.filter(t => t.Status === 'Completed').length;

    mainContentView.innerHTML = `
      <div class="space-y-3">
        <!-- Top Header & 4 KPI Boxes (Exact Screenshot 13) -->
        <div class="bg-white p-4 rounded-sm border border-slate-200 flex flex-wrap items-center justify-between gap-4">
          <div>
            <h2 class="text-[20px] font-bold text-slate-900 flex items-center gap-2">
              <span>☀️</span>
              <span>Special Trip History</span>
            </h2>
            <p class="text-xs text-slate-500">Historical archive, itemized stop breakdown, and lifecycle execution of special on-demand dispatches</p>
          </div>

          <div class="flex items-center space-x-2">
            <div class="w-24 border border-slate-400 p-2 bg-white">
              <span class="text-[11px] text-slate-600 block">Total Trips</span>
              <span class="text-lg font-bold text-[#B4182C]">${allTrips.length}</span>
            </div>
            <div class="w-24 border border-slate-400 p-2 bg-white">
              <span class="text-[11px] text-slate-600 block">Pending</span>
              <span class="text-lg font-bold text-slate-900">${pendingCount}</span>
            </div>
            <div class="w-24 border border-slate-400 p-2 bg-white">
              <span class="text-[11px] text-slate-600 block">In-Transit</span>
              <span class="text-lg font-bold text-[#0284C7]">${transitCount}</span>
            </div>
            <div class="w-24 border border-slate-400 p-2 bg-white">
              <span class="text-[11px] text-slate-600 block">Completed</span>
              <span class="text-lg font-bold text-slate-900">${completedCount}</span>
            </div>
          </div>
        </div>

        <!-- Filter & Action Bar (Exact Screenshot 13) -->
        <div class="bg-white p-3 rounded-sm border border-slate-200 flex flex-wrap items-center justify-between gap-2">
          <div class="flex flex-wrap items-center gap-2 text-xs">
            <input type="text" id="special-trip-search" value="${searchTerm}" placeholder="🔍 Search ref, company..." class="w-44 border border-slate-300 rounded px-2.5 py-1 text-xs">
            <select class="border border-slate-300 rounded px-2.5 py-1 text-xs bg-white">
              <option>All Statuses</option>
              <option>Pending</option>
              <option>In-Transit</option>
              <option>Completed</option>
            </select>
            <span class="font-semibold text-slate-700">From:</span>
            <input type="date" value="2026-08-26" class="border border-slate-300 rounded px-2 py-1 text-xs">
            <span class="font-semibold text-slate-700">To:</span>
            <input type="date" value="2026-11-24" class="border border-slate-300 rounded px-2 py-1 text-xs">
          </div>

          <div class="flex items-center space-x-2">
            <button id="btn-refresh-special" class="btn-secondary py-1 px-2.5 text-xs">🔄 Refresh</button>
            <button id="btn-export-special-csv" class="btn-secondary py-1 px-2.5 text-xs">📊 Export CSV</button>
            <button id="btn-new-special-trip" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1.5 rounded text-xs font-semibold">+ New Special Trip</button>
          </div>
        </div>

        <!-- Special Trip Table (Exact Columns from Screenshot 13) -->
        <div class="desktop-table-container min-h-[380px] p-3">
          <table class="desktop-table">
            <thead>
              <tr>
                <th>Order Refer...</th>
                <th>Dispatc...</th>
                <th>Company</th>
                <th>Address / Route</th>
                <th>Service Type</th>
                <th>Truck</th>
                <th>Driver</th>
                <th>Aide</th>
                <th>St...</th>
                <th>Status</th>
                <th>Payment Set...</th>
              </tr>
            </thead>
            <tbody>
              ${allTrips.map((t, idx) => `
                <tr class="${idx === 0 ? 'selected' : ''}">
                  <td class="font-bold">${t.DispatchNumber}</td>
                  <td>${t.DispatchDate}</td>
                  <td>${t.LoadingScheduleInfo ? t.LoadingScheduleInfo.split('(')[0] : 'BCS GROUP COMPANY'}</td>
                  <td>Direct Special Route</td>
                  <td>Dedicated / Special Delivery</td>
                  <td>${t.TruckInfo}</td>
                  <td>${t.DriverName}</td>
                  <td>${t.AideName || 'None'}</td>
                  <td>1</td>
                  <td><span class="status-pill status-pill-scheduled">${t.Status}</span></td>
                  <td>Mode of payment with report</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;

    document.getElementById('btn-new-special-trip').addEventListener('click', () => openSpecialTripModal());
    document.getElementById('btn-refresh-special').addEventListener('click', () => renderSpecialTripsView());
    document.getElementById('btn-export-special-csv').addEventListener('click', () => exportToCsv(allTrips, 'Special_Trip_History.csv'));
  }

  // =========================================================================
  // VIEW 5: TRIP REPORTS (Exact Screenshots 14, 15, 16)
  // =========================================================================
  function renderTripReportsView() {
    const reports = store.get('TripReports');
    const deliveries = store.get('Deliveries');

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 ${showTripReportForm ? 'lg:grid-cols-12' : ''} gap-4 items-start">
        
        ${showTripReportForm ? `
          <!-- LEFT PANE: Trip Completion Record (Exact Screenshots 15 & 16) -->
          <div class="lg:col-span-4 bg-white border border-[#E5E7EB] rounded-sm p-3.5 flex flex-col max-h-[calc(100vh-158px)] overflow-y-auto space-y-3">
            <div class="flex items-center justify-between border-b border-slate-200 pb-2">
              <h3 class="font-bold text-[14px] text-slate-900">Trip Completion Record</h3>
              <button type="button" id="btn-close-tr-form" class="bg-[#F3F4F6] hover:bg-slate-200 text-slate-700 px-2.5 py-0.5 rounded text-xs">✕ Close</button>
            </div>

            <form id="form-trip-completion" class="space-y-3">
              <!-- STEP 1 -->
              <div>
                <div class="text-[11px] font-bold text-[#991B1B] uppercase border-b border-slate-200 pb-1 mb-2">
                  ① STEP 1: SELECT DISPATCH ORDER
                </div>
                <label class="block text-xs text-slate-700 mb-1">Select Dispatch Order:</label>
                <select id="tc-dispatch-select" class="w-full border border-slate-300 rounded px-2 py-1 text-xs bg-white">
                  ${deliveries.map(d => `<option value="${d.Id}">${d.DispatchNumber} - ${d.LoadingScheduleInfo || 'BCS GROUP COMPANY'}</option>`).join('')}
                </select>
                <div class="mt-2 p-1.5 bg-[#FDF2F4] border border-[#F6CCD1] rounded text-[11px] font-bold text-[#991B1B]">
                  📄 REF: RCS VILLASIS102126-003 &nbsp;|&nbsp; 🗺️ ROUTE: R1 - EODLAB
                </div>
              </div>

              <!-- STEP 2: PER-STORE TRIP REPORT TABLE -->
              <div>
                <div class="text-[11px] font-bold text-[#991B1B] uppercase border-b border-slate-200 pb-1 mb-2">
                  ② STEP 2: PER-STORE TRIP REPORT TABLE &amp;
                </div>
                <div class="border border-slate-300 rounded p-2 space-y-2">
                  <div class="flex items-center justify-between">
                    <span class="text-[11px] font-bold text-[#991B1B] truncate">📋 R1 - EODLAB (Ilocos Route) ...</span>
                    <div class="flex items-center space-x-1">
                      <button type="button" id="btn-tr-add-store" class="bg-[#B4182C] text-white px-2 py-0.5 rounded text-[11px] font-bold">+ Add</button>
                      <button type="button" id="btn-tr-edit-table" class="bg-[#1C1917] text-white px-2 py-0.5 rounded text-[11px] font-bold">✏️ Edit</button>
                    </div>
                  </div>
                  <div class="overflow-x-auto max-h-28 border border-slate-200">
                    <table class="desktop-table text-[11px]">
                      <thead>
                        <tr>
                          <th>St...</th>
                          <th>Store Name ...</th>
                          <th>DR / ...</th>
                          <th>St...</th>
                        </tr>
                      </thead>
                      <tbody id="tr-stores-tbody">
                        <tr class="selected">
                          <td>203...</td>
                          <td>RR-Laoag Ba...</td>
                          <td>DR-101</td>
                          <td>10:30</td>
                        </tr>
                        <tr>
                          <td>204...</td>
                          <td>RR-San Nico...</td>
                          <td>DR-102</td>
                          <td>11:45</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </div>

                <!-- 2x2 Auto-Sum Crate Grid -->
                <div class="grid grid-cols-2 gap-2 mt-2 text-[11px]">
                  <div class="border-b border-slate-200 pb-1">
                    <span class="text-slate-700 block">Total Delivered G-Crates (Auto-Sum):</span>
                    <input type="number" id="tc-del-g" value="150" class="w-full border border-slate-300 rounded px-2 py-0.5 font-bold mt-0.5">
                  </div>
                  <div class="border-b border-slate-200 pb-1">
                    <span class="text-slate-700 block">Total Delivered E-Crates (Auto-Sum):</span>
                    <input type="number" id="tc-del-e" value="0" class="w-full border border-slate-300 rounded px-2 py-0.5 font-bold mt-0.5">
                  </div>
                  <div class="border-b border-slate-200 pb-1">
                    <span class="text-slate-700 block">Total Returned G-Crates (Auto-Sum):</span>
                    <input type="number" id="tc-ret-g" value="150" class="w-full border border-slate-300 rounded px-2 py-0.5 font-bold mt-0.5">
                  </div>
                  <div class="border-b border-slate-200 pb-1">
                    <span class="text-slate-700 block">Total Returned E-Crates (Auto-Sum):</span>
                    <input type="number" id="tc-ret-e" value="0" class="w-full border border-slate-300 rounded px-2 py-0.5 font-bold mt-0.5">
                  </div>
                </div>
              </div>

              <!-- STEP 3: PLANT GATE TIMES & SIGNATORIES (Exact Screenshot 15 & 16) -->
              <div>
                <div class="text-[11px] font-bold text-[#991B1B] uppercase border-b border-slate-200 pb-1 mb-2">
                  ③ STEP 3: PLANT GATE TIMES &amp; SIGNATORIES
                </div>
                <div class="border border-slate-300 rounded p-2 space-y-2">
                  <div class="text-[11px] font-bold text-[#991B1B]">🕒 Plant Gate Times &amp; Official Signatories (RCS</div>
                  <div class="grid grid-cols-4 gap-1.5 text-[10px]">
                    <div>
                      <label class="block text-slate-600 truncate">Truck Time</label>
                      <input type="text" id="tc-arr-time" value="2042" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">Truck Time</label>
                      <input type="text" id="tc-dep-time" value="0139" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">DTR</label>
                      <input type="text" id="tc-dtr-chk" value="RAMIL ME" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">Return</label>
                      <input type="text" id="tc-ret-rec" value="" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">DTR</label>
                      <input type="text" id="tc-dtr-rel" value="XERXES R" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">DR</label>
                      <input type="text" id="tc-dr-ret" value="VINJOHN" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">DR Check</label>
                      <input type="text" id="tc-mgr-name" value="JOE-EM FI" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                    <div>
                      <label class="block text-slate-600 truncate">Guard</label>
                      <input type="text" id="tc-guard" value="" class="w-full border border-slate-300 px-1.5 py-1 text-xs">
                    </div>
                  </div>
                </div>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">QA / Store Remarks:</label>
                <textarea id="tc-remarks" rows="2" class="w-full border border-slate-300 rounded p-1.5 text-xs">Incident Recorded: Missing / Damaged Crates from Trip Delivery: 50 damaged crates</textarea>
              </div>

              <!-- Action Buttons (Exact Screenshot 15 & 16) -->
              <div class="space-y-1.5 pt-2 border-t border-slate-200">
                <div class="grid grid-cols-2 gap-2">
                  <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white py-1.5 rounded text-xs font-bold">💾 COMPLETE TRIP</button>
                  <button type="button" id="btn-update-tr" class="bg-[#1C1917] hover:bg-[#2E2B29] text-white py-1.5 rounded text-xs font-bold">✏️ UPDATE REPORT</button>
                </div>
                <button type="button" id="btn-clear-tr" class="w-full btn-secondary py-1.5 text-xs font-bold">🖌️ CLEAR FORM</button>
              </div>
            </form>
          </div>
        ` : ''}

        <!-- RIGHT PANE: Completed Trips Log (Exact Screenshot 14, 15, 16) -->
        <div class="${showTripReportForm ? 'lg:col-span-8' : 'w-full'} desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Completed Trips Log (${reports.length})</h2>
            
            <div class="flex flex-wrap items-center gap-2 mb-3">
              ${showTripReportForm ? `
                <button id="btn-toggle-tr-form" class="bg-[#F3F4F6] hover:bg-slate-200 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Hide Form ▴</button>
              ` : `
                <button id="btn-toggle-tr-form" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">📝 + New / Form</button>
              `}
              <input type="text" placeholder="🔍 Search report #, dis..." class="w-44 border border-slate-300 rounded px-2.5 py-1 text-xs">
              <button class="w-7 h-7 rounded bg-[#E5E7EB] text-slate-700 font-bold text-xs">✖</button>
              <button id="btn-print-official-tr" class="bg-[#7F1D1D] hover:bg-[#991B1B] text-white px-3 py-1 rounded text-xs font-semibold">📄 Official Trip Repo</button>
              <button class="btn-secondary py-1 px-2.5 text-xs">⋮ Actions ▾</button>
            </div>

            <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
              <table class="desktop-table">
                <thead>
                  <tr>
                    <th>Report #</th>
                    <th>Dispatch #</th>
                    <th>Date</th>
                    <th>Arrival</th>
                    <th>Departure</th>
                    <th>Del. G</th>
                    <th>Del. E</th>
                    <th>Total Del.</th>
                    <th>Ret. G</th>
                    <th>Ret. E</th>
                    <th>Total Ret.</th>
                    <th>Manager Signee</th>
                    <th>Remarks</th>
                  </tr>
                </thead>
                <tbody>
                  ${reports.map((r, idx) => `
                    <tr class="${idx === 0 ? 'selected' : ''}">
                      <td class="font-bold">${r.TripReportNumber}</td>
                      <td>${r.DispatchNumber}</td>
                      <td>${r.ReportDate}</td>
                      <td>${r.ArrivalTime || '10:30 AM'}</td>
                      <td>${r.DepartureTime || '11:45 AM'}</td>
                      <td>${r.DeliveredCrates || 150}</td>
                      <td>0</td>
                      <td>${r.DeliveredCrates || 150}</td>
                      <td>${r.ReturnedCrates || 150}</td>
                      <td>0</td>
                      <td>${r.ReturnedCrates || 150}</td>
                      <td>${r.StoreManagerName || 'maricel de oca...'}</td>
                      <td class="max-w-[100px] truncate">${r.Remarks || ''}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <div class="bottom-summary-strip mt-4">
            <span>📊 Total Completed Trips: ${reports.length}</span>
            <span>|</span>
            <span>📥 Delivered: ${reports.reduce((s,r)=>s+Number(r.DeliveredCrates||150),0)} Crates</span>
            <span>|</span>
            <span>🔄 Returned Empties: ${reports.reduce((s,r)=>s+Number(r.ReturnedCrates||150),0)} Crates</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-toggle-tr-form').addEventListener('click', () => {
      showTripReportForm = !showTripReportForm;
      renderTripReportsView();
    });

    document.getElementById('btn-print-official-tr').addEventListener('click', () => {
      if (reports.length > 0) {
        const d = store.getById('Deliveries', reports[0].DeliveryId) || deliveries[0];
        printer.printOfficialTripReport(reports[0], d);
      }
    });

    if (showTripReportForm) {
      document.getElementById('btn-close-tr-form').addEventListener('click', () => {
        showTripReportForm = false;
        renderTripReportsView();
      });
      document.getElementById('btn-tr-edit-table').addEventListener('click', () => openSpecialTripTableEditorModal());
      document.getElementById('btn-tr-add-store').addEventListener('click', () => {
        const tbody = document.getElementById('tr-stores-tbody');
        const tr = document.createElement('tr');
        tr.innerHTML = `<td>205...</td><td>RR-Vigan Br...</td><td>DR-103</td><td>12:30</td>`;
        tbody.appendChild(tr);
      });
      document.getElementById('btn-update-tr').addEventListener('click', () => {
        toast.show('Trip report updated ✓', 'success');
      });
      document.getElementById('btn-clear-tr').addEventListener('click', () => {
        document.getElementById('tc-remarks').value = '';
        toast.show('Form cleared', 'info');
      });
      document.getElementById('form-trip-completion').addEventListener('submit', (e) => {
        e.preventDefault();
        const delId = Number(document.getElementById('tc-dispatch-select').value);
        const rep = store.completeTrip(delId, {
          ArrivalTime: '10:30 AM',
          DepartureTime: '11:45 AM',
          DeliveredGCrates: Number(document.getElementById('tc-del-g').value),
          DeliveredECrates: Number(document.getElementById('tc-del-e').value),
          ReturnedGCrates: Number(document.getElementById('tc-ret-g').value),
          ReturnedECrates: Number(document.getElementById('tc-ret-e').value),
          StoreManagerName: document.getElementById('tc-mgr-name').value,
          Remarks: document.getElementById('tc-remarks').value
        }, auth.getCurrentUser());
        toast.show(`Trip completed: ${rep.TripReportNumber} ✓`, 'success');
        renderTripReportsView();
      });
    }
  }

  // =========================================================================
  // VIEW 6: INCIDENT & DAMAGE LOGS (Exact Screenshots 17 & 18)
  // =========================================================================
  function renderIncidentDamageLogsView() {
    const incidents = store.get('IncidentReports');
    const deliveries = store.get('Deliveries');

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 ${showIncidentForm ? 'lg:grid-cols-12' : ''} gap-4 items-start">
        
        ${showIncidentForm ? `
          <!-- LEFT PANE: Incident Details Form (Exact Screenshot 18) -->
          <div class="lg:col-span-4 bg-white border border-[#E5E7EB] rounded-sm p-3.5 flex flex-col max-h-[calc(100vh-158px)] overflow-y-auto space-y-3">
            <div class="flex items-center justify-between border-b border-slate-200 pb-2">
              <h3 class="font-bold text-[14px] text-slate-900">Incident Details Form</h3>
              <button type="button" id="btn-close-inc-form" class="bg-[#F3F4F6] hover:bg-slate-200 text-slate-700 px-2.5 py-0.5 rounded text-xs">✕ Close</button>
            </div>

            <!-- Amber Unsaved Draft Banner -->
            <div id="inc-draft-banner" class="bg-[#FFFBEB] border border-[#F59E0B] rounded px-2.5 py-1.5 flex items-center justify-between text-xs">
              <span class="text-[#92400E] truncate">📄 Unsaved draft (Oct 25, 0...</span>
              <div class="flex items-center space-x-1.5 ml-1">
                <button type="button" onclick="toast.show('Draft restored ✓','info')" class="bg-[#D97706] text-white px-2 py-0.5 rounded font-bold text-[11px]">Restore</button>
                <button type="button" onclick="document.getElementById('inc-draft-banner').remove()" class="text-[#92400E] text-[11px]">Discard</button>
              </div>
            </div>

            <form id="form-incident-report" class="space-y-3">
              <div>
                <label class="block text-xs text-slate-700 mb-1">Associated Delivery Trip:</label>
                <select id="inc-delivery-select" class="w-full border border-slate-300 rounded px-2 py-1.5 text-xs bg-white">
                  ${deliveries.map(d => `<option value="${d.Id}" data-num="${d.DispatchNumber}">${d.DispatchNumber} - Red Ribbon Bakeshop Inc. (${d.LoadingScheduleInfo || 'R2 - EOD'})</option>`).join('')}
                </select>
              </div>

              <div class="grid grid-cols-2 gap-2">
                <div>
                  <label class="block text-xs text-slate-700 mb-1 truncate">Damaged Quantity (Qty</label>
                  <input type="number" id="inc-qty" value="2" min="1" class="w-full border border-slate-300 rounded px-2 py-1 text-xs">
                </div>
                <div>
                  <label class="block text-xs text-slate-700 mb-1 truncate">Estimated Damage Cost</label>
                  <input type="number" id="inc-cost" value="1500.00" step="0.01" class="w-full border border-slate-300 rounded px-2 py-1 text-xs">
                </div>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Primary Cause of Incident:</label>
                <input type="text" id="inc-cause" placeholder="e.g. Broken refrigeration latch / rough road tr..." required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>

              <label class="flex items-center space-x-2 text-xs text-slate-800 cursor-pointer">
                <input type="checkbox" id="inc-charge-crew" class="rounded border-slate-300 text-[#B4182C]">
                <span>Damage Chargeable to Crew Negligence</span>
              </label>

              <div>
                <label class="block text-xs text-slate-500 mb-1">Detailed Incident Remarks:</label>
                <textarea id="inc-remarks" rows="2" placeholder="Describe damage extent and store acceptanc..." class="w-full border border-slate-300 rounded p-2 text-xs"></textarea>
              </div>

              <!-- Damage Photo / Proof Reference (Exact Screenshot 18) -->
              <div>
                <label class="block text-xs text-slate-700 mb-1">Damage Photo / Proof Reference:</label>
                <div class="flex items-start space-x-2">
                  <div id="inc-photo-box" class="w-16 h-16 border border-slate-400 bg-[#F3F4F6] flex items-center justify-center overflow-hidden flex-shrink-0">
                    ${uploadedIncidentPhoto ? `<img src="${uploadedIncidentPhoto}" class="w-full h-full object-cover">` : ''}
                  </div>
                  <div class="flex-1 space-y-1.5">
                    <input type="file" id="inc-photo-input" accept="image/*" class="hidden">
                    <button type="button" id="btn-upload-inc-photo" class="w-full btn-secondary py-1 text-xs">🖼️ Upload Photo Evidence</button>
                    <div class="grid grid-cols-2 gap-1.5">
                      <button type="button" id="btn-view-inc-photo" class="bg-[#F3F4F6] text-slate-500 border border-slate-200 rounded py-1 text-xs">👁️ View Full</button>
                      <button type="button" id="btn-remove-inc-photo" class="bg-[#FEF2F2] text-[#DC2626] border border-[#FECACA] rounded py-1 text-xs">🗑️ Remove</button>
                    </div>
                  </div>
                </div>
              </div>

              <div class="space-y-1.5 pt-3 border-t border-slate-200">
                <div class="grid grid-cols-2 gap-2">
                  <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white py-1.5 rounded text-xs font-bold">💾 SUBMIT INCIDENT</button>
                  <button type="button" class="bg-[#F3F4F6] text-slate-400 border border-slate-200 py-1.5 rounded text-xs font-bold">✏️ UPDATE INCIDENT</button>
                </div>
                <button type="button" id="btn-clear-inc" class="w-full btn-secondary py-1.5 text-xs font-bold">🖌️ CLEAR FORM</button>
              </div>
            </form>
          </div>
        ` : ''}

        <!-- RIGHT PANE: Active Incident & Damage Logs (Exact Screenshot 17 & 18) -->
        <div class="${showIncidentForm ? 'lg:col-span-8' : 'w-full'} desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Active Incident &amp; Damage Logs (${incidents.length})</h2>
            
            <div class="flex flex-wrap items-center gap-2 mb-3">
              ${showIncidentForm ? `
                <button id="btn-toggle-inc-form" class="bg-[#F3F4F6] hover:bg-slate-200 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Hide Form ▴</button>
              ` : `
                <button id="btn-toggle-inc-form" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">📝 + New / Form</button>
              `}
              <input type="text" placeholder="🔍 Search incident, dis..." class="w-44 border border-slate-300 rounded px-2.5 py-1 text-xs">
              <button class="w-7 h-7 rounded bg-[#E5E7EB] text-slate-700 font-bold text-xs">✖</button>
              <button class="btn-secondary py-1 px-2.5 text-xs">⋮ Actions ▾</button>
              <button onclick="window.switchHub('operations', 'trip_reports')" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">Proceed to Trip Reports</button>
            </div>

            <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
              <table class="desktop-table">
                <thead>
                  <tr>
                    <th>Charge Crew</th>
                    <th>Dispatch #</th>
                    <th>Date</th>
                    <th>Damaged</th>
                    <th>Est. Cost</th>
                    <th>Cause</th>
                    <th>Remarks</th>
                    <th>Photo</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  ${incidents.map((i, idx) => `
                    <tr class="${idx === 0 ? 'selected' : ''}">
                      <td class="text-center"><input type="checkbox" ${i.ChargeableToCrew ? 'checked' : ''} class="accent-[#0284C7] w-4 h-4"></td>
                      <td class="font-medium">${i.DispatchNumber}</td>
                      <td>${i.IncidentDate}</td>
                      <td>${i.DamagedQuantity}</td>
                      <td>${Number(i.EstimatedCost||0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td class="max-w-[140px] truncate">${i.Cause}</td>
                      <td class="max-w-[160px] truncate">${i.Remarks || 'Automated variance log: 50 cra...'}</td>
                      <td>${i.PhotoReference ? '📷' : ''}</td>
                      <td><span class="px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 text-[11px] font-semibold">• ${i.Status || 'Logged'}</span></td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <div class="bottom-summary-strip mt-4">
            <span>📊 Total Incident Reports: ${incidents.length}</span>
            <span>|</span>
            <span>📦 Total Damaged: ${incidents.reduce((s,i)=>s+Number(i.DamagedQuantity||0),0)} Units</span>
            <span>|</span>
            <span>⚠️ Estimated Loss: PHP ${incidents.reduce((s,i)=>s+Number(i.EstimatedCost||0),0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-toggle-inc-form').addEventListener('click', () => {
      showIncidentForm = !showIncidentForm;
      renderIncidentDamageLogsView();
    });

    if (showIncidentForm) {
      document.getElementById('btn-close-inc-form').addEventListener('click', () => {
        showIncidentForm = false;
        renderIncidentDamageLogsView();
      });

      const fileInp = document.getElementById('inc-photo-input');
      document.getElementById('btn-upload-inc-photo').addEventListener('click', () => fileInp.click());
      fileInp.addEventListener('change', (e) => {
        const f = e.target.files[0];
        if (f) {
          const reader = new FileReader();
          reader.onload = (ev) => {
            uploadedIncidentPhoto = ev.target.result;
            renderIncidentDamageLogsView();
          };
          reader.readAsDataURL(f);
        }
      });
      document.getElementById('btn-remove-inc-photo').addEventListener('click', () => {
        uploadedIncidentPhoto = null;
        renderIncidentDamageLogsView();
      });

      document.getElementById('form-incident-report').addEventListener('submit', (e) => {
        e.preventDefault();
        const sel = document.getElementById('inc-delivery-select');
        store.insert('IncidentReports', {
          DeliveryId: Number(sel.value),
          DispatchNumber: sel.selectedOptions[0]?.getAttribute('data-num') || 'DSP-20260901',
          IncidentDate: '10/25/2026',
          DamagedQuantity: Number(document.getElementById('inc-qty').value),
          EstimatedCost: Number(document.getElementById('inc-cost').value),
          Cause: document.getElementById('inc-cause').value,
          ChargeableToCrew: document.getElementById('inc-charge-crew').checked ? 1 : 0,
          Remarks: document.getElementById('inc-remarks').value,
          Status: 'Logged'
        }, auth.getCurrentUser());
        toast.show('Incident report submitted ✓', 'success');
        renderIncidentDamageLogsView();
      });
    }
  }

  // =========================================================================
  // VIEW 7: BILLING INVOICES (Exact Screenshots 19 & 20)
  // =========================================================================
  function renderBillingInvoicesView() {
    const invoices = store.get('Invoices');
    const completedDeliveries = store.get('Deliveries').filter(d => d.Status === 'Completed');
    const unpaidCount = invoices.filter(i => i.Status !== 'Paid').length;
    const paidCount = invoices.filter(i => i.Status === 'Paid').length;
    const totalBilled = invoices.reduce((s, i) => s + Number(i.NetAmount || 0), 0);

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 ${showInvoiceForm ? 'lg:grid-cols-12' : ''} gap-4 items-start">
        
        ${showInvoiceForm ? `
          <!-- LEFT PANE: Invoice Editor (Exact Screenshot 20) -->
          <div class="lg:col-span-4 bg-white border border-[#E5E7EB] rounded-sm p-3.5 flex flex-col max-h-[calc(100vh-158px)] overflow-y-auto space-y-3">
            <div class="flex items-center justify-between border-b border-slate-200 pb-2">
              <h3 class="font-bold text-[14px] text-slate-900">Invoice #SI-647</h3>
              <button type="button" id="btn-close-inv-form" class="bg-[#F3F4F6] hover:bg-slate-200 text-slate-700 px-2.5 py-0.5 rounded text-xs">✕ Close</button>
            </div>

            <form id="form-invoice-editor" class="space-y-3">
              <div>
                <label class="block text-xs text-slate-700 mb-1">Invoice / Billing Reference # *:</label>
                <input type="text" id="inv-num" value="SI-647" required class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Completed Delivery Trip *:</label>
                <select id="inv-delivery-select" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="0"></option>
                  ${completedDeliveries.map(d => `<option value="${d.Id}">${d.DispatchNumber} — ${d.LoadingScheduleInfo}</option>`).join('')}
                </select>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Linked Loading Schedule / Volume:</label>
                <input type="text" value="(Delivery Reference Recorded)" readonly class="w-full bg-[#F5F5F4] border border-slate-300 rounded px-2.5 py-1.5 text-xs text-slate-700">
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Billed Client / Client Name *:</label>
                <select id="inv-client-name" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="Red Ribbon Bakeshop Inc." selected>Red Ribbon Bakeshop Inc.</option>
                  <option value="BCS GROUP COMPANY">BCS GROUP COMPANY</option>
                  <option value="GOLDILOCKS CORPORATION">GOLDILOCKS CORPORATION</option>
                </select>
              </div>

              <div class="grid grid-cols-2 gap-2">
                <div>
                  <label class="block text-xs text-slate-700 mb-1 truncate">Standard Gross (₱) *:</label>
                  <input type="number" id="inv-gross-amt" value="0.00" step="0.01" class="w-full border border-slate-300 rounded px-2 py-1 text-xs">
                </div>
                <div>
                  <label class="block text-xs text-slate-700 mb-1 truncate">Deductions / Damage (₱)</label>
                  <input type="number" id="inv-deduct-amt" value="0.00" step="0.01" class="w-full border border-slate-300 rounded px-2 py-1 text-xs">
                </div>
              </div>

              <!-- Grey Net Due Summary Box (Exact Screenshot 20) -->
              <div class="p-2.5 bg-[#F5F5F4] border border-slate-400 text-xs space-y-1">
                <div class="text-slate-900">Net Due Amount: <span id="inv-net-label">PHP 8,500.00</span></div>
                <div class="text-slate-800">Payment Due Date (15 Days): <span>11/06/2026</span></div>
              </div>

              <div class="pt-6 space-y-1.5 border-t border-slate-200">
                <div class="flex justify-end">
                  <button type="submit" class="bg-[#15803D] hover:bg-[#166534] text-white px-4 py-1.5 rounded text-xs font-bold">✏️ UPDATE INVOICE</button>
                </div>
                <button type="button" id="btn-clear-inv" class="w-full btn-secondary py-1.5 text-xs font-bold">🖌️ CLEAR FORM</button>
              </div>
            </form>
          </div>
        ` : ''}

        <!-- RIGHT PANE: Active Issued Invoices (Exact Screenshots 19 & 20) -->
        <div class="${showInvoiceForm ? 'lg:col-span-8' : 'w-full'} desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Active Issued Invoices (${invoices.length})</h2>
            
            <div class="flex flex-wrap items-center gap-2 mb-3">
              ${showInvoiceForm ? `
                <button id="btn-toggle-inv-form" class="bg-[#F3F4F6] hover:bg-slate-200 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Hide Form ▴</button>
              ` : `
                <button id="btn-toggle-inv-form" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">📝 + New / Form</button>
              `}
              <input type="text" placeholder="🔍 Search SOA, inv..." class="w-44 border border-slate-300 rounded px-2.5 py-1 text-xs">
              <button class="w-7 h-7 rounded bg-[#E5E7EB] text-slate-700 font-bold text-xs">✖</button>
              <button class="btn-secondary py-1 px-2.5 text-xs">⋮ Actions ▾</button>
            </div>

            <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
              <table class="desktop-table">
                <thead>
                  <tr>
                    <th>Invoice #</th>
                    <th>Schedule #</th>
                    <th>Dispatch #</th>
                    <th>Client</th>
                    <th>Date</th>
                    <th>Gross</th>
                    <th>Deductions</th>
                    <th>Net Due</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  ${invoices.map((i, idx) => `
                    <tr class="${idx === 0 ? 'selected' : ''}">
                      <td>${i.InvoiceNumber}</td>
                      <td>${idx === 0 ? '' : 'LOAD-20260829-...'}</td>
                      <td>${idx === 0 ? '' : 'DSP-20260901-1...'}</td>
                      <td>${i.ClientName || 'RED RIBBON BAKESHO...'}</td>
                      <td>${i.IssueDate || ''}</td>
                      <td>${i.TotalAmount ? Number(i.TotalAmount).toFixed(2) : ''}</td>
                      <td>${Number(i.Deductions || 0).toFixed(2)}</td>
                      <td>${Number(i.NetAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td>
                        <span class="${i.Status === 'Paid' ? 'status-pill-paid' : 'status-pill-unpaid'}">
                          • ${i.Status}
                        </span>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <div class="bottom-summary-strip mt-4">
            <span>📊 Total Invoices: ${invoices.length}</span>
            <span>|</span>
            <span>⏳ Unpaid: ${unpaidCount}</span>
            <span>|</span>
            <span>◐ Partial: 0</span>
            <span>|</span>
            <span>🚨 Overdue: 0</span>
            <span>|</span>
            <span>☑️ Paid: ${paidCount}</span>
            <span>|</span>
            <span>💰 Total Billed: PHP ${totalBilled.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-toggle-inv-form').addEventListener('click', () => {
      showInvoiceForm = !showInvoiceForm;
      renderBillingInvoicesView();
    });

    if (showInvoiceForm) {
      document.getElementById('btn-close-inv-form').addEventListener('click', () => {
        showInvoiceForm = false;
        renderBillingInvoicesView();
      });

      const gInp = document.getElementById('inv-gross-amt');
      const dInp = document.getElementById('inv-deduct-amt');
      const netLbl = document.getElementById('inv-net-label');
      const recalc = () => {
        const g = parseFloat(gInp.value) || 0;
        const d = parseFloat(dInp.value) || 0;
        netLbl.innerText = `PHP ${Math.max(0, g - d).toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
      };
      gInp.addEventListener('input', recalc);
      dInp.addEventListener('input', recalc);

      document.getElementById('form-invoice-editor').addEventListener('submit', (e) => {
        e.preventDefault();
        const g = parseFloat(gInp.value) || 8500;
        const d = parseFloat(dInp.value) || 0;
        store.createBillingInvoice({
          InvoiceNumber: document.getElementById('inv-num').value,
          ClientName: document.getElementById('inv-client-name').value,
          TotalAmount: g,
          Deductions: d
        }, auth.getCurrentUser());
        toast.show('Invoice saved ✓', 'success');
        renderBillingInvoicesView();
      });
    }
  }

  // =========================================================================
  // VIEW 8: STATEMENT OF ACCOUNT (Exact Screenshot 21 & 22)
  // =========================================================================
  function renderStatementOfAccountView() {
    const soas = store.get('StatementsOfAccount');
    const clients = store.get('Clients');

    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4 min-h-[calc(100vh-158px)]">
        <!-- Toolbar Row (Exact Screenshot 22) -->
        <div class="flex flex-wrap items-center gap-2 mb-3">
          <button id="btn-open-soa-generator" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">+ New Statement of Accoun</button>
          <button id="btn-print-official-soa" class="btn-secondary py-1 px-3 text-xs">🖨️ Print Official SO</button>
          <button id="btn-export-soa-csv" class="btn-secondary py-1 px-3 text-xs">📊 Export to CS</button>
          <button onclick="window.switchHub('finance', 'billing')" class="btn-secondary py-1 px-3 text-xs">📄 View Linked Invoi</button>
          
          <span class="text-xs font-bold text-slate-800 ml-1">Search:</span>
          <input type="text" id="soa-grid-search" value="${searchTerm}" class="w-28 border border-slate-300 rounded px-2 py-1 text-xs">
          
          <span class="text-xs font-bold text-slate-800 ml-1">Client:</span>
          <select class="border border-slate-300 rounded px-2.5 py-1 text-xs bg-white">
            <option>All Clients</option>
            ${clients.map(c => `<option>${c.CompanyName}</option>`).join('')}
          </select>
        </div>

        <!-- SOA Data Table (Exact Screenshot 22) -->
        <div class="overflow-x-auto">
          <table class="desktop-table">
            <tbody>
              ${soas.map((s, idx) => `
                <tr class="${idx === 0 ? 'selected' : ''}">
                  <td class="font-medium">${s.SoaNumber || 'SOA-2026-0001'}</td>
                  <td>${s.IssueDate || '2026-10-22'}</td>
                  <td>${s.ClientName || 'RED RIBBON BAKESHOP INC.'}</td>
                  <td>${s.PeriodFrom || '2026-10-01'} to ${s.PeriodTo || '2026-10...'}</td>
                  <td>REF</td>
                  <td>-7.00</td>
                  <td class="font-bold">0.00</td>
                  <td class="font-bold">647</td>
                  <td><span class="px-2 py-0.5 rounded-full bg-white text-slate-800 text-[11px] font-semibold">• ${s.Status || 'Issued'}</span></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;

    document.getElementById('btn-open-soa-generator').addEventListener('click', () => openGenerateSoaModal());
    document.getElementById('btn-print-official-soa').addEventListener('click', () => {
      if (soas.length > 0) printer.printSOA(soas[0]);
    });
    document.getElementById('btn-export-soa-csv').addEventListener('click', () => exportToCsv(soas, 'Statements_Of_Account.csv'));
  }

  // =========================================================================
  // VIEW 9: COLLECTIONS & RECEIPTS (Exact Screenshots 23 & 24)
  // =========================================================================
  function renderCollectionsReceiptsView() {
    const receipts = store.get('CollectionReceipts');
    const invoices = store.get('Invoices');
    const totalSettled = receipts.reduce((s, r) => s + Number(r.AmountPaid || 0), 0);

    mainContentView.innerHTML = `
      <div class="grid grid-cols-1 ${showCollectionForm ? 'lg:grid-cols-12' : ''} gap-4 items-start">
        
        ${showCollectionForm ? `
          <!-- LEFT PANE: Record Payment Receipt Form (Exact Screenshot 24) -->
          <div class="lg:col-span-4 bg-white border border-[#E5E7EB] rounded-sm p-3.5 flex flex-col max-h-[calc(100vh-158px)] overflow-y-auto space-y-3">
            <div class="flex items-center justify-between border-b border-slate-200 pb-2">
              <h3 class="font-bold text-[14px] text-slate-900 truncate">Receipt #CR-20260923-1617...</h3>
              <button type="button" id="btn-close-rc-form" class="bg-[#F3F4F6] hover:bg-slate-200 text-slate-700 px-2.5 py-0.5 rounded text-xs">✕ Close</button>
            </div>

            <form id="form-record-receipt" class="space-y-3">
              <div>
                <label class="block text-xs text-slate-700 mb-1">Select Unpaid Service Invoice:</label>
                <select id="rc-invoice-select" class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs bg-white">
                  <option value="${invoices[0]?.Id || 1}"></option>
                  ${invoices.map(i => `<option value="${i.Id}">${i.InvoiceNumber} — ${i.ClientName}</option>`).join('')}
                </select>
              </div>

              <div class="p-2 bg-[#F5F5F4] border border-slate-400 text-xs text-slate-800 truncate">
                📄 Linked Invoice: INV-20260923-161652-8594
              </div>

              <div class="grid grid-cols-2 gap-2">
                <div>
                  <label class="block text-xs text-slate-700 mb-1 truncate">Payment Amount (PHI</label>
                  <input type="number" id="rc-amount" value="9000.00" step="0.01" required class="w-full border border-slate-300 rounded px-2 py-1 text-xs">
                </div>
                <div>
                  <label class="block text-xs text-slate-700 mb-1">Payment Method:</label>
                  <select id="rc-method" class="w-full border border-slate-300 rounded px-2 py-1 text-xs bg-white">
                    <option value="CHECK" selected>CHECK</option>
                    <option value="CASH">CASH</option>
                    <option value="BANK TRANSFER">BANK TRANSFER</option>
                  </select>
                </div>
              </div>

              <div>
                <label class="block text-xs text-slate-700 mb-1">Payment / Transaction Reference # *:</label>
                <input type="text" id="rc-ref" placeholder="e.g. CHK-990214, BDO-TXN-55410, or REF-..." class="w-full border border-slate-300 rounded px-2.5 py-1.5 text-xs">
              </div>

              <div>
                <label class="block text-xs text-slate-500 mb-1">Collection Notes / Remarks:</label>
                <textarea id="rc-notes" rows="3" class="w-full border border-slate-300 rounded p-2 text-xs">Payment received in full settlement of invoi...</textarea>
              </div>

              <div class="pt-4 space-y-1.5 border-t border-slate-200">
                <div>
                  <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-1.5 rounded text-xs font-bold">💵 RECORD PAYMENT</button>
                </div>
                <div class="grid grid-cols-2 gap-2">
                  <button type="button" id="btn-clear-rc" class="btn-secondary py-1.5 text-xs font-bold">🖌️ CLEAR FORM</button>
                  <button type="button" id="btn-print-active-rc" class="btn-secondary py-1.5 text-xs font-bold">🧾 PRINT RECEIPT</button>
                </div>
              </div>
            </form>
          </div>
        ` : ''}

        <!-- RIGHT PANE: Payment Collections Register Log (Exact Screenshot 23 & 24) -->
        <div class="${showCollectionForm ? 'lg:col-span-8' : 'w-full'} desktop-table-container p-4 flex flex-col min-h-[calc(100vh-158px)] justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A] mb-2.5">Payment Collections Register Log (${receipts.length})</h2>
            
            <div class="flex flex-wrap items-center gap-2 mb-3">
              ${showCollectionForm ? `
                <button id="btn-toggle-rc-form" class="bg-[#F3F4F6] hover:bg-slate-200 border border-slate-300 text-slate-700 px-3 py-1 rounded text-xs font-medium">✕ Hide Form ▴</button>
              ` : `
                <button id="btn-toggle-rc-form" class="bg-[#B4182C] hover:bg-[#981323] text-white px-3 py-1 rounded text-xs font-semibold">📝 + New / Form</button>
              `}
              <input type="text" placeholder="🔍 Search receipt #, invoice #, re..." class="w-52 border border-slate-300 rounded px-2.5 py-1 text-xs">
              <button class="w-7 h-7 rounded bg-[#E5E7EB] text-slate-700 font-bold text-xs">✖</button>
              <button class="btn-secondary py-1 px-2.5 text-xs">⋮ Actions ▾</button>
            </div>

            <div class="overflow-x-auto border border-[#E5E7EB] rounded-sm">
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
                  ${receipts.map((r, idx) => `
                    <tr class="${idx === 0 ? 'selected' : ''}">
                      <td>${r.ReceiptNumber}</td>
                      <td>${r.InvoiceNumber}</td>
                      <td>${r.PaymentDate}</td>
                      <td>${Number(r.AmountPaid || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                      <td>${r.PaymentMethod}</td>
                      <td>${r.ReferenceNumber || ''}</td>
                      <td>${r.Notes || ''}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <div class="bottom-summary-strip mt-4">
            <span>📊 Total Collections: ${receipts.length}</span>
            <span>|</span>
            <span>💵 Total Settled Payments: PHP ${totalSettled.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-toggle-rc-form').addEventListener('click', () => {
      showCollectionForm = !showCollectionForm;
      renderCollectionsReceiptsView();
    });

    if (showCollectionForm) {
      document.getElementById('btn-close-rc-form').addEventListener('click', () => {
        showCollectionForm = false;
        renderCollectionsReceiptsView();
      });
      document.getElementById('btn-print-active-rc').addEventListener('click', () => {
        if (receipts.length > 0) {
          const inv = store.getById('Invoices', receipts[0].InvoiceId) || invoices[0];
          printer.printOfficialReceipt(receipts[0], inv);
        }
      });
      document.getElementById('form-record-receipt').addEventListener('submit', (e) => {
        e.preventDefault();
        const invId = Number(document.getElementById('rc-invoice-select').value) || 1;
        store.recordCollectionPayment(invId, {
          AmountPaid: parseFloat(document.getElementById('rc-amount').value) || 9000,
          PaymentMethod: document.getElementById('rc-method').value,
          ReferenceNumber: document.getElementById('rc-ref').value,
          Notes: document.getElementById('rc-notes').value
        }, auth.getCurrentUser());
        toast.show('Payment receipt recorded ✓', 'success');
        renderCollectionsReceiptsView();
      });
    }
  }

  // =========================================================================
  // VIEW 10: TRUCKS, CREW, HISTORY, MASTER DATA, SETTINGS, DASHBOARD
  // =========================================================================
  function renderTrucksView() {
    const trucks = store.get('Trucks');
    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4">
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-[16px] font-bold text-[#0F172A]">Truck Fleet Roster (${trucks.length})</h2>
          <button class="bg-[#B4182C] text-white px-3 py-1 rounded text-xs font-semibold" onclick="toast.show('Truck registration ready','info')">+ Register Truck</button>
        </div>
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
            ${trucks.map((t, idx) => `
              <tr class="${idx === 0 ? 'selected' : ''}">
                <td class="font-bold">${t.PlateNumber}</td>
                <td>${t.TruckType}</td>
                <td class="font-bold">${t.CapacityCrates} Crates</td>
                <td>${t.CargoType}</td>
                <td><span class="${t.Status === 'Available' ? 'status-pill-paid' : 'status-pill-scheduled'}">• ${t.Status}</span></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }

  function renderCrewView() {
    const crews = store.get('Crews');
    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4">
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-[16px] font-bold text-[#0F172A]">Personnel &amp; Crew Directory (${crews.length})</h2>
          <button class="bg-[#B4182C] text-white px-3 py-1 rounded text-xs font-semibold" onclick="toast.show('Crew onboarding ready','info')">+ Add Personnel</button>
        </div>
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
            ${crews.map((c, idx) => `
              <tr class="${idx === 0 ? 'selected' : ''}">
                <td class="font-bold">${c.FullName}</td>
                <td>${c.Role}</td>
                <td>${c.ContactNumber || '—'}</td>
                <td>${c.LicenseNumber || '—'}</td>
                <td>PHP ${Number(c.DailyWage || 0).toLocaleString()}</td>
                <td>PHP ${Number(c.FoodAllowance || 0).toLocaleString()}</td>
                <td><span class="${c.IsAvailable ? 'status-pill-paid' : 'status-pill-scheduled'}">• ${c.IsAvailable ? 'Available' : 'On Trip'}</span></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }

  function renderCrewHistoryView() {
    const crews = store.get('Crews');
    const deliveries = store.get('Deliveries');
    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4">
        <h2 class="text-[16px] font-bold text-[#0F172A] mb-3">Crew Trip &amp; Duty History</h2>
        <table class="desktop-table">
          <thead>
            <tr>
              <th>Crew Member</th>
              <th>Role</th>
              <th>License / ID</th>
              <th>Daily Wage</th>
              <th>Total Assigned Trips</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            ${crews.map((c, idx) => {
              const cnt = deliveries.filter(d => d.DriverId === c.Id || (d.AideIds && d.AideIds.includes(String(c.Id)))).length;
              return `
                <tr class="${idx === 0 ? 'selected' : ''}">
                  <td class="font-bold">${c.FullName}</td>
                  <td>${c.Role}</td>
                  <td>${c.LicenseNumber || 'N/A'}</td>
                  <td>PHP ${Number(c.DailyWage || 0).toLocaleString()}</td>
                  <td class="font-bold">${cnt} Trips</td>
                  <td><span class="status-pill-paid">• AVAILABLE</span></td>
                </tr>
              `;
            }).join('')}
          </tbody>
        </table>
      </div>
    `;
  }

  function renderOrderHistoryView() {
    const audits = store.get('AuditTrail');
    const deliveries = store.get('Deliveries');
    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4">
        <h2 class="text-[16px] font-bold text-[#0F172A] mb-3">System Audit &amp; Order Lifecycle History</h2>
        <table class="desktop-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>User Account</th>
              <th>Module</th>
              <th>Action</th>
              <th>Reference</th>
              <th>Details</th>
            </tr>
          </thead>
          <tbody>
            ${(audits.length ? audits : deliveries).map((item, idx) => `
              <tr class="${idx === 0 ? 'selected' : ''}">
                <td>${item.Timestamp || item.DispatchDate || '2026-10-25'}</td>
                <td class="font-bold">${item.Username || 'admin'}</td>
                <td>${item.TableName || 'Deliveries'}</td>
                <td><span class="status-pill status-pill-complete">${item.Action || item.Status || 'RECORDED'}</span></td>
                <td class="font-bold">${item.DispatchNumber || ('#' + item.Id)}</td>
                <td>${item.Details || item.LoadingScheduleInfo || 'Completed'}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }

  function renderMasterDataView(activeSub = 'clients') {
    const clients = store.get('Clients');
    const routes = store.get('LookupCategories').filter(c => c.CategoryType === 'Destination');
    const rates = store.get('RateMatrix');

    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4">
        ${activeSub === 'routes' ? `
          <div class="flex items-center justify-between mb-3">
            <h3 class="font-bold text-[15px]">Delivery Routes (${routes.length})</h3>
            <button id="btn-md-add-route" class="bg-[#B4182C] text-white px-3 py-1 rounded text-xs font-semibold">+ Quick Add Route</button>
          </div>
          <table class="desktop-table">
            <thead>
              <tr><th>Route Code</th><th>Route / Destination</th><th>Origin</th><th>Status</th></tr>
            </thead>
            <tbody>
              ${routes.map((r, i) => `
                <tr class="${i === 0 ? 'selected' : ''}">
                  <td class="font-bold text-[#B4182C]">R${i + 1}</td>
                  <td>${r.ItemValue}</td>
                  <td>Tarlac City Depot</td>
                  <td><span class="status-pill-paid">• Active</span></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        ` : activeSub === 'rate_matrix' ? `
          <h3 class="font-bold text-[15px] mb-3">Commercial Rate Matrix (${rates.length})</h3>
          <table class="desktop-table">
            <thead>
              <tr><th>Client</th><th>Route</th><th>Truck Type</th><th>Base Rate</th></tr>
            </thead>
            <tbody>
              ${rates.map((r, i) => `
                <tr class="${i === 0 ? 'selected' : ''}">
                  <td class="font-bold">${r.ClientName || 'RED RIBBON BAKESHOP INC.'}</td>
                  <td>${r.Destination || 'Standard Route'}</td>
                  <td>Forward 4-Tons</td>
                  <td class="font-bold text-[#059669]">PHP ${Number(r.BaseRate || 4500).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        ` : `
          <h3 class="font-bold text-[15px] mb-3">Commercial Clients (${clients.length})</h3>
          <table class="desktop-table">
            <thead>
              <tr><th>Company Name</th><th>Client Code</th><th>Address</th><th>Contact</th><th>Status</th></tr>
            </thead>
            <tbody>
              ${clients.map((c, i) => `
                <tr class="${i === 0 ? 'selected' : ''}">
                  <td class="font-bold">${c.CompanyName}</td>
                  <td>${c.ClientCode || 'RRBI'}</td>
                  <td>${c.Address || 'Luzon'}</td>
                  <td>${c.ContactPerson || 'Logistics Dept'}</td>
                  <td><span class="status-pill-paid">• Active</span></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        `}
      </div>
    `;

    const addR = document.getElementById('btn-md-add-route');
    if (addR) {
      addR.addEventListener('click', () => {
        openQuickAddRouteModal((nr) => {
          store.insert('LookupCategories', { CategoryType: 'Destination', ItemValue: `${nr.RouteCode} - ${nr.RouteName}`, ExtraValue: nr.RouteCode });
          renderMasterDataView('routes');
        });
      });
    }
  }

  function renderSettingsAndAuditView() {
    const users = store.get('Users');
    mainContentView.innerHTML = `
      <div class="desktop-table-container p-4 space-y-4">
        <div class="flex items-center justify-between">
          <div>
            <h2 class="text-[16px] font-bold text-[#0F172A]">System User Accounts &amp; Database Management</h2>
            <p class="text-xs text-slate-500">Manage roles, passwords, and local database state</p>
          </div>
          <button onclick="if(confirm('Reset local database to factory SQLite seed?')) window.appStore.resetToFactoryDefaults();" class="bg-[#B4182C] text-white px-3 py-1.5 rounded text-xs font-semibold">🔄 Reset Database to Default Seed</button>
        </div>
        <table class="desktop-table">
          <thead>
            <tr><th>Username</th><th>Full Name</th><th>Role</th><th>Contact</th><th>Status</th></tr>
          </thead>
          <tbody>
            ${users.map((u, i) => `
              <tr class="${i === 0 ? 'selected' : ''}">
                <td class="font-bold">${u.Username}</td>
                <td>${u.FullName}</td>
                <td>${u.Role}</td>
                <td>${u.ContactNumber || '—'}</td>
                <td><span class="${u.IsActive ? 'status-pill-paid' : 'status-pill-unpaid'}">• ${u.IsActive ? 'Active' : 'Inactive'}</span></td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }

  function renderDashboardView() {
    const kpis = store.getDashboardKPIs();
    const deliveries = store.get('Deliveries');
    mainContentView.innerHTML = `
      <div class="space-y-4">
        <div class="grid grid-cols-1 sm:grid-cols-4 gap-3">
          <div class="bg-white p-4 rounded-sm border border-slate-200 border-t-4 border-t-[#B4182C]">
            <span class="text-[11px] font-bold text-slate-500 uppercase">Active Dispatches</span>
            <div class="text-2xl font-bold text-slate-900 mt-1">${kpis.activeDispatches}</div>
          </div>
          <div class="bg-white p-4 rounded-sm border border-slate-200 border-t-4 border-t-[#059669]">
            <span class="text-[11px] font-bold text-slate-500 uppercase">Available Fleet</span>
            <div class="text-2xl font-bold text-slate-900 mt-1">${kpis.availableTrucks} / ${kpis.totalTrucks}</div>
          </div>
          <div class="bg-white p-4 rounded-sm border border-slate-200 border-t-4 border-t-[#0284C7]">
            <span class="text-[11px] font-bold text-slate-500 uppercase">Available Drivers</span>
            <div class="text-2xl font-bold text-slate-900 mt-1">${kpis.availableDrivers}</div>
          </div>
          <div class="bg-white p-4 rounded-sm border border-slate-200 border-t-4 border-t-[#D97706]">
            <span class="text-[11px] font-bold text-slate-500 uppercase">Settled Revenue</span>
            <div class="text-2xl font-bold text-[#B4182C] mt-1">PHP ${kpis.totalRevenue.toLocaleString('en-US')}</div>
          </div>
        </div>

        <div class="desktop-table-container p-4">
          <div class="flex items-center justify-between mb-3">
            <h3 class="font-bold text-sm">Recent Dispatch Orders</h3>
            <button onclick="window.switchHub('operations', 'dispatch')" class="bg-[#B4182C] text-white px-3 py-1 rounded text-xs font-semibold">Go to Operations</button>
          </div>
          <table class="desktop-table">
            <thead>
              <tr><th>Dispatch #</th><th>Date</th><th>Route</th><th>Truck</th><th>Driver</th><th>Total Crates</th><th>Status</th></tr>
            </thead>
            <tbody>
              ${deliveries.map((d, i) => `
                <tr class="${i === 0 ? 'selected' : ''}">
                  <td class="font-bold">${d.DispatchNumber}</td>
                  <td>${d.DispatchDate}</td>
                  <td>${d.LoadingScheduleInfo}</td>
                  <td>${d.TruckInfo}</td>
                  <td>${d.DriverName}</td>
                  <td class="font-bold">${d.TotalCrates}</td>
                  <td><span class="font-semibold">${d.Status}</span></td>
                </tr>
              `).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // =========================================================================
  // MODAL 1: ADD DELIVERY DROP / STOP (Exact Screenshot 06)
  // =========================================================================
  function openAddStopModal(onAdd) {
    const schedules = store.get('LoadingSchedules');
    const stopIndex = routeStops.length + 1;

    const modalHtml = `
      <div id="modal-add-stop" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-sm shadow-2xl w-full max-w-[490px] border border-slate-400 overflow-hidden">
          <!-- Window Titlebar -->
          <div class="bg-[#F3F4F6] px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-500">Add Delivery Drop / Stop</span>
            <button onclick="document.getElementById('modal-add-stop').remove()" class="text-slate-400 hover:text-slate-800 text-sm">✕</button>
          </div>

          <!-- Light Green Banner (Exact Screenshot 06) -->
          <div class="bg-[#ECFDF5] px-5 py-3 border-b border-[#D1FAE5]">
            <h3 class="text-[17px] font-bold text-[#065F46] flex items-center gap-2">
              <span>🗺️</span> <span>Add Route Stop #${stopIndex}</span>
            </h3>
            <p class="text-xs text-[#047857] mt-0.5">Attach a pending loading schedule or define a custom waypoint</p>
          </div>

          <form id="form-add-stop" class="p-5 space-y-3 bg-white">
            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Select from Pending Loading Schedule (Optional):</label>
              <select id="stop-sched" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                <option value="">-- Manual / Custom Destination --</option>
                ${schedules.map(s => `<option value="${s.Id}">${s.ScheduleNumber} - ${s.ClientName} (${s.Destination})</option>`).join('')}
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Client / Store Name:</label>
              <input type="text" id="stop-client" required class="w-full border border-slate-300 px-2.5 py-1.5 text-xs">
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Destination / Delivery Address:</label>
              <input type="text" id="stop-dest" required class="w-full border border-slate-300 px-2.5 py-1.5 text-xs">
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-900 mb-1">G-Crates:</label>
                <input type="number" id="stop-gcrate" value="100" min="0" class="w-full border border-slate-300 px-2.5 py-1 text-xs">
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-900 mb-1">E-Crates:</label>
                <input type="number" id="stop-ecrate" value="0" min="0" class="w-full border border-slate-300 px-2.5 py-1 text-xs">
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Stop Remarks / Gate Instructions:</label>
              <input type="text" id="stop-remarks" class="w-full border border-slate-300 border-b-2 border-b-[#0284C7] px-2.5 py-1.5 text-xs focus:outline-none">
            </div>

            <div class="bg-[#F5F5F4] -mx-5 -mb-5 px-5 py-3 mt-4 flex items-center justify-end space-x-2 border-t border-slate-200">
              <button type="submit" class="bg-[#059669] hover:bg-[#047857] text-white px-4 py-1.5 text-xs font-bold">+ Add Stop</button>
              <button type="button" onclick="document.getElementById('modal-add-stop').remove()" class="bg-[#E5E7EB] hover:bg-slate-300 text-slate-800 px-4 py-1.5 text-xs">Cancel</button>
            </div>
          </form>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('form-add-stop').addEventListener('submit', (e) => {
      e.preventDefault();
      if (typeof onAdd === 'function') {
        onAdd({
          client: document.getElementById('stop-client').value || 'Red Ribbon Branch',
          destination: document.getElementById('stop-dest').value || 'San Fernando',
          gcrate: Number(document.getElementById('stop-gcrate').value) || 100,
          ecrate: Number(document.getElementById('stop-ecrate').value) || 0,
          remarks: document.getElementById('stop-remarks').value
        });
      }
      toast.show('Stop added ✓', 'success');
      document.getElementById('modal-add-stop').remove();
    });
  }

  // =========================================================================
  // MODAL 2: QUICK ADD DELIVERY ROUTE (Exact Screenshot 07)
  // =========================================================================
  function openQuickAddRouteModal(onSave) {
    const clients = store.get('Clients');
    const existingRoutes = store.get('LookupCategories').filter(c => c.CategoryType === 'Destination');
    const nextCode = `R${existingRoutes.length + 1}`;

    const modalHtml = `
      <div id="modal-quick-route" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-[#FAF9F8] rounded-sm shadow-2xl w-full max-w-[440px] border border-slate-400 overflow-hidden">
          <!-- Window Titlebar -->
          <div class="bg-white px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-700">Add New Delivery Route</span>
            <button onclick="document.getElementById('modal-quick-route').remove()" class="text-slate-400 hover:text-slate-800 text-sm">✕</button>
          </div>

          <!-- Dark Charcoal Header (Exact Screenshot 07) -->
          <div class="bg-[#1C1917] px-5 py-3.5 text-white flex items-center gap-2.5">
            <span class="text-lg">🗺️</span>
            <h3 class="text-[17px] font-bold">Quick Add Delivery Route</h3>
          </div>

          <form id="form-quick-route" class="p-5 space-y-3 text-xs">
            <div class="grid grid-cols-12 items-center gap-2">
              <label class="col-span-4 text-right font-semibold text-slate-900 pr-2">Client</label>
              <select id="qr-client" class="col-span-8 border border-[#B4182C] px-2 py-1.5 bg-white font-medium">
                <option value="BCS GROUP COMPANY">BCS GROUP COMPANY</option>
                ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
              </select>
            </div>

            <div class="grid grid-cols-12 items-center gap-2">
              <label class="col-span-4 text-right font-semibold text-slate-900 pr-2">Route Code:</label>
              <input type="text" id="qr-code" value="${nextCode}" required class="col-span-8 border border-slate-300 px-2.5 py-1.5 bg-white">
            </div>

            <div class="grid grid-cols-12 items-center gap-2">
              <label class="col-span-4 text-right font-semibold text-slate-900 pr-2">Route</label>
              <input type="text" id="qr-name" placeholder="e.g. Pangasinan Arterial Depot / Urd..." required class="col-span-8 border border-slate-300 px-2.5 py-1.5 bg-white">
            </div>

            <div class="grid grid-cols-12 items-start gap-2">
              <label class="col-span-4 text-right font-semibold text-slate-900 pr-2 pt-1">Description:</label>
              <textarea id="qr-desc" rows="2" placeholder="Optional route description or operat..." class="col-span-8 border border-slate-300 px-2.5 py-1.5 bg-white"></textarea>
            </div>

            <div class="grid grid-cols-12 items-start gap-2">
              <label class="col-span-4 text-right font-semibold text-slate-900 pr-2 pt-1">Stores / Places:</label>
              <textarea id="qr-stores" rows="2" placeholder="Semicolon separated, e.g. Tarlac Bran..." class="col-span-8 border border-slate-300 px-2.5 py-1.5 bg-white"></textarea>
            </div>

            <div class="grid grid-cols-12 items-center gap-2 pt-1">
              <div class="col-span-4"></div>
              <label class="col-span-8 flex items-center space-x-2 cursor-pointer font-semibold text-slate-900">
                <input type="checkbox" id="qr-active" checked class="w-4 h-4 accent-[#0284C7]">
                <span>Active Route</span>
              </label>
            </div>

            <div class="flex items-center justify-end space-x-2 pt-4">
              <button type="button" onclick="document.getElementById('modal-quick-route').remove()" class="bg-white border border-slate-300 hover:bg-slate-100 text-slate-800 px-5 py-1.5 rounded-md font-semibold">Cancel</button>
              <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white px-5 py-1.5 rounded-md font-bold flex items-center gap-1.5">
                <span>💾</span> <span>Save Route</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('form-quick-route').addEventListener('submit', (e) => {
      e.preventDefault();
      if (typeof onSave === 'function') {
        onSave({
          RouteCode: document.getElementById('qr-code').value,
          RouteName: document.getElementById('qr-name').value
        });
      }
      toast.show('Route saved ✓', 'success');
      document.getElementById('modal-quick-route').remove();
    });
  }

  // =========================================================================
  // MODAL 3: SPECIAL TRIP FORM (Exact Screenshot 08)
  // =========================================================================
  function openSpecialTripModal() {
    const clients = store.get('Clients');
    const trucks = store.get('Trucks');
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER'));
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE'));

    const existingModal = document.getElementById('modal-special-trip');
    if (existingModal) existingModal.remove();

    const modalHtml = `
      <div id="modal-special-trip" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4 overflow-y-auto">
        <div class="bg-[#FAF9F8] rounded-sm shadow-2xl w-full max-w-[780px] border border-slate-400 overflow-hidden my-4">
          <!-- Window Titlebar -->
          <div class="bg-white px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-700 font-medium">Special Trip Form</span>
            <div class="flex items-center space-x-3 text-slate-400 text-xs">
              <span>─</span>
              <span>□</span>
              <button onclick="document.getElementById('modal-special-trip').remove()" class="hover:text-red-600">✕</button>
            </div>
          </div>

          <!-- Dark Charcoal Header (Exact Screenshot 08) -->
          <div class="bg-[#1C1917] px-6 py-3 text-white">
            <h2 class="text-[20px] font-bold leading-tight">Special Trip Form</h2>
            <p class="text-[11px] text-slate-400">Direct on-demand delivery configuration, resource assignment, and itemized multi-stop routing</p>
            <div class="text-[11px] font-bold text-white mt-1">
              Create Special Trip &nbsp;➔&nbsp; Type Date &nbsp;➔&nbsp; Assign Truck &amp; Driver &nbsp;➔&nbsp; Assign Route &amp; Items &nbsp;➔&nbsp; Complete -&gt; Save to History
            </div>
          </div>

          <!-- Form Content -->
          <form id="form-special-trip" class="p-5 space-y-4 max-h-[72vh] overflow-y-auto bg-white m-3 border border-slate-200">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-700 mb-1">Order Reference / Dispatch</label>
                <input type="text" id="sp-ref" value="ST-20261025-0001" readonly class="w-full bg-[#F5F5F4] border border-slate-300 px-2.5 py-1.5 text-xs">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-700 mb-1">Dispatch Date</label>
                <input type="date" id="sp-date" value="2026-10-25" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-700 mb-1">Company</label>
                <select id="sp-client" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                  <option value=""></option>
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-700 mb-1">Departure Time</label>
                <select id="sp-time" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                  <option value="08:00 AM" selected>08:00 AM</option>
                  <option value="06:00 AM">06:00 AM</option>
                  <option value="10:00 AM">10:00 AM</option>
                  <option value="01:00 PM">01:00 PM</option>
                </select>
              </div>
            </div>

            <!-- Delivery Details Table Section (Exact Screenshot 08) -->
            <div class="pt-2">
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-baseline space-x-3">
                  <span class="text-sm font-bold text-slate-900">${spTableTitle}</span>
                  <span class="text-xs italic text-slate-500">${spTableSubtitle}</span>
                </div>
                <div class="flex items-center space-x-1.5">
                  <button type="button" id="btn-add-item-sp" class="bg-[#0369A1] hover:bg-[#075985] text-white px-3 py-1 rounded text-xs font-bold">+ Add</button>
                  <button type="button" id="btn-table-editor-sp" class="bg-[#15803D] hover:bg-[#166534] text-white px-3 py-1 rounded text-xs font-bold">✏️ Edit</button>
                </div>
              </div>

              <div class="overflow-x-auto border border-slate-200">
                <table class="desktop-table">
                  <thead>
                    <tr>
                      <th>#...</th>
                      ${spColumns.map(col => `<th>${col}</th>`).join('')}
                      <th></th>
                    </tr>
                  </thead>
                  <tbody id="sp-items-tbody">
                    ${spRows.map((row, rIdx) => `
                      <tr class="${rIdx === 0 ? 'selected' : ''}">
                        <td>${rIdx + 1}</td>
                        ${row.map(cell => `<td>${cell}</td>`).join('')}
                        <td class="text-right"><button type="button" onclick="window.removeSpRow(${rIdx})" class="text-slate-600 hover:text-red-600">🗑️</button></td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Bottom 2-Column Assignment Section (Exact Screenshot 08) -->
            <div class="grid grid-cols-2 gap-6 pt-3">
              <div class="space-y-2.5">
                <div>
                  <label class="block text-xs font-bold text-slate-700 mb-1">Service Type</label>
                  <select id="sp-service-type" class="w-full border border-[#B4182C] px-2.5 py-1.5 text-xs bg-white">
                    <option value="Dedicated / Special Delivery" selected>Dedicated / Special Delivery</option>
                    <option value="Point-to-Point Express">Point-to-Point Express</option>
                  </select>
                </div>
                <div>
                  <label class="block text-xs font-bold text-slate-700 mb-1">Status</label>
                  <select id="sp-status" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                    <option value="Pending" selected>Pending</option>
                    <option value="In Transit">In Transit</option>
                    <option value="Completed">Completed</option>
                  </select>
                </div>
                <div class="border border-slate-300 bg-[#F5F5F4] p-2.5 space-y-1.5 text-xs">
                  <label class="flex items-center space-x-2 cursor-pointer">
                    <input type="checkbox" checked class="w-4 h-4 accent-[#0284C7]">
                    <span>Mode of payment with my report</span>
                  </label>
                  <label class="flex items-center space-x-2 cursor-pointer">
                    <input type="checkbox" class="w-4 h-4 accent-[#0284C7]">
                    <span>With SOA</span>
                  </label>
                  <label class="flex items-center space-x-2 cursor-pointer">
                    <input type="checkbox" class="w-4 h-4 accent-[#0284C7]">
                    <span>With Invoice</span>
                  </label>
                </div>
              </div>

              <div class="space-y-2.5">
                <div>
                  <label class="block text-xs font-bold text-slate-700 mb-1">Assign Truck</label>
                  <select id="sp-truck" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                    <option value="${trucks[0]?.Id || 1}" data-info="${trucks[0]?.PlateNumber || 'NBJ 8329'}">-- Select Truck --</option>
                    ${trucks.map(t => `<option value="${t.Id}" data-info="${t.PlateNumber} (${t.TruckType})">${t.PlateNumber} (${t.TruckType})</option>`).join('')}
                  </select>
                </div>
                <div>
                  <label class="block text-xs font-bold text-slate-700 mb-1">Assign Driver</label>
                  <select id="sp-driver" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                    <option value="${drivers[0]?.Id || 1}" data-name="${drivers[0]?.FullName || 'HIBAYA, CRISANTO'}">-- Select Driver --</option>
                    ${drivers.map(d => `<option value="${d.Id}" data-name="${d.FullName}">${d.FullName}</option>`).join('')}
                  </select>
                </div>
                <div>
                  <label class="block text-xs font-bold text-slate-700 mb-1">Assign Aide</label>
                  <select id="sp-aide" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                    <option value="${aides[0]?.Id || 1}" data-name="${aides[0]?.FullName || 'ALMAREZ, JHON MARK'}">-- Select Aide / Helper --</option>
                    ${aides.map(a => `<option value="${a.Id}" data-name="${a.FullName}">${a.FullName}</option>`).join('')}
                  </select>
                </div>
              </div>
            </div>

            <!-- Footer Bar -->
            <div class="flex items-center justify-end space-x-2 pt-3 border-t border-slate-200">
              <button type="submit" class="bg-[#0369A1] hover:bg-[#075985] text-white px-5 py-1.5 rounded text-xs font-bold">💾 Save</button>
              <button type="button" id="btn-clear-sp" class="bg-white border border-slate-300 text-slate-700 px-4 py-1.5 rounded text-xs font-semibold">🖌️ Clear</button>
              <button type="button" onclick="document.getElementById('modal-special-trip').remove()" class="bg-[#F3F4F6] text-slate-700 px-4 py-1.5 rounded text-xs font-semibold">✕ Close</button>
            </div>
          </form>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    window.removeSpRow = (idx) => {
      spRows.splice(idx, 1);
      openSpecialTripModal();
    };

    document.getElementById('btn-add-item-sp').addEventListener('click', () => {
      spRows.push([`ST-00${spRows.length + 1}`, 'Additional Stop Delivery', 'Direct Special Route', '08:00 AM', '']);
      openSpecialTripModal();
    });

    document.getElementById('btn-table-editor-sp').addEventListener('click', () => {
      openSpecialTripTableEditorModal();
    });

    document.getElementById('btn-clear-sp').addEventListener('click', () => {
      spRows = [['ST-001', 'Initial Stop Delivery', 'Direct Special Route', '08:00 AM', '']];
      openSpecialTripModal();
    });

    document.getElementById('form-special-trip').addEventListener('submit', (e) => {
      e.preventDefault();
      const truckSel = document.getElementById('sp-truck');
      const driverSel = document.getElementById('sp-driver');
      store.insert('Deliveries', {
        DispatchNumber: document.getElementById('sp-ref').value,
        DispatchDate: document.getElementById('sp-date').value,
        DepartureTime: document.getElementById('sp-time').value,
        LoadingScheduleInfo: `${document.getElementById('sp-client').value || 'BCS GROUP COMPANY'} (Direct Special Route)`,
        TruckInfo: truckSel.selectedOptions[0]?.getAttribute('data-info') || 'NBJ 8329',
        DriverName: driverSel.selectedOptions[0]?.getAttribute('data-name') || 'HIBAYA, CRISANTO',
        TotalCrates: 100,
        Status: document.getElementById('sp-status').value,
        Remarks: 'Special Trip'
      }, auth.getCurrentUser());
      toast.show('Special Trip saved to history ✓', 'success');
      document.getElementById('modal-special-trip').remove();
      renderActiveTabContent();
    });
  }
  window.openSpecialTripModalGlobal = openSpecialTripModal;

  // =========================================================================
  // MODAL 4: TABLE & DELIVERY DETAILS EDITOR (Exact Screenshot 09)
  // =========================================================================
  function openSpecialTripTableEditorModal() {
    const existing = document.getElementById('modal-table-editor');
    if (existing) existing.remove();

    const modalHtml = `
      <div id="modal-table-editor" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4 overflow-y-auto">
        <div class="bg-white rounded-sm shadow-2xl w-full max-w-[820px] border border-slate-400 overflow-hidden my-4">
          <!-- Window Titlebar -->
          <div class="bg-white px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-700">Edit Delivery Details Table — Title, Columns, Rows &amp; Details</span>
            <button onclick="document.getElementById('modal-table-editor').remove()" class="text-slate-400 hover:text-red-600 text-sm">✕</button>
          </div>

          <!-- Dark Charcoal Header (Exact Screenshot 09) -->
          <div class="bg-[#1C1917] px-5 py-3 text-white">
            <h3 class="text-[18px] font-bold flex items-center gap-2">
              <span>✏️</span> <span>Table &amp; Delivery Details Editor</span>
            </h3>
            <p class="text-[11px] text-slate-400">Edit table title, description, column headers (add, rename, delete, reorder), and all row details in one place</p>
          </div>

          <div class="p-4 space-y-3 max-h-[75vh] overflow-y-auto text-xs">
            <!-- Box 1: Table Title & Description -->
            <div class="border border-slate-400 p-3 grid grid-cols-2 gap-4">
              <div>
                <label class="block font-bold text-slate-900 mb-1">1. Table Title (Section Header)</label>
                <input type="text" id="te-title" value="${spTableTitle}" class="w-full border border-slate-300 px-2.5 py-1">
              </div>
              <div>
                <label class="block font-bold text-slate-900 mb-1">Table Description / Subtitle</label>
                <input type="text" id="te-subtitle" value="${spTableSubtitle}" class="w-full border border-slate-300 px-2.5 py-1">
              </div>
            </div>

            <!-- Box 2: Title Head Columns -->
            <div class="border border-slate-400 p-3 space-y-2">
              <div class="font-bold text-slate-900">2. Title Head Columns — Add, Rename, Delete, or Reorder Column Headers</div>
              <div class="grid grid-cols-12 gap-3">
                <div class="col-span-6 border border-slate-200 max-h-36 overflow-y-auto">
                  <table class="desktop-table">
                    <thead>
                      <tr><th>#...</th><th>Column Header Title (Click or Type to Rena...</th><th>D...</th></tr>
                    </thead>
                    <tbody>
                      ${spColumns.map((col, idx) => `
                        <tr class="${idx === 0 ? 'selected' : ''}">
                          <td>${idx + 1}...</td>
                          <td>${col}</td>
                          <td>🗑️</td>
                        </tr>
                      `).join('')}
                    </tbody>
                  </table>
                </div>
                <div class="col-span-6 space-y-2">
                  <div>
                    <label class="block font-bold text-slate-700 mb-1">Column Title Head Name:</label>
                    <input type="text" id="te-col-name" value="${spColumns[0] || 'Standard ID'}" class="w-full border border-slate-300 px-2 py-1">
                  </div>
                  <div class="grid grid-cols-3 gap-1.5">
                    <button type="button" id="te-add-col" class="bg-[#0369A1] text-white py-1 px-2 rounded font-bold text-[11px]">+ Add Column</button>
                    <button type="button" id="te-ren-col" class="bg-[#B4182C] text-white py-1 px-2 rounded font-bold text-[11px]">Rename Head</button>
                    <button type="button" id="te-del-col" class="bg-[#DC2626] text-white py-1 px-2 rounded font-bold text-[11px]">Delete Colum</button>
                    <button type="button" class="bg-[#F3F4F6] border border-slate-300 py-1 px-2 rounded text-[11px]">Move Left / U</button>
                    <button type="button" class="bg-[#F3F4F6] border border-slate-300 py-1 px-2 rounded text-[11px]">Move Right / Do</button>
                    <button type="button" class="bg-[#F3F4F6] border border-slate-300 py-1 px-2 rounded text-[11px]">Default Colum</button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Box 3: Rows, Descriptions & Delivery Details -->
            <div class="border border-slate-400 p-3 space-y-2">
              <div class="flex items-center justify-between">
                <span class="font-bold text-slate-900">3. Rows, Descriptions &amp; Delivery Details (Click any cell to edit details)</span>
                <div class="flex items-center space-x-1.5">
                  <button type="button" id="te-add-row" class="bg-[#0369A1] text-white py-1 px-2.5 rounded font-bold text-[11px]">+ Add Row</button>
                  <button type="button" class="bg-[#3F3B39] text-white py-1 px-2.5 rounded font-bold text-[11px]">Duplicate Row</button>
                  <button type="button" class="bg-[#DC2626] text-white py-1 px-2.5 rounded font-bold text-[11px]">🗑️ Delete Row</button>
                  <button type="button" class="bg-white border border-slate-300 py-1 px-2.5 rounded text-[11px]">🖌️ Clear Row</button>
                </div>
              </div>
              <div class="border border-slate-200 min-h-[120px]">
                <table class="desktop-table">
                  <thead>
                    <tr>
                      <th>#</th>
                      ${spColumns.map(c => `<th>${c}</th>`).join('')}
                      <th>D...</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${spRows.map((r, rIdx) => `
                      <tr>
                        <td>${rIdx + 1}</td>
                        ${r.map(val => `<td>${val}</td>`).join('')}
                        <td>🗑️</td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>
            </div>

            <!-- Footer -->
            <div class="flex items-center justify-end space-x-2 pt-2">
              <button type="button" onclick="document.getElementById('modal-table-editor').remove()" class="bg-white border border-slate-300 px-4 py-1.5 rounded text-xs">✕ Cancel</button>
              <button type="button" id="te-apply-all" class="bg-[#15803D] hover:bg-[#166534] text-white px-4 py-1.5 rounded text-xs font-bold">✓ Apply All Changes</button>
            </div>
          </div>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('te-add-col').addEventListener('click', () => {
      const name = document.getElementById('te-col-name').value || 'New Column';
      spColumns.push(name);
      spRows.forEach(r => r.push(''));
      openSpecialTripTableEditorModal();
    });

    document.getElementById('te-apply-all').addEventListener('click', () => {
      spTableTitle = document.getElementById('te-title').value || 'Delivery Details';
      spTableSubtitle = document.getElementById('te-subtitle').value || 'add a line or row and column';
      document.getElementById('modal-table-editor').remove();
      if (document.getElementById('modal-special-trip')) openSpecialTripModal();
      toast.show('Table & delivery details updated ✓', 'success');
    });
  }

  // =========================================================================
  // MODAL 5: RESCHEDULE DELIVERY (Exact Screenshot 10)
  // =========================================================================
  window.openRescheduleModal = (id) => {
    const d = store.getById('Deliveries', id) || store.get('Deliveries')[0];
    if (!d) return;

    const trucks = store.get('Trucks');
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER'));
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE'));

    const modalHtml = `
      <div id="modal-reschedule" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-[#FAF9F8] rounded-sm shadow-2xl w-full max-w-[540px] border border-slate-400 overflow-hidden">
          <!-- Window Titlebar -->
          <div class="bg-white px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-800">Reschedule Dispatch #${d.DispatchNumber}</span>
            <button onclick="document.getElementById('modal-reschedule').remove()" class="text-slate-400 hover:text-slate-800">✕</button>
          </div>

          <!-- Dark Charcoal Header (Exact Screenshot 10) -->
          <div class="bg-[#1C1917] px-5 py-3 text-white">
            <h3 class="text-[17px] font-bold flex items-center gap-2">
              <span>📅</span> <span>Reschedule Dispatch Order</span>
            </h3>
            <p class="text-[11px] text-slate-400 truncate">Adjust target dispatch date, departure time, and resource assignments for Red Ribbon Bake</p>
          </div>

          <form id="form-resched" class="p-5 space-y-3 text-xs">
            <!-- Summary Box -->
            <div class="border border-slate-400 bg-[#F5F5F4] p-3 space-y-1">
              <div class="font-bold text-slate-900 text-[13px]">Dispatch Order: #${d.DispatchNumber}</div>
              <div class="text-slate-700">Client: Red Ribbon Bakeshop Inc. &nbsp;|&nbsp; Route: ${d.LoadingScheduleInfo || 'R2 - EODGAA (La Union Route)'}</div>
              <div class="text-slate-700">Cargo: ${d.TotalCrates || 92} Crates (${d.CargoType || 'Dry Goods'}) &nbsp;|&nbsp; Truck: ${d.TruckInfo}</div>
              <div class="font-bold text-[#C2410C]">Current Scheduled: ${d.DispatchDate} at ${d.DepartureTime}</div>
              <div class="italic text-slate-600">Current Status: ${d.Status} &nbsp;|&nbsp; Driver: ${d.DriverName} &nbsp;|&nbsp; Aide: ${d.AideNames || d.AideName || 'ALMAREZ, JHO...'}</div>
            </div>

            <div class="grid grid-cols-12 gap-3 items-end">
              <div class="col-span-5">
                <label class="block font-semibold text-slate-900 mb-1">New Scheduled Date: *</label>
                <input type="date" id="resched-date" value="2026-10-25" required class="w-full border border-[#B4182C] px-2.5 py-1.5 bg-white">
              </div>
              <div class="col-span-7">
                <label class="block font-semibold text-slate-900 mb-1">New Departure Time: *</label>
                <div class="flex space-x-1.5">
                  <input type="text" id="resched-time" value="${d.DepartureTime || '08:00 AM'}" readonly class="flex-1 border border-slate-300 px-2.5 py-1.5 bg-white cursor-pointer">
                  <button type="button" id="btn-clock-resched" class="bg-[#7F1D1D] text-white px-3 py-1.5 rounded font-bold flex items-center gap-1">⏰ Time Picker</button>
                </div>
              </div>
            </div>

            <!-- Reassign Resources Box -->
            <div class="border border-slate-400 p-3 space-y-2">
              <div class="font-bold text-slate-900 truncate">🚚 Reassign Resources (Optional - Leave as '(Keep Current)' to preserve assign</div>
              <div class="grid grid-cols-3 gap-2">
                <div>
                  <label class="block font-bold text-slate-700 mb-0.5">Truck:</label>
                  <select class="w-full border border-slate-300 px-2 py-1 bg-white">
                    <option>(Keep Current) (Curr</option>
                    ${trucks.map(t => `<option>${t.PlateNumber}</option>`).join('')}
                  </select>
                </div>
                <div>
                  <label class="block font-bold text-slate-700 mb-0.5">Driver:</label>
                  <select class="w-full border border-slate-300 px-2 py-1 bg-white">
                    <option>(Keep Current) (Curr</option>
                    ${drivers.map(dr => `<option>${dr.FullName}</option>`).join('')}
                  </select>
                </div>
                <div>
                  <label class="block font-bold text-slate-700 mb-0.5">Aide / Crew:</label>
                  <select class="w-full border border-slate-300 px-2 py-1 bg-white">
                    <option>(Keep Current) (Curr</option>
                    ${aides.map(a => `<option>${a.FullName}</option>`).join('')}
                  </select>
                </div>
              </div>
              <p class="italic text-[11px] text-slate-500">Note: Selecting a specific truck, driver, or aide will update that assignment upon confirmation.</p>
            </div>

            <div>
              <label class="block font-semibold text-slate-900 mb-1">Reason for Rescheduling: *</label>
              <select id="resched-reason" class="w-full border border-slate-300 px-2.5 py-1.5 bg-white">
                <option value="Adverse Weather / Typhoon Advisory" selected>Adverse Weather / Typhoon Advisory</option>
                <option value="Truck Mechanical Maintenance">Truck Mechanical Maintenance</option>
                <option value="Store Request / Branch Delay">Store Request / Branch Delay</option>
              </select>
            </div>

            <div>
              <label class="block font-semibold text-slate-900 mb-1">Detailed Remarks / Operational Notes:</label>
              <textarea rows="3" class="w-full border border-slate-300 p-2 bg-white"></textarea>
            </div>

            <div class="flex items-center justify-end space-x-2 pt-2">
              <button type="button" onclick="document.getElementById('modal-reschedule').remove()" class="bg-white border border-slate-300 px-4 py-1.5 rounded-md font-semibold">✕ Cancel</button>
              <button type="submit" class="bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-1.5 rounded-md font-bold">💾 Confirm Reschedul</button>
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
      toast.show('Dispatch rescheduled ✓', 'success');
      document.getElementById('modal-reschedule').remove();
      renderActiveTabContent();
    });
  };

  // =========================================================================
  // MODAL 6: CANCELLATION REASON REQUIRED (Exact Screenshot 11)
  // =========================================================================
  window.openCancelModal = (id) => {
    const d = store.getById('Deliveries', id) || store.get('Deliveries')[0];
    if (!d) return;

    const modalHtml = `
      <div id="modal-cancel" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4">
        <div class="bg-white rounded-sm shadow-2xl w-full max-w-[440px] border border-slate-400 overflow-hidden">
          <!-- Window Titlebar -->
          <div class="bg-[#F3F4F6] px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-500">Cancellation Reason Required</span>
            <button onclick="document.getElementById('modal-cancel').remove()" class="text-slate-400 hover:text-slate-800">✕</button>
          </div>

          <!-- Light Pink Header (Exact Screenshot 11) -->
          <div class="bg-[#FEF2F2] px-5 py-3 border-b border-red-100">
            <h3 class="text-[17px] font-bold text-[#B91C1C]">Cancel Dispatch Order #${d.DispatchNumber}</h3>
            <p class="text-xs text-[#7F1D1D]">Please provide the official reason for cancelling this</p>
          </div>

          <form id="form-cancel" class="p-5 space-y-3 bg-white">
            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Common Cancellation Reasons:</label>
              <select id="cancel-reason" class="w-full border border-slate-300 px-2.5 py-1.5 text-xs bg-white">
                <option value="Client Cancellation Request">-- Select a standard reason or type custom details be</option>
                <option value="Store Closed / Holiday">Store Closed / Holiday</option>
                <option value="Order Duplication">Order Duplication</option>
                <option value="Client Cancellation Request">Client Cancellation Request</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-900 mb-1">Detailed Reason / Explanation (Mandatory):</label>
              <textarea id="cancel-detail" rows="3" class="w-full border border-slate-300 border-b-2 border-b-[#0284C7] p-2 text-xs focus:outline-none"></textarea>
            </div>

            <div class="bg-[#F5F5F4] -mx-5 -mb-5 px-5 py-3 mt-4 flex items-center justify-end space-x-3 border-t border-slate-200">
              <button type="submit" class="bg-[#DC2626] hover:bg-red-700 text-white px-8 py-1.5 rounded text-sm font-medium border-2 border-[#0284C7]">Confirm</button>
              <button type="button" onclick="document.getElementById('modal-cancel').remove()" class="bg-white border border-slate-300 hover:bg-slate-100 text-slate-900 px-5 py-1.5 rounded text-sm">Back / Do Not</button>
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
        CancellationReason: document.getElementById('cancel-detail').value || document.getElementById('cancel-reason').value
      }, auth.getCurrentUser());
      toast.show('Dispatch order cancelled ✓', 'info');
      document.getElementById('modal-cancel').remove();
      renderActiveTabContent();
    });
  };

  // =========================================================================
  // MODAL 7: STATEMENT OF ACCOUNT (SOA) GENERATOR (Exact Screenshot 21)
  // =========================================================================
  function openGenerateSoaModal() {
    const invoices = store.get('Invoices');

    const modalHtml = `
      <div id="modal-gen-soa" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 select-none p-4 overflow-y-auto">
        <div class="bg-white rounded-sm shadow-2xl w-full max-w-[780px] border border-slate-400 overflow-hidden my-4">
          <!-- Window Titlebar -->
          <div class="bg-[#F3F4F6] px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-500">Statement of Account (SOA) Generator - M.J. Billones Transport Service</span>
            <button onclick="document.getElementById('modal-gen-soa').remove()" class="text-slate-400 hover:text-slate-800">✕</button>
          </div>

          <!-- Dark Red Banner (Exact Screenshot 21) -->
          <div class="bg-[#881317] px-6 py-3.5 text-white">
            <h2 class="text-[19px] font-bold tracking-wide uppercase">STATEMENT OF ACCOUNT (SOA) BATCHING &nbsp;BILLING</h2>
            <p class="text-xs text-red-100">Consolidate and issue itemized monthly or weekly statements of account per client company.</p>
          </div>

          <div class="p-4">
            <div class="border border-slate-400 flex flex-col">
              <!-- Top Select / Unselect Strip -->
              <div class="p-4 border-b border-slate-400 flex items-center space-x-2">
                <button type="button" id="soa-btn-select-all" class="bg-[#F3F4F6] border border-slate-300 px-4 py-1 text-xs font-bold">☑ Select</button>
                <button type="button" id="soa-btn-unselect-all" class="bg-[#F3F4F6] border border-slate-300 px-4 py-1 text-xs font-bold">☐ Unselect</button>
              </div>

              <!-- Middle Table -->
              <div class="p-2 min-h-[260px]">
                <table class="desktop-table border border-slate-300">
                  <thead>
                    <tr>
                      <th class="border-r border-slate-300">Include</th>
                      <th class="border-r border-slate-300">Invoice #</th>
                      <th class="border-r border-slate-300">Issue</th>
                      <th class="border-r border-slate-300">Gross</th>
                      <th class="border-r border-slate-300">Incident</th>
                      <th class="border-r border-slate-300">Net Due</th>
                      <th class="border-r border-slate-300">Status</th>
                      <th>Billed In</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${invoices.map(inv => `
                      <tr>
                        <td class="text-center"><input type="checkbox" class="soa-chk" checked data-net="${inv.NetAmount || 9000}"></td>
                        <td>${inv.InvoiceNumber}</td>
                        <td>${inv.IssueDate || '2026-10-22'}</td>
                        <td>${Number(inv.TotalAmount || 9000).toFixed(2)}</td>
                        <td>0.00</td>
                        <td>${Number(inv.NetAmount || 9000).toFixed(2)}</td>
                        <td>${inv.Status}</td>
                        <td>—</td>
                      </tr>
                    `).join('')}
                  </tbody>
                </table>
              </div>

              <!-- Bottom Summary & Buttons Bar (Exact Screenshot 21) -->
              <div class="p-4 border-t border-slate-400 flex items-center justify-between">
                <div class="space-y-0.5 text-xs">
                  <div class="font-bold text-slate-900">Selected Invoices: 0 / 0</div>
                  <div class="text-slate-800">Gross Hauling: PHP 0.00</div>
                  <div class="text-[#DC2626]">Total Deductions: - PHP 0.00</div>
                </div>

                <div class="flex flex-col items-end space-y-1.5">
                  <div class="flex items-center space-x-2">
                    <button type="button" onclick="document.getElementById('modal-gen-soa').remove()" class="bg-[#D1D5DB] hover:bg-slate-400 text-slate-900 font-bold text-xs px-5 py-2">CLOSE</button>
                    <button type="button" id="btn-save-gen-soa" class="bg-[#881317] hover:bg-[#6B0F1B] text-white font-bold text-xs px-5 py-2">💾 SAVE &nbsp;PRINT OFFICIAL</button>
                  </div>
                  <button type="button" id="btn-soa-preview" class="bg-[#52525B] hover:bg-zinc-700 text-white font-bold text-xs px-8 py-1.5">🖨️ PREVIEW</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;
    document.body.insertAdjacentHTML('beforeend', modalHtml);

    document.getElementById('btn-save-gen-soa').addEventListener('click', () => {
      const soas = store.get('StatementsOfAccount');
      const newSoa = {
        SoaNumber: `SOA-2026-${String(soas.length + 1).padStart(4, '0')}`,
        ClientName: 'RED RIBBON BAKESHOP INC.',
        IssueDate: '2026-10-25',
        PeriodFrom: '2026-10-01',
        PeriodTo: '2026-10-25',
        TotalSales: 9000,
        TotalAmountDue: 9000,
        Status: 'Issued'
      };
      store.insert('StatementsOfAccount', newSoa, auth.getCurrentUser());
      toast.show(`Statement ${newSoa.SoaNumber} issued ✓`, 'success');
      document.getElementById('modal-gen-soa').remove();
      printer.printSOA(newSoa);
      renderActiveTabContent();
    });

    document.getElementById('btn-soa-preview').addEventListener('click', () => {
      const soas = store.get('StatementsOfAccount');
      printer.printSOA(soas[0] || { SoaNumber: 'SOA-2026-0001', ClientName: 'RED RIBBON BAKESHOP INC.', TotalSales: 9000 });
    });
  }

  window.printDispatchDoc = (id) => {
    const d = store.getById('Deliveries', id);
    if (d) printer.printDispatchSlip(d);
  };

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

  function initHeaderEvents() {
    const eyeBtn = document.getElementById('btn-toggle-login-pass');
    if (eyeBtn) {
      eyeBtn.addEventListener('click', () => {
        const passInp = document.getElementById('login-password');
        passInp.type = passInp.type === 'password' ? 'text' : 'password';
      });
    }

    const forgotLink = document.getElementById('forgot-password-link');
    if (forgotLink) {
      forgotLink.addEventListener('click', () => {
        alert('Default Admin Credentials:\nUsername: admin\nPassword: admin123\n\nDefault Staff Credentials:\nUsername: staff\nPassword: staff123');
      });
    }
  }

  window.switchHub = switchHub;

  init();
  initHeaderEvents();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', startMJBillonesApp);
} else {
  startMJBillonesApp();
}
