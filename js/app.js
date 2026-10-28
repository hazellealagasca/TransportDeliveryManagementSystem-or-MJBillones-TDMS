/**
 * M.J. Billones Transport Service - Core Single Page Application Logic
 */

document.addEventListener('DOMContentLoaded', () => {
  const store = window.appStore;
  const auth = window.auth;
  const toast = window.toast;
  const clock = window.analogClockPicker;
  const printer = window.docPrinter;

  let currentView = 'dashboard';
  let searchTerm = '';

  // DOM Elements
  const authContainer = document.getElementById('auth-container');
  const appContainer = document.getElementById('app-container');
  const mainContent = document.getElementById('main-content');
  const userFullNameEl = document.getElementById('header-user-fullname');
  const userRoleEl = document.getElementById('header-user-role');
  const logoutBtn = document.getElementById('btn-logout');
  const navLinks = document.querySelectorAll('.nav-link');
  const globalSearchInput = document.getElementById('global-search-input');

  // Initialize App
  function init() {
    bindAuthEvents();
    bindGlobalEvents();
    checkAuth();
  }

  function checkAuth() {
    const user = auth.getCurrentUser();
    if (user) {
      authContainer.classList.add('hidden');
      appContainer.classList.remove('hidden');
      userFullNameEl.innerText = user.FullName || user.Username;
      userRoleEl.innerText = user.Role || 'Staff';

      // Update sidebar visibility for Admin-only items
      document.querySelectorAll('.admin-only').forEach(el => {
        if (auth.isAdmin()) {
          el.classList.remove('hidden');
        } else {
          el.classList.add('hidden');
        }
      });

      navigateTo(currentView);
    } else {
      authContainer.classList.remove('hidden');
      appContainer.classList.add('hidden');
    }
  }

  function bindAuthEvents() {
    const loginForm = document.getElementById('login-form');
    const demoAdminBtn = document.getElementById('demo-login-admin');
    const demoStaffBtn = document.getElementById('demo-login-staff');
    const forgotBtn = document.getElementById('forgot-password-link');
    const forgotModal = document.getElementById('forgot-password-modal');
    const forgotClose = document.getElementById('forgot-modal-close');
    const forgotForm = document.getElementById('forgot-password-form');

    loginForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const username = document.getElementById('login-username').value;
      const password = document.getElementById('login-password').value;
      const errorEl = document.getElementById('login-error');

      try {
        errorEl.classList.add('hidden');
        await auth.login(username, password);
        toast.show(`Welcome back, ${username}!`, 'success');
        checkAuth();
      } catch (err) {
        errorEl.innerText = err.message || 'Login failed.';
        errorEl.classList.remove('hidden');
      }
    });

    demoAdminBtn.addEventListener('click', async () => {
      document.getElementById('login-username').value = 'admin';
      document.getElementById('login-password').value = 'admin123';
      loginForm.dispatchEvent(new Event('submit'));
    });

    demoStaffBtn.addEventListener('click', async () => {
      document.getElementById('login-username').value = 'staff';
      document.getElementById('login-password').value = 'staff123';
      loginForm.dispatchEvent(new Event('submit'));
    });

    if (forgotBtn) {
      forgotBtn.addEventListener('click', () => forgotModal.classList.remove('hidden'));
    }
    if (forgotClose) {
      forgotClose.addEventListener('click', () => forgotModal.classList.add('hidden'));
    }
    if (forgotForm) {
      forgotForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const user = document.getElementById('forgot-username').value;
        const ans = document.getElementById('forgot-answer').value;
        const pass = document.getElementById('forgot-newpass').value;
        try {
          await auth.resetPasswordWithSecurityAnswer(user, ans, pass);
          toast.show('Password reset successfully! You can now log in.', 'success');
          forgotModal.classList.add('hidden');
        } catch (err) {
          alert(err.message);
        }
      });
    }

    logoutBtn.addEventListener('click', () => {
      auth.logout();
      toast.show('You have logged out.', 'info');
      checkAuth();
    });
  }

  function bindGlobalEvents() {
    navLinks.forEach(link => {
      link.addEventListener('click', (e) => {
        e.preventDefault();
        const view = link.getAttribute('data-view');
        if (view) navigateTo(view);
      });
    });

    if (globalSearchInput) {
      globalSearchInput.addEventListener('input', (e) => {
        searchTerm = e.target.value.toLowerCase();
        renderView(currentView);
      });
    }

    // Live clock in topbar
    const digitalClock = document.getElementById('topbar-live-clock');
    if (digitalClock) {
      setInterval(() => {
        const d = new Date();
        digitalClock.innerText = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
      }, 1000);
    }
  }

  function navigateTo(viewName) {
    currentView = viewName;
    navLinks.forEach(link => {
      if (link.getAttribute('data-view') === viewName) {
        link.className = 'nav-link flex items-center px-4 py-3 text-sm font-semibold rounded-xl bg-[#B4182C] text-white shadow-md shadow-red-900/20';
      } else {
        link.className = 'nav-link flex items-center px-4 py-3 text-sm font-medium rounded-xl text-slate-300 hover:bg-[#242D38] hover:text-white transition-colors';
      }
    });

    renderView(viewName);
  }

  function renderView(viewName) {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    switch (viewName) {
      case 'dashboard': renderDashboard(); break;
      case 'loading': renderLoadingManagement(); break;
      case 'dispatch': renderDispatchManagement(); break;
      case 'trip-reports': renderTripReports(); break;
      case 'incidents': renderIncidentReports(); break;
      case 'trucks': renderTruckManagement(); break;
      case 'crew': renderCrewManagement(); break;
      case 'billing': renderBillingManagement(); break;
      case 'collections': renderCollections(); break;
      case 'rate-matrix': renderRateMatrix(); break;
      case 'reports': renderReportsHub(); break;
      case 'users': renderUserManagement(); break;
      case 'settings': renderSettings(); break;
      default: renderDashboard(); break;
    }
  }

  // --- VIEW RENDERERS ---

  function renderDashboard() {
    const kpis = store.getDashboardKPIs();
    const deliveries = store.get('Deliveries').slice(0, 5);
    const notifications = store.get('SystemNotifications').slice(0, 4);

    mainContent.innerHTML = `
      <div class="space-y-6">
        <!-- Banner -->
        <div class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-[#6B0F1B] to-[#B4182C] p-6 text-white shadow-xl">
          <div class="relative z-10 max-w-2xl">
            <h1 class="text-2xl font-black tracking-tight sm:text-3xl">M.J. Billones Transport Delivery Management</h1>
            <p class="mt-2 text-sm text-red-100">Live fleet operations, dual-cargo cold/dry logistics, standardized G/E-crate accounting & billing statements.</p>
            <div class="mt-4 flex flex-wrap gap-3">
              <button id="dash-btn-new-order" class="inline-flex items-center space-x-2 rounded-xl bg-white px-4 py-2 text-sm font-bold text-[#B4182C] shadow hover:bg-red-50 transition">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
                <span>New Loading Order</span>
              </button>
              <button id="dash-btn-dispatch" class="inline-flex items-center space-x-2 rounded-xl bg-red-950/40 border border-white/20 px-4 py-2 text-sm font-bold text-white hover:bg-red-950/60 transition">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>
                <span>Dispatch Trip</span>
              </button>
            </div>
          </div>
        </div>

        <!-- KPI Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          <!-- Active Dispatches -->
          <div class="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">Active Dispatches</p>
              <h3 class="text-3xl font-black text-slate-800 mt-1">${kpis.activeDispatches}</h3>
              <p class="text-xs text-blue-600 mt-1 font-semibold flex items-center">
                <span class="inline-block w-2 h-2 rounded-full bg-blue-500 mr-1.5 animate-pulse"></span>
                In Transit
              </p>
            </div>
            <div class="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1 1H9m4-1V8a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6-1a1 1 0 001 1h1M5 17a2 2 0 104 0m-4 0a2 2 0 114 0m6 0a2 2 0 104 0m-4 0a2 2 0 114 0"></path></svg>
            </div>
          </div>

          <!-- Available Trucks -->
          <div class="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">Available Trucks</p>
              <h3 class="text-3xl font-black text-slate-800 mt-1">${kpis.availableTrucks} <span class="text-sm font-semibold text-slate-400">/ ${kpis.totalTrucks}</span></h3>
              <p class="text-xs text-emerald-600 mt-1 font-semibold flex items-center">
                <span class="inline-block w-2 h-2 rounded-full bg-emerald-500 mr-1.5"></span>
                ${kpis.inUseTrucks} In Use, ${kpis.maintenanceTrucks} Maint.
              </p>
            </div>
            <div class="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            </div>
          </div>

          <!-- Active Crew Available -->
          <div class="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">Available Crew</p>
              <h3 class="text-3xl font-black text-slate-800 mt-1">${kpis.availableDrivers} <span class="text-xs font-medium text-slate-500">Drivers</span></h3>
              <p class="text-xs text-slate-500 mt-1 font-semibold">${kpis.availableAides} Aides Ready for Duty</p>
            </div>
            <div class="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"></path></svg>
            </div>
          </div>

          <!-- Total Revenue Collected -->
          <div class="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm flex items-center justify-between">
            <div>
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">Collected Revenue</p>
              <h3 class="text-2xl font-black text-[#B4182C] mt-1">PHP ${kpis.totalRevenue.toLocaleString('en-US', { minimumFractionDigits: 2 })}</h3>
              <p class="text-xs text-slate-500 mt-1 font-semibold">Receivables: PHP ${kpis.outstandingReceivables.toLocaleString('en-US')}</p>
            </div>
            <div class="w-12 h-12 rounded-xl bg-red-50 text-[#B4182C] flex items-center justify-center">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            </div>
          </div>
        </div>

        <!-- Recent Dispatches & Activity Logs -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2 bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
            <div class="p-5 border-b border-slate-100 flex items-center justify-between">
              <div>
                <h3 class="font-bold text-slate-800 text-base">Recent Delivery Dispatches</h3>
                <p class="text-xs text-slate-500">Live operational status and vehicle assignments</p>
              </div>
              <button onclick="window.navigateTo('dispatch')" class="text-xs font-bold text-[#B4182C] hover:underline">View All &rarr;</button>
            </div>
            <div class="overflow-x-auto">
              <table class="w-full text-left mj-table">
                <thead>
                  <tr>
                    <th>Dispatch No</th>
                    <th>Destination / Order</th>
                    <th>Truck & Driver</th>
                    <th>Crates</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  ${deliveries.length === 0 ? '<tr><td colspan="5" class="text-center py-6 text-slate-400">No active dispatches.</td></tr>' : deliveries.map(d => `
                    <tr>
                      <td class="font-bold text-slate-800">${d.DispatchNumber}</td>
                      <td>
                        <div class="font-semibold text-slate-700">${d.LoadingScheduleInfo || 'General Order'}</div>
                        <div class="text-xs text-slate-400">${d.DispatchDate} • ${d.DepartureTime}</div>
                      </td>
                      <td>
                        <div class="font-medium text-slate-800">${d.TruckInfo}</div>
                        <div class="text-xs text-slate-500">Driver: ${d.DriverName}</div>
                      </td>
                      <td>
                        <span class="font-bold">${d.TotalCrates}</span>
                        <span class="text-xs text-slate-400">(${d.GCrate}G / ${d.ECrate}E)</span>
                      </td>
                      <td>
                        <span class="badge ${d.Status === 'Completed' ? 'badge-completed' : (d.Status === 'Cancelled' ? 'badge-cancelled' : 'badge-in-transit')}">
                          ${d.Status}
                        </span>
                      </td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          </div>

          <!-- Operational Notifications -->
          <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm p-5 flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between mb-4">
                <h3 class="font-bold text-slate-800 text-base">System Notifications</h3>
                <span class="text-xs bg-red-100 text-[#B4182C] px-2 py-0.5 rounded-full font-bold">${notifications.length} New</span>
              </div>
              <div class="space-y-3.5">
                ${notifications.map(n => `
                  <div class="p-3 rounded-xl border border-slate-100 bg-slate-50/70 hover:bg-slate-100/60 transition">
                    <div class="flex items-start justify-between">
                      <h4 class="text-xs font-bold text-slate-800">${n.Title}</h4>
                      <span class="text-[10px] text-slate-400">${n.Timestamp ? n.Timestamp.substring(11, 16) : ''}</span>
                    </div>
                    <p class="text-xs text-slate-600 mt-1 line-clamp-2">${n.Message}</p>
                  </div>
                `).join('')}
              </div>
            </div>
            <button onclick="window.navigateTo('reports')" class="w-full mt-4 py-2.5 text-xs font-bold text-slate-600 hover:text-slate-900 bg-slate-100 hover:bg-slate-200 rounded-xl transition">
              Open Full Audit Trail & Logs
            </button>
          </div>
        </div>
      </div>
    `;

    document.getElementById('dash-btn-new-order').addEventListener('click', () => {
      window.navigateTo('loading');
      setTimeout(() => document.getElementById('btn-add-schedule')?.click(), 100);
    });

    document.getElementById('dash-btn-dispatch').addEventListener('click', () => {
      window.navigateTo('dispatch');
    });
  }

  // --- LOADING SCHEDULES VIEW ---
  function renderLoadingManagement() {
    const schedules = store.get('LoadingSchedules').filter(s => {
      if (!searchTerm) return true;
      return (s.ScheduleNumber && s.ScheduleNumber.toLowerCase().includes(searchTerm)) ||
             (s.ClientName && s.ClientName.toLowerCase().includes(searchTerm)) ||
             (s.Destination && s.Destination.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Loading Management</h1>
            <p class="text-sm text-slate-500">Client delivery orders, Cold/Dry cargo specs, and crate breakdowns</p>
          </div>
          <button id="btn-add-schedule" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md shadow-red-200 transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Create Loading Order</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Schedule No</th>
                  <th>Client & Destination</th>
                  <th>Date & Departure</th>
                  <th>Cargo Type</th>
                  <th>G-Crates</th>
                  <th>E-Crates</th>
                  <th>Total</th>
                  <th>Status</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${schedules.length === 0 ? '<tr><td colspan="9" class="text-center py-8 text-slate-400">No loading schedules found.</td></tr>' : schedules.map(s => `
                  <tr>
                    <td class="font-bold text-slate-800">${s.ScheduleNumber}</td>
                    <td>
                      <div class="font-semibold text-slate-800">${s.ClientName}</div>
                      <div class="text-xs text-slate-500 font-medium">${s.Destination}</div>
                    </td>
                    <td>
                      <div class="font-medium text-slate-700">${s.ScheduleDate}</div>
                      <div class="text-xs text-slate-400">${s.DepartureTime || '01:00 AM'}</div>
                    </td>
                    <td>
                      <span class="badge ${s.CargoType && s.CargoType.includes('Dry') ? 'badge-dry' : 'badge-cold'}">
                        ${s.CargoType || 'Cold / Chilled'}
                      </span>
                    </td>
                    <td><span class="font-semibold text-emerald-700">${s.GCrate || 0}</span></td>
                    <td><span class="font-semibold text-amber-700">${s.ECrate || 0}</span></td>
                    <td><span class="font-black text-slate-800">${s.TotalCrates || 0}</span></td>
                    <td>
                      <span class="badge ${s.Status === 'Completed' ? 'badge-completed' : (s.Status === 'Dispatched' ? 'badge-dispatched' : (s.Status === 'Cancelled' ? 'badge-cancelled' : 'badge-pending'))}">
                        ${s.Status}
                      </span>
                    </td>
                    <td class="text-right space-x-1">
                      ${s.Status === 'Pending' ? `
                        <button onclick="window.dispatchOrder(${s.Id})" class="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg font-bold text-xs" title="Dispatch Order">Dispatch</button>
                        <button onclick="window.cancelOrder(${s.Id})" class="p-1.5 text-red-600 hover:bg-red-50 rounded-lg font-bold text-xs" title="Cancel Order">Cancel</button>
                      ` : `<span class="text-xs text-slate-400 font-medium">Locked</span>`}
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-add-schedule').addEventListener('click', openAddScheduleModal);
  }

  function openAddScheduleModal() {
    const clients = store.get('Clients');
    const categories = store.get('LookupCategories');
    const destinations = categories.filter(c => c.CategoryType === 'Destination').map(c => c.ItemValue);

    const now = new Date();
    const schedNum = `LS-${now.getFullYear()}${String(now.getMonth()+1).padStart(2,'0')}${String(now.getDate()).padStart(2,'0')}-${String(Math.floor(10000 + Math.random() * 90000))}`;

    const modalHtml = `
      <div id="modal-add-schedule" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Create Loading Schedule Order</h3>
            <button onclick="document.getElementById('modal-add-schedule').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-create-schedule" class="p-6 space-y-4">
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Schedule Number</label>
              <input type="text" id="sch-num" value="${schedNum}" readonly class="w-full bg-slate-100 border border-slate-200 rounded-xl px-3 py-2 text-sm font-semibold text-slate-700">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Client Name</label>
                <select id="sch-client" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium">
                  ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                  <option value="RED RIBBON BAKESHOP INC." selected>RED RIBBON BAKESHOP INC.</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Destination</label>
                <input type="text" id="sch-destination" list="dest-list" value="TABUK" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                <datalist id="dest-list">
                  ${destinations.map(d => `<option value="${d}"></option>`).join('')}
                  <option value="TABUK"></option>
                  <option value="Pampanga"></option>
                  <option value="Tuguegarao"></option>
                  <option value="Santiago"></option>
                </datalist>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Schedule Date</label>
                <input type="date" id="sch-date" value="${now.toISOString().substring(0, 10)}" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Departure Time (🕒)</label>
                <div class="flex items-center">
                  <input type="text" id="sch-time" value="01:00 AM" readonly class="w-full border border-slate-200 rounded-l-xl px-3 py-2 text-sm font-semibold bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-sch" class="bg-[#B4182C] text-white px-3 py-2 rounded-r-xl hover:bg-[#981323]">🕒</button>
                </div>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Cargo Type</label>
                <select id="sch-cargo" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                  <option value="Cold / Chilled" selected>Cold / Chilled (0°-4°C)</option>
                  <option value="Dry Goods">Dry Goods (Ambient)</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">G-Crates</label>
                <input type="number" id="sch-gcrate" value="150" min="0" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-emerald-700">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">E-Crates</label>
                <input type="number" id="sch-ecrate" value="50" min="0" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-amber-700">
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Remarks</label>
              <input type="text" id="sch-remarks" placeholder="Optional delivery instructions" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-schedule').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md shadow-red-200">Save Order</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const timeInput = document.getElementById('sch-time');
    document.getElementById('btn-clock-sch').addEventListener('click', () => clock.open(timeInput));
    timeInput.addEventListener('click', () => clock.open(timeInput));

    const form = document.getElementById('form-create-schedule');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const g = parseInt(document.getElementById('sch-gcrate').value, 10) || 0;
      const ec = parseInt(document.getElementById('sch-ecrate').value, 10) || 0;
      const total = g + ec;

      const record = {
        ScheduleNumber: document.getElementById('sch-num').value,
        ClientName: document.getElementById('sch-client').value,
        Destination: document.getElementById('sch-destination').value,
        ScheduleDate: document.getElementById('sch-date').value,
        DepartureTime: timeInput.value,
        CargoType: document.getElementById('sch-cargo').value,
        GCrate: g,
        ECrate: ec,
        TotalCrates: total,
        Status: 'Pending',
        Remarks: document.getElementById('sch-remarks').value,
        IsPeak: 0
      };

      store.insert('LoadingSchedules', record, auth.getCurrentUser());
      toast.show(`Created loading schedule ${record.ScheduleNumber}`, 'success');
      document.getElementById('modal-add-schedule').remove();
      renderLoadingManagement();
    });
  }

  // --- DISPATCH MANAGEMENT VIEW ---
  function renderDispatchManagement() {
    const deliveries = store.get('Deliveries').filter(d => {
      if (!searchTerm) return true;
      return (d.DispatchNumber && d.DispatchNumber.toLowerCase().includes(searchTerm)) ||
             (d.TruckInfo && d.TruckInfo.toLowerCase().includes(searchTerm)) ||
             (d.DriverName && d.DriverName.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Dispatch Management</h1>
            <p class="text-sm text-slate-500">Intelligent truck recommendation, crew assignment, and trip dispatching</p>
          </div>
          <button id="btn-create-dispatch-direct" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md shadow-red-200 transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Dispatch Pending Order</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Dispatch No</th>
                  <th>Order Info</th>
                  <th>Vehicle & Specs</th>
                  <th>Assigned Crew</th>
                  <th>Crates</th>
                  <th>Status</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${deliveries.length === 0 ? '<tr><td colspan="7" class="text-center py-8 text-slate-400">No dispatches found.</td></tr>' : deliveries.map(d => `
                  <tr>
                    <td class="font-bold text-slate-800">${d.DispatchNumber}</td>
                    <td>
                      <div class="font-semibold text-slate-800">${d.LoadingScheduleInfo || 'Standard Haul'}</div>
                      <div class="text-xs text-slate-400">${d.DispatchDate} • ${d.DepartureTime}</div>
                    </td>
                    <td>
                      <div class="font-bold text-slate-800">${d.TruckInfo}</div>
                      <span class="badge ${d.CargoType && d.CargoType.includes('Dry') ? 'badge-dry' : 'badge-cold'} text-[11px] mt-0.5">
                        ${d.CargoType || 'Cold / Chilled'}
                      </span>
                    </td>
                    <td>
                      <div class="font-medium text-slate-800">Driver: ${d.DriverName}</div>
                      <div class="text-xs text-slate-500">Aides: ${d.AideNames || d.AideName || 'None'}</div>
                    </td>
                    <td>
                      <span class="font-black text-slate-800">${d.TotalCrates}</span>
                      <span class="text-xs text-slate-400">(${d.GCrate}G / ${d.ECrate}E)</span>
                    </td>
                    <td>
                      <span class="badge ${d.Status === 'Completed' ? 'badge-completed' : (d.Status === 'Cancelled' ? 'badge-cancelled' : 'badge-in-transit')}">
                        ${d.Status}
                      </span>
                    </td>
                    <td class="text-right space-x-1">
                      <button onclick="window.printDispatchSlip(${d.Id})" class="p-1.5 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-bold border border-slate-200" title="Print Trip Manifest">🖨️ Slip</button>
                      ${d.Status === 'In Transit' || d.Status === 'Dispatched' ? `
                        <button onclick="window.openCompleteTripModal(${d.Id})" class="p-1.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg text-xs font-bold shadow-sm" title="Complete Trip">Complete</button>
                      ` : ''}
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-create-dispatch-direct').addEventListener('click', () => {
      const pendingSchedules = store.get('LoadingSchedules').filter(s => s.Status === 'Pending');
      if (pendingSchedules.length === 0) {
        toast.show('No pending loading schedules to dispatch. Create a loading order first!', 'warning');
        return;
      }
      openDispatchModal(pendingSchedules[0].Id);
    });
  }

  function openDispatchModal(scheduleId) {
    const schedule = store.getById('LoadingSchedules', scheduleId);
    if (!schedule) return;

    const recommendedTrucks = store.recommendTrucks(schedule.CargoType || 'Cold / Chilled', schedule.TotalCrates || 0);
    const drivers = store.get('Crews').filter(c => c.Role.toUpperCase().includes('DRIVER') && c.IsAvailable && c.IsActive);
    const aides = store.get('Crews').filter(c => c.Role.toUpperCase().includes('AIDE') && c.IsAvailable && c.IsActive);

    const modalHtml = `
      <div id="modal-dispatch-trip" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <div>
              <h3 class="font-bold text-lg">Dispatch Trip Assignment</h3>
              <p class="text-xs text-red-100">${schedule.ScheduleNumber} • ${schedule.ClientName} (${schedule.Destination})</p>
            </div>
            <button onclick="document.getElementById('modal-dispatch-trip').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-dispatch-trip" class="p-6 space-y-5">
            <!-- Order Specs Strip -->
            <div class="bg-slate-50 p-4 rounded-xl border border-slate-200 flex flex-wrap justify-between items-center text-xs">
              <div>
                <span class="text-slate-500">Destination:</span>
                <span class="font-bold text-slate-800 ml-1">${schedule.Destination}</span>
              </div>
              <div>
                <span class="text-slate-500">Cargo Type:</span>
                <span class="badge ${schedule.CargoType && schedule.CargoType.includes('Dry') ? 'badge-dry' : 'badge-cold'} ml-1">${schedule.CargoType}</span>
              </div>
              <div>
                <span class="text-slate-500">Total Crates:</span>
                <span class="font-extrabold text-slate-900 ml-1 text-sm">${schedule.TotalCrates}</span>
                <span class="text-slate-400">(${schedule.GCrate}G / ${schedule.ECrate}E)</span>
              </div>
            </div>

            <!-- Intelligent Truck Selector -->
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Select Truck (Smart Temperature & Capacity Recommendation)</label>
              <select id="dsp-truck-select" class="w-full border border-slate-200 rounded-xl px-3 py-2.5 text-sm bg-white font-medium" required>
                ${recommendedTrucks.map(t => `
                  <option value="${t.Id}" data-info="${t.PlateNumber} (${t.TruckType})" ${!t.isAvailable ? 'disabled' : ''}>
                    ${t.PlateNumber} — ${t.TruckType} [Cap: ${t.CapacityCrates} cr] [${t.CargoType}] — (${t.badgeText})
                  </option>
                `).join('')}
              </select>
            </div>

            <!-- Crew Selection -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Assigned Driver</label>
                <select id="dsp-driver-select" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium" required>
                  ${drivers.length === 0 ? '<option value="" disabled>No drivers available</option>' : drivers.map(d => `
                    <option value="${d.Id}" data-name="${d.FullName}">${d.FullName} (${d.LicenseNumber || 'Lic. OK'})</option>
                  `).join('')}
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Primary Aide</label>
                <select id="dsp-aide-select" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium">
                  <option value="0" data-name="None">None</option>
                  ${aides.map(a => `
                    <option value="${a.Id}" data-name="${a.FullName}">${a.FullName}</option>
                  `).join('')}
                </select>
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Dispatch Date</label>
                <input type="date" id="dsp-date" value="${new Date().toISOString().substring(0, 10)}" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Departure Time (🕒)</label>
                <div class="flex items-center">
                  <input type="text" id="dsp-time" value="${schedule.DepartureTime || '08:00 AM'}" readonly class="w-full border border-slate-200 rounded-l-xl px-3 py-2 text-sm font-semibold bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-dsp" class="bg-[#B4182C] text-white px-3 py-2 rounded-r-xl hover:bg-[#981323]">🕒</button>
                </div>
              </div>
            </div>

            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Dispatch Remarks</label>
              <input type="text" id="dsp-remarks" value="Dispatched Delivery Order" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>

            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-dispatch-trip').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md shadow-red-200">Confirm & Dispatch Trip</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const timeInput = document.getElementById('dsp-time');
    document.getElementById('btn-clock-dsp').addEventListener('click', () => clock.open(timeInput));
    timeInput.addEventListener('click', () => clock.open(timeInput));

    const form = document.getElementById('form-dispatch-trip');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const truckSel = document.getElementById('dsp-truck-select');
      const driverSel = document.getElementById('dsp-driver-select');
      const aideSel = document.getElementById('dsp-aide-select');

      const truckId = truckSel.value;
      const truckInfo = truckSel.selectedOptions[0]?.getAttribute('data-info') || 'Truck';
      const driverId = driverSel.value;
      const driverName = driverSel.selectedOptions[0]?.getAttribute('data-name') || 'Driver';
      const aideId = aideSel.value;
      const aideName = aideSel.selectedOptions[0]?.getAttribute('data-name') || 'None';

      try {
        const delivery = store.dispatchLoadingSchedule(schedule.Id, {
          TruckId: truckId,
          TruckInfo: truckInfo,
          DriverId: driverId,
          DriverName: driverName,
          AideId: aideId,
          AideName: aideName,
          DispatchDate: document.getElementById('dsp-date').value,
          DepartureTime: timeInput.value,
          Remarks: document.getElementById('dsp-remarks').value
        }, auth.getCurrentUser());

        toast.show(`Trip ${delivery.DispatchNumber} dispatched successfully!`, 'success');
        document.getElementById('modal-dispatch-trip').remove();
        renderDispatchManagement();
      } catch (err) {
        alert(err.message);
      }
    });
  }

  // --- TRIP COMPLETION MODAL ---
  function openCompleteTripModal(deliveryId) {
    const delivery = store.getById('Deliveries', deliveryId);
    if (!delivery) return;

    const modalHtml = `
      <div id="modal-complete-trip" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <div>
              <h3 class="font-bold text-lg">Complete Trip & Crate Reconciliation</h3>
              <p class="text-xs text-red-100">${delivery.DispatchNumber} • ${delivery.TruckInfo}</p>
            </div>
            <button onclick="document.getElementById('modal-complete-trip').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-complete-trip" class="p-6 space-y-4">
            <div class="bg-slate-50 p-3.5 rounded-xl border border-slate-200 text-xs">
              <span class="text-slate-500 font-medium">Dispatched Crate Load:</span>
              <span class="font-black text-slate-800 ml-1 text-sm">${delivery.TotalCrates} Crates</span>
              <span class="text-slate-400">(${delivery.GCrate} G-Crates / ${delivery.ECrate} E-Crates)</span>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Store Arrival Time (🕒)</label>
                <div class="flex items-center">
                  <input type="text" id="trip-arrival-time" value="10:30 AM" readonly class="w-full border border-slate-200 rounded-l-xl px-3 py-2 text-sm font-semibold bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-arr" class="bg-[#B4182C] text-white px-3 py-2 rounded-r-xl hover:bg-[#981323]">🕒</button>
                </div>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Departure Time (🕒)</label>
                <div class="flex items-center">
                  <input type="text" id="trip-dept-time" value="11:45 AM" readonly class="w-full border border-slate-200 rounded-l-xl px-3 py-2 text-sm font-semibold bg-slate-50 cursor-pointer">
                  <button type="button" id="btn-clock-dep" class="bg-[#B4182C] text-white px-3 py-2 rounded-r-xl hover:bg-[#981323]">🕒</button>
                </div>
              </div>
            </div>

            <!-- Crates Delivered & Returned -->
            <div class="grid grid-cols-2 gap-4">
              <div class="p-3 bg-emerald-50 rounded-xl border border-emerald-200">
                <div class="text-xs font-bold text-emerald-800 uppercase mb-2">Delivered Crates</div>
                <div class="space-y-2">
                  <div>
                    <label class="text-[11px] font-semibold text-emerald-700">G-Crates Delivered</label>
                    <input type="number" id="trip-del-g" value="${delivery.GCrate || 0}" min="0" class="w-full border border-emerald-300 rounded-lg px-2 py-1 text-sm font-bold bg-white">
                  </div>
                  <div>
                    <label class="text-[11px] font-semibold text-emerald-700">E-Crates Delivered</label>
                    <input type="number" id="trip-del-e" value="${delivery.ECrate || 0}" min="0" class="w-full border border-emerald-300 rounded-lg px-2 py-1 text-sm font-bold bg-white">
                  </div>
                </div>
              </div>

              <div class="p-3 bg-blue-50 rounded-xl border border-blue-200">
                <div class="text-xs font-bold text-blue-800 uppercase mb-2">Returned Empty Crates</div>
                <div class="space-y-2">
                  <div>
                    <label class="text-[11px] font-semibold text-blue-700">G-Crates Returned</label>
                    <input type="number" id="trip-ret-g" value="${delivery.GCrate || 0}" min="0" class="w-full border border-blue-300 rounded-lg px-2 py-1 text-sm font-bold bg-white">
                  </div>
                  <div>
                    <label class="text-[11px] font-semibold text-blue-700">E-Crates Returned</label>
                    <input type="number" id="trip-ret-e" value="${delivery.ECrate || 0}" min="0" class="w-full border border-blue-300 rounded-lg px-2 py-1 text-sm font-bold bg-white">
                  </div>
                </div>
              </div>
            </div>

            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Store Manager Sign-off</label>
              <input type="text" id="trip-mgr" value="Maricel De Ocampo" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-semibold">
            </div>

            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Remarks</label>
              <input type="text" id="trip-remarks" value="Trip Completed Successfully" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>

            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-complete-trip').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl text-sm shadow-md">Complete Trip</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const arrInput = document.getElementById('trip-arrival-time');
    const depInput = document.getElementById('trip-dept-time');
    document.getElementById('btn-clock-arr').addEventListener('click', () => clock.open(arrInput));
    document.getElementById('btn-clock-dep').addEventListener('click', () => clock.open(depInput));

    const form = document.getElementById('form-complete-trip');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      try {
        const report = store.completeTrip(delivery.Id, {
          ArrivalTime: arrInput.value,
          DepartureTime: depInput.value,
          DeliveredGCrates: parseInt(document.getElementById('trip-del-g').value, 10) || 0,
          DeliveredECrates: parseInt(document.getElementById('trip-del-e').value, 10) || 0,
          ReturnedGCrates: parseInt(document.getElementById('trip-ret-g').value, 10) || 0,
          ReturnedECrates: parseInt(document.getElementById('trip-ret-e').value, 10) || 0,
          StoreManagerName: document.getElementById('trip-mgr').value,
          Remarks: document.getElementById('trip-remarks').value
        }, auth.getCurrentUser());

        toast.show(`Trip ${delivery.DispatchNumber} completed! Report #${report.TripReportNumber}`, 'success');
        document.getElementById('modal-complete-trip').remove();
        renderDispatchManagement();
      } catch (err) {
        alert(err.message);
      }
    });
  }

  // --- TRIP REPORTS VIEW ---
  function renderTripReports() {
    const reports = store.get('TripReports').filter(r => {
      if (!searchTerm) return true;
      return (r.TripReportNumber && r.TripReportNumber.toLowerCase().includes(searchTerm)) ||
             (r.DispatchNumber && r.DispatchNumber.toLowerCase().includes(searchTerm)) ||
             (r.StoreManagerName && r.StoreManagerName.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div>
          <h1 class="text-2xl font-black text-slate-800">Trip Completion Reports</h1>
          <p class="text-sm text-slate-500">Delivery completion registers, crate returns, and store manager signatures</p>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Report No</th>
                  <th>Dispatch Ref</th>
                  <th>Date & Time</th>
                  <th>Delivered Crates</th>
                  <th>Returned Crates</th>
                  <th>Store Manager</th>
                  <th>Remarks</th>
                </tr>
              </thead>
              <tbody>
                ${reports.length === 0 ? '<tr><td colspan="7" class="text-center py-8 text-slate-400">No trip reports found.</td></tr>' : reports.map(r => `
                  <tr>
                    <td class="font-bold text-slate-800">${r.TripReportNumber}</td>
                    <td class="font-semibold text-slate-700">${r.DispatchNumber}</td>
                    <td>
                      <div class="font-medium text-slate-800">${r.ReportDate}</div>
                      <div class="text-xs text-slate-400">${r.ArrivalTime} - ${r.DepartureTime}</div>
                    </td>
                    <td>
                      <span class="font-bold text-emerald-700">${r.DeliveredCrates}</span>
                      <span class="text-xs text-slate-400">(${r.DeliveredGCrates || 0}G / ${r.DeliveredECrates || 0}E)</span>
                    </td>
                    <td>
                      <span class="font-bold text-blue-700">${r.ReturnedCrates}</span>
                      <span class="text-xs text-slate-400">(${r.ReturnedGCrates || 0}G / ${r.ReturnedECrates || 0}E)</span>
                    </td>
                    <td class="font-medium text-slate-800">${r.StoreManagerName}</td>
                    <td class="text-xs text-slate-500">${r.Remarks || 'N/A'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  }

  // --- INCIDENT REPORTS VIEW ---
  function renderIncidentReports() {
    const incidents = store.get('IncidentReports').filter(i => {
      if (!searchTerm) return true;
      return (i.DispatchNumber && i.DispatchNumber.toLowerCase().includes(searchTerm)) ||
             (i.Cause && i.Cause.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Incident Reports</h1>
            <p class="text-sm text-slate-500">Damage logs, delays, breakdowns, and crew liability assessments</p>
          </div>
          <button id="btn-add-incident" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Log New Incident</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Dispatch Ref</th>
                  <th>Incident Date</th>
                  <th>Cause & Description</th>
                  <th>Damaged Crates</th>
                  <th>Estimated Cost</th>
                  <th>Crew Chargeable</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${incidents.length === 0 ? '<tr><td colspan="8" class="text-center py-8 text-slate-400">No incident reports found.</td></tr>' : incidents.map(i => `
                  <tr>
                    <td class="font-bold text-slate-800">#${i.Id}</td>
                    <td class="font-semibold text-slate-700">${i.DispatchNumber}</td>
                    <td class="text-slate-600">${i.IncidentDate}</td>
                    <td>
                      <div class="font-bold text-slate-800 text-xs">${i.Cause}</div>
                      <div class="text-xs text-slate-500 line-clamp-1">${i.Description}</div>
                    </td>
                    <td><span class="font-bold text-red-600">${i.DamagedQuantity}</span></td>
                    <td><span class="font-black text-slate-800">PHP ${Number(i.EstimatedCost || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span></td>
                    <td>
                      <span class="badge ${i.ChargeableToCrew ? 'badge-cancelled' : 'badge-completed'}">
                        ${i.ChargeableToCrew ? 'Yes' : 'No'}
                      </span>
                    </td>
                    <td>
                      <span class="badge badge-pending">${i.Status}</span>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-add-incident').addEventListener('click', openAddIncidentModal);
  }

  function openAddIncidentModal() {
    const deliveries = store.get('Deliveries');
    const modalHtml = `
      <div id="modal-add-incident" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Log Incident / Damage Report</h3>
            <button onclick="document.getElementById('modal-add-incident').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-log-incident" class="p-6 space-y-4">
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Select Delivery Dispatch</label>
              <select id="inc-delivery" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium" required>
                ${deliveries.map(d => `<option value="${d.Id}" data-num="${d.DispatchNumber}">${d.DispatchNumber} — ${d.TruckInfo} (${d.DispatchDate})</option>`).join('')}
              </select>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Incident Date</label>
                <input type="date" id="inc-date" value="${new Date().toISOString().substring(0, 10)}" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Damaged / Missing Crates</label>
                <input type="number" id="inc-qty" value="10" min="0" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-red-600">
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Estimated Cost (PHP)</label>
                <input type="number" id="inc-cost" value="4500" min="0" step="0.01" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Cause Category</label>
                <select id="inc-cause" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium">
                  <option value="Missing / Damaged Crates from Trip Delivery">Missing / Damaged Crates</option>
                  <option value="Vehicle Breakdown / Mechanical Failure">Vehicle Breakdown</option>
                  <option value="Traffic Route Delay">Route Delay</option>
                  <option value="Chiller / Temperature Malfunction">Temperature Malfunction</option>
                </select>
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Description / Notes</label>
              <textarea id="inc-desc" rows="2" class="w-full border border-slate-200 rounded-xl p-3 text-sm" placeholder="Detailed description of the event..."></textarea>
            </div>
            <div class="flex items-center space-x-2">
              <input type="checkbox" id="inc-chargeable" checked class="rounded border-slate-300 text-[#B4182C] focus:ring-[#B4182C]">
              <label for="inc-chargeable" class="text-xs font-bold text-slate-700">Chargeable to Crew / Driver Liability</label>
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-incident').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Record Incident</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const form = document.getElementById('form-log-incident');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const sel = document.getElementById('inc-delivery');
      const dNum = sel.selectedOptions[0]?.getAttribute('data-num') || 'DSP';

      const record = {
        DeliveryId: Number(sel.value),
        DispatchNumber: dNum,
        IncidentDate: document.getElementById('inc-date').value,
        DamagedQuantity: parseInt(document.getElementById('inc-qty').value, 10) || 0,
        EstimatedCost: parseFloat(document.getElementById('inc-cost').value) || 0,
        Cause: document.getElementById('inc-cause').value,
        ChargeableToCrew: document.getElementById('inc-chargeable').checked ? 1 : 0,
        Description: document.getElementById('inc-desc').value,
        Status: 'Logged',
        PhotoPath: ''
      };

      store.insert('IncidentReports', record, auth.getCurrentUser());
      toast.show('Incident recorded successfully!', 'warning');
      document.getElementById('modal-add-incident').remove();
      renderIncidentReports();
    });
  }

  // --- TRUCK MANAGEMENT VIEW ---
  function renderTruckManagement() {
    const trucks = store.get('Trucks').filter(t => {
      if (!searchTerm) return true;
      return (t.PlateNumber && t.PlateNumber.toLowerCase().includes(searchTerm)) ||
             (t.TruckType && t.TruckType.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Fleet & Truck Management</h1>
            <p class="text-sm text-slate-500">Commercial vehicles, plate numbers, capacities & Cold/Dry capabilities</p>
          </div>
          <button id="btn-add-truck" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Register Truck</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Plate Number</th>
                  <th>Truck Type</th>
                  <th>Capacity (Crates)</th>
                  <th>Cargo Capability</th>
                  <th>Status</th>
                  <th>Remarks</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${trucks.map(t => `
                  <tr>
                    <td class="font-black text-slate-800">${t.PlateNumber}</td>
                    <td class="font-medium text-slate-700">${t.TruckType}</td>
                    <td><span class="font-extrabold text-slate-900">${t.CapacityCrates}</span> Crates</td>
                    <td>
                      <span class="badge ${t.CargoType && t.CargoType.includes('Dry') ? 'badge-dry' : 'badge-cold'}">
                        ${t.CargoType}
                      </span>
                    </td>
                    <td>
                      <span class="badge ${t.Status === 'Available' ? 'badge-available' : (t.Status === 'In Use' ? 'badge-in-use' : (t.Status === 'Under Maintenance' ? 'badge-maintenance' : 'badge-inactive'))}">
                        ${t.Status}
                      </span>
                    </td>
                    <td class="text-xs text-slate-400">${t.Remarks || '—'}</td>
                    <td class="text-right space-x-1">
                      <button onclick="window.editTruckStatus(${t.Id})" class="p-1.5 text-blue-600 hover:bg-blue-50 rounded-lg text-xs font-bold">Status</button>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-add-truck').addEventListener('click', openAddTruckModal);
  }

  function openAddTruckModal() {
    const modalHtml = `
      <div id="modal-add-truck" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Register Truck Vehicle</h3>
            <button onclick="document.getElementById('modal-add-truck').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-add-truck" class="p-6 space-y-4">
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Plate Number</label>
              <input type="text" id="trk-plate" placeholder="e.g. NBD 1234" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm uppercase font-bold">
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Truck Type / Category</label>
              <select id="trk-type" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                <option value="FORWARD 4 TONS">FORWARD 4 TONS</option>
                <option value="FORWARD 6 TONS" selected>FORWARD 6 TONS</option>
                <option value="FORWARD 8 TONS">FORWARD 8 TONS</option>
                <option value="Refrigerated Van">Refrigerated Van</option>
                <option value="6-Wheeler Truck">6-Wheeler Truck</option>
              </select>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Capacity (Crates)</label>
                <input type="number" id="trk-cap" value="230" min="10" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Cargo Type</label>
                <select id="trk-cargo" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                  <option value="Cold / Chilled" selected>Cold / Chilled</option>
                  <option value="Dry Goods">Dry Goods</option>
                </select>
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Initial Status</label>
              <select id="trk-status" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                <option value="Available" selected>Available</option>
                <option value="Under Maintenance">Under Maintenance</option>
                <option value="Inactive">Inactive</option>
              </select>
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-truck').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Register</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const form = document.getElementById('form-add-truck');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const record = {
        PlateNumber: document.getElementById('trk-plate').value.trim().toUpperCase(),
        TruckType: document.getElementById('trk-type').value,
        CapacityCrates: parseInt(document.getElementById('trk-cap').value, 10),
        CargoType: document.getElementById('trk-cargo').value,
        Status: document.getElementById('trk-status').value,
        Remarks: ''
      };
      store.insert('Trucks', record, auth.getCurrentUser());
      toast.show(`Registered Truck ${record.PlateNumber}`, 'success');
      document.getElementById('modal-add-truck').remove();
      renderTruckManagement();
    });
  }

  // --- CREW MANAGEMENT VIEW ---
  function renderCrewManagement() {
    const crews = store.get('Crews').filter(c => {
      if (!searchTerm) return true;
      return (c.FullName && c.FullName.toLowerCase().includes(searchTerm)) ||
             (c.Role && c.Role.toLowerCase().includes(searchTerm)) ||
             (c.LicenseNumber && c.LicenseNumber.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Crew Management</h1>
            <p class="text-sm text-slate-500">Drivers, aides, licenses, wages, and duty availability</p>
          </div>
          <button id="btn-add-crew" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Add Crew Member</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Full Name</th>
                  <th>Role</th>
                  <th>Contact Number</th>
                  <th>License No</th>
                  <th>Daily Wage</th>
                  <th>Allowance</th>
                  <th>Availability</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                ${crews.map(c => `
                  <tr>
                    <td class="font-bold text-slate-800">${c.FullName}</td>
                    <td>
                      <span class="inline-block px-2 py-0.5 rounded text-xs font-bold ${c.Role.toUpperCase().includes('DRIVER') ? 'bg-indigo-50 text-indigo-700' : 'bg-slate-100 text-slate-700'}">
                        ${c.Role}
                      </span>
                    </td>
                    <td class="text-xs text-slate-600 font-mono">${c.ContactNumber || '—'}</td>
                    <td class="text-xs text-slate-600 font-mono">${c.LicenseNumber || '—'}</td>
                    <td class="font-semibold text-slate-800">PHP ${Number(c.DailyWage || 0).toLocaleString('en-US')}</td>
                    <td class="font-semibold text-slate-800">PHP ${Number(c.FoodAllowance || 0).toLocaleString('en-US')}</td>
                    <td>
                      <span class="badge ${c.IsAvailable ? 'badge-available' : 'badge-in-use'}">
                        ${c.IsAvailable ? 'Available' : 'On Trip'}
                      </span>
                    </td>
                    <td>
                      <span class="badge ${c.IsActive ? 'badge-available' : 'badge-inactive'}">
                        ${c.IsActive ? 'Active' : 'Inactive'}
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

    document.getElementById('btn-add-crew').addEventListener('click', openAddCrewModal);
  }

  function openAddCrewModal() {
    const modalHtml = `
      <div id="modal-add-crew" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Add Crew Personnel</h3>
            <button onclick="document.getElementById('modal-add-crew').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-add-crew" class="p-6 space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">First Name</label>
                <input type="text" id="crw-first" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Last Name</label>
                <input type="text" id="crw-last" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Role</label>
                <select id="crw-role" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                  <option value="DRIVER" selected>Driver</option>
                  <option value="Aide">Aide</option>
                  <option value="Mechanic">Mechanic</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">License Number</label>
                <input type="text" id="crw-license" placeholder="e.g. D01-24-000001" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm uppercase">
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Contact Number</label>
              <input type="text" id="crw-phone" placeholder="09XX-XXX-XXXX" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Daily Wage (PHP)</label>
                <input type="number" id="crw-wage" value="650" min="0" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Food Allowance (PHP)</label>
                <input type="number" id="crw-allowance" value="150" min="0" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-crew').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Add Personnel</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const form = document.getElementById('form-add-crew');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const first = document.getElementById('crw-first').value.trim();
      const last = document.getElementById('crw-last').value.trim();
      const full = `${last.toUpperCase()}, ${first.toUpperCase()}`;

      const record = {
        FirstName: first,
        LastName: last,
        MiddleName: '',
        FullName: full,
        Role: document.getElementById('crw-role').value,
        LicenseNumber: document.getElementById('crw-license').value.trim(),
        ContactNumber: document.getElementById('crw-phone').value.trim(),
        DailyWage: parseFloat(document.getElementById('crw-wage').value) || 0,
        FoodAllowance: parseFloat(document.getElementById('crw-allowance').value) || 0,
        IsAvailable: 1,
        IsActive: 1,
        PhotoPath: ''
      };

      store.insert('Crews', record, auth.getCurrentUser());
      toast.show(`Added crew member ${full}`, 'success');
      document.getElementById('modal-add-crew').remove();
      renderCrewManagement();
    });
  }

  // --- BILLING MANAGEMENT VIEW ---
  function renderBillingManagement() {
    const invoices = store.get('Invoices').filter(i => {
      if (!searchTerm) return true;
      return (i.InvoiceNumber && i.InvoiceNumber.toLowerCase().includes(searchTerm)) ||
             (i.ClientName && i.ClientName.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Billing & Invoices</h1>
            <p class="text-sm text-slate-500">15-day delivery service billing statements, gross transport amounts & deduction clamping</p>
          </div>
          <button id="btn-create-invoice" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Generate Invoice</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Invoice No</th>
                  <th>Client</th>
                  <th>Issue & Due Date</th>
                  <th>Gross Amount</th>
                  <th>Deductions</th>
                  <th>Net Due</th>
                  <th>Status</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${invoices.length === 0 ? '<tr><td colspan="8" class="text-center py-8 text-slate-400">No invoices generated yet.</td></tr>' : invoices.map(i => `
                  <tr>
                    <td class="font-bold text-slate-800">${i.InvoiceNumber}</td>
                    <td class="font-semibold text-slate-800">${i.ClientName}</td>
                    <td>
                      <div class="font-medium text-slate-700">${i.IssueDate}</div>
                      <div class="text-xs text-slate-400">Due: ${i.DueDate}</div>
                    </td>
                    <td>PHP ${Number(i.TotalAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td class="text-red-600 font-medium">- PHP ${Number(i.Deductions || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td><span class="font-black text-slate-900">PHP ${Number(i.NetAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span></td>
                    <td>
                      <span class="badge ${i.Status === 'Paid' ? 'badge-completed' : (i.Status === 'Partially Paid' ? 'badge-pending' : 'badge-unpaid')}">
                        ${i.Status}
                      </span>
                    </td>
                    <td class="text-right space-x-1">
                      <button onclick="window.printInvoiceDoc(${i.Id})" class="p-1.5 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-bold border border-slate-200" title="Print Invoice">🖨️ Statement</button>
                      ${i.Status !== 'Paid' ? `
                        <button onclick="window.openRecordPaymentModal(${i.Id})" class="p-1.5 bg-[#B4182C] hover:bg-[#981323] text-white rounded-lg text-xs font-bold shadow-sm" title="Receive Payment">Collect</button>
                      ` : ''}
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-create-invoice').addEventListener('click', openCreateInvoiceModal);
  }

  function openCreateInvoiceModal() {
    const completedTrips = store.get('Deliveries').filter(d => d.Status === 'Completed');
    const clients = store.get('Clients');

    const modalHtml = `
      <div id="modal-create-invoice" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Generate Billing Invoice</h3>
            <button onclick="document.getElementById('modal-create-invoice').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-create-inv" class="p-6 space-y-4">
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Select Delivery Trip</label>
              <select id="inv-delivery" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium" required>
                ${completedTrips.length === 0 ? '<option value="0">No completed trips (manual entry)</option>' : completedTrips.map(d => `
                  <option value="${d.Id}">${d.DispatchNumber} — ${d.LoadingScheduleInfo} (${d.TotalCrates} Crates)</option>
                `).join('')}
              </select>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Client Name</label>
              <select id="inv-client" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium">
                ${clients.map(c => `<option value="${c.CompanyName}">${c.CompanyName}</option>`).join('')}
                <option value="RED RIBBON BAKESHOP INC." selected>RED RIBBON BAKESHOP INC.</option>
              </select>
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Gross Amount (PHP)</label>
                <input type="number" id="inv-gross" value="12500" min="0" step="0.01" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold">
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Deductions (PHP)</label>
                <input type="number" id="inv-deductions" value="0" min="0" step="0.01" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold text-red-600">
              </div>
            </div>
            <div class="p-3 bg-red-50 rounded-xl border border-red-100 flex justify-between items-center text-sm">
              <span class="font-bold text-[#6B0F1B]">Net Due:</span>
              <span id="inv-net-preview" class="font-black text-[#B4182C] text-base">PHP 12,500.00</span>
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-create-invoice').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Issue Invoice</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const grossInput = document.getElementById('inv-gross');
    const dedInput = document.getElementById('inv-deductions');
    const netPreview = document.getElementById('inv-net-preview');

    const updateNet = () => {
      const g = parseFloat(grossInput.value) || 0;
      const d = parseFloat(dedInput.value) || 0;
      const net = Math.max(0, g - d);
      netPreview.innerText = `PHP ${net.toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
    };

    grossInput.addEventListener('input', updateNet);
    dedInput.addEventListener('input', updateNet);

    const form = document.getElementById('form-create-inv');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const invoice = store.createBillingInvoice({
        DeliveryId: Number(document.getElementById('inv-delivery').value),
        ClientName: document.getElementById('inv-client').value,
        TotalAmount: parseFloat(grossInput.value) || 0,
        Deductions: parseFloat(dedInput.value) || 0
      }, auth.getCurrentUser());

      toast.show(`Issued invoice ${invoice.InvoiceNumber}`, 'success');
      document.getElementById('modal-create-invoice').remove();
      renderBillingManagement();
    });
  }

  // --- COLLECTIONS & RECEIPTS VIEW ---
  function renderCollections() {
    const receipts = store.get('CollectionReceipts').filter(r => {
      if (!searchTerm) return true;
      return (r.ReceiptNumber && r.ReceiptNumber.toLowerCase().includes(searchTerm)) ||
             (r.InvoiceNumber && r.InvoiceNumber.toLowerCase().includes(searchTerm)) ||
             (r.PaymentMethod && r.PaymentMethod.toLowerCase().includes(searchTerm));
    });

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Collections & Official Receipts</h1>
            <p class="text-sm text-slate-500">Payment receipts (Cash, Check, Transfer), invoice balance reconciliations & receipt printing</p>
          </div>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Receipt No</th>
                  <th>Invoice Ref</th>
                  <th>Payment Date</th>
                  <th>Payment Method</th>
                  <th>Check / Ref No</th>
                  <th>Amount Paid</th>
                  <th class="text-right">Action</th>
                </tr>
              </thead>
              <tbody>
                ${receipts.length === 0 ? '<tr><td colspan="7" class="text-center py-8 text-slate-400">No collection receipts logged yet.</td></tr>' : receipts.map(r => `
                  <tr>
                    <td class="font-bold text-[#B4182C]">${r.ReceiptNumber}</td>
                    <td class="font-semibold text-slate-800">${r.InvoiceNumber}</td>
                    <td class="text-slate-600">${r.PaymentDate}</td>
                    <td>
                      <span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-bold bg-slate-100 text-slate-700">
                        ${r.PaymentMethod}
                      </span>
                    </td>
                    <td class="font-mono text-xs text-slate-500">${r.ReferenceNumber || '—'}</td>
                    <td><span class="font-black text-emerald-700">PHP ${Number(r.AmountPaid || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span></td>
                    <td class="text-right">
                      <button onclick="window.printReceiptDoc(${r.Id})" class="p-1.5 text-slate-700 hover:bg-slate-100 rounded-lg text-xs font-bold border border-slate-200" title="Print Official Receipt">🖨️ Receipt</button>
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

  function openRecordPaymentModal(invoiceId) {
    const invoice = store.getById('Invoices', invoiceId);
    if (!invoice) return;

    const modalHtml = `
      <div id="modal-record-payment" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <div>
              <h3 class="font-bold text-lg">Record Collection Payment</h3>
              <p class="text-xs text-red-100">${invoice.InvoiceNumber} • ${invoice.ClientName}</p>
            </div>
            <button onclick="document.getElementById('modal-record-payment').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-record-pmt" class="p-6 space-y-4">
            <div class="p-3 bg-slate-50 rounded-xl border border-slate-200 flex justify-between items-center text-xs">
              <span class="text-slate-500 font-semibold">Net Due on Invoice:</span>
              <span class="font-black text-slate-900 text-sm">PHP ${Number(invoice.NetAmount).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Amount Paid (PHP)</label>
              <input type="number" id="pmt-amount" value="${invoice.NetAmount}" min="0.01" step="0.01" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-base font-black text-emerald-700">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Payment Method</label>
                <select id="pmt-method" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm bg-white font-medium">
                  <option value="CHECK" selected>Check</option>
                  <option value="CASH">Cash</option>
                  <option value="BANK TRANSFER">Bank Transfer</option>
                  <option value="CARD">Card</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Payment Date</label>
                <input type="date" id="pmt-date" value="${new Date().toISOString().substring(0, 10)}" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Check / Reference Number</label>
              <input type="text" id="pmt-ref" placeholder="e.g. CHK-123456" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-record-payment').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Confirm & Print Receipt</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const form = document.getElementById('form-record-pmt');
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      try {
        const rc = store.recordCollectionPayment(invoice.Id, {
          AmountPaid: parseFloat(document.getElementById('pmt-amount').value),
          PaymentMethod: document.getElementById('pmt-method').value,
          PaymentDate: document.getElementById('pmt-date').value,
          ReferenceNumber: document.getElementById('pmt-ref').value
        }, auth.getCurrentUser());

        toast.show(`Issued Official Receipt ${rc.ReceiptNumber}`, 'success');
        document.getElementById('modal-record-payment').remove();
        renderBillingManagement();
        printer.printOfficialReceipt(rc, invoice);
      } catch (err) {
        alert(err.message);
      }
    });
  }

  // --- RATE MATRIX VIEW ---
  function renderRateMatrix() {
    const rates = store.get('RateMatrix');

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Transport Rate Matrix</h1>
            <p class="text-sm text-slate-500">Route schedule pricing, destination tariffs, and seasonal peak rates</p>
          </div>
          <button id="btn-add-rate" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Add Rate</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Route Name</th>
                  <th>Destination</th>
                  <th>Truck Type</th>
                  <th>Peak Season</th>
                  <th>Standard Rate (PHP)</th>
                </tr>
              </thead>
              <tbody>
                ${rates.map(r => `
                  <tr>
                    <td class="font-bold text-slate-800">${r.RouteName}</td>
                    <td class="font-semibold text-slate-700">${r.Destination}</td>
                    <td class="text-slate-600">${r.TruckType}</td>
                    <td>
                      <span class="badge ${r.IsPeakSeason ? 'badge-cancelled' : 'badge-completed'}">
                        ${r.IsPeakSeason ? 'Peak Rate' : 'Regular Rate'}
                      </span>
                    </td>
                    <td><span class="font-black text-slate-900">PHP ${Number(r.RateAmount || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}</span></td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-add-rate').addEventListener('click', () => {
      const route = prompt('Enter Route Name:', 'Route D - Isabela Express');
      if (!route) return;
      const dest = prompt('Enter Destination:', 'Santiago / Cauayan');
      if (!dest) return;
      const rate = parseFloat(prompt('Enter Rate (PHP):', '14000')) || 0;
      store.insert('RateMatrix', {
        RouteName: route,
        Destination: dest,
        TruckType: 'FORWARD 6 TONS',
        IsPeakSeason: 0,
        RateAmount: rate
      }, auth.getCurrentUser());
      toast.show('Rate added to matrix!', 'success');
      renderRateMatrix();
    });
  }

  // --- AUDIT REPORTS VIEW ---
  function renderReportsHub() {
    const auditLogs = store.get('AuditTrail').slice(0, 50);

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">Audit & Management Reports</h1>
            <p class="text-sm text-slate-500">Security event logs, dispatch summaries, and CSV data export</p>
          </div>
          <button id="btn-export-audit-csv" class="inline-flex items-center space-x-2 bg-slate-800 hover:bg-slate-900 text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"></path></svg>
            <span>Export CSV</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>User & Role</th>
                  <th>Action</th>
                  <th>Target Record</th>
                  <th>Details</th>
                  <th>Client</th>
                </tr>
              </thead>
              <tbody>
                ${auditLogs.map(a => `
                  <tr>
                    <td class="font-mono text-xs text-slate-500">${a.Timestamp}</td>
                    <td>
                      <div class="font-bold text-slate-800 text-xs">${a.Username}</div>
                      <div class="text-[11px] text-slate-400">${a.UserRole || 'User'}</div>
                    </td>
                    <td>
                      <span class="inline-block px-2 py-0.5 rounded text-xs font-bold ${a.ActionType === 'INSERT' ? 'bg-emerald-50 text-emerald-700' : (a.ActionType === 'LOGIN' ? 'bg-blue-50 text-blue-700' : 'bg-slate-100 text-slate-700')}">
                        ${a.ActionType}
                      </span>
                    </td>
                    <td class="font-semibold text-xs text-slate-700">${a.RecordIdentifier || a.TableName}</td>
                    <td class="text-xs text-slate-600 max-w-xs truncate">${a.ReasonOrNotes || a.NewValue}</td>
                    <td class="text-xs text-slate-400">${a.IpOrMachine || 'Web'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-export-audit-csv').addEventListener('click', () => {
      let csv = 'Timestamp,Username,Role,Action,Table,Details\n';
      auditLogs.forEach(a => {
        csv += `"${a.Timestamp}","${a.Username}","${a.UserRole}","${a.ActionType}","${a.TableName}","${(a.ReasonOrNotes || '').replace(/"/g, '""')}"\n`;
      });
      const blob = new Blob([csv], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `MJ_Billones_Audit_Trail_${new Date().toISOString().substring(0,10)}.csv`;
      a.click();
      toast.show('Exported audit trail to CSV!', 'success');
    });
  }

  // --- USER MANAGEMENT VIEW ---
  function renderUserManagement() {
    if (!auth.isAdmin()) {
      mainContent.innerHTML = `<div class="p-8 text-center text-red-600 font-bold">Access restricted to Admin role.</div>`;
      return;
    }

    const users = store.get('Users');

    mainContent.innerHTML = `
      <div class="space-y-6">
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 class="text-2xl font-black text-slate-800">User Account Management</h1>
            <p class="text-sm text-slate-500">System credentials, roles (Admin / Staff), and access controls</p>
          </div>
          <button id="btn-add-user" class="inline-flex items-center space-x-2 bg-[#B4182C] hover:bg-[#981323] text-white px-4 py-2.5 rounded-xl font-bold text-sm shadow-md transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path></svg>
            <span>Create User</span>
          </button>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left mj-table">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Full Name</th>
                  <th>Role</th>
                  <th>Contact Number</th>
                  <th>Status</th>
                  <th class="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                ${users.map(u => `
                  <tr>
                    <td class="font-bold text-slate-800">${u.Username}</td>
                    <td class="font-semibold text-slate-700">${u.FullName}</td>
                    <td>
                      <span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-bold ${u.Role.includes('Admin') ? 'bg-red-50 text-[#B4182C]' : 'bg-slate-100 text-slate-700'}">
                        ${u.Role}
                      </span>
                    </td>
                    <td class="text-slate-600 font-mono text-xs">${u.ContactNumber || '—'}</td>
                    <td>
                      <span class="badge ${u.IsActive ? 'badge-completed' : 'badge-inactive'}">
                        ${u.IsActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td class="text-right space-x-1">
                      <button onclick="window.toggleUserStatus(${u.Id})" class="p-1.5 text-slate-600 hover:bg-slate-100 rounded-lg text-xs font-bold">
                        ${u.IsActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-add-user').addEventListener('click', openAddUserModal);
  }

  function openAddUserModal() {
    const modalHtml = `
      <div id="modal-add-user" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-200">
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <h3 class="font-bold text-lg">Create User Account</h3>
            <button onclick="document.getElementById('modal-add-user').remove()" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg">✕</button>
          </div>
          <form id="form-add-user" class="p-6 space-y-4">
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Username</label>
              <input type="text" id="usr-name" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-bold">
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Password</label>
              <input type="password" id="usr-pass" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Full Legal Name</label>
              <input type="text" id="usr-fullname" placeholder="e.g. Juan Dela Cruz" required class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">System Role</label>
                <select id="usr-role" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm font-medium">
                  <option value="Office Staff">Office Staff</option>
                  <option value="Owner/Admin">Owner/Admin</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-bold text-slate-600 uppercase mb-1">Contact Phone</label>
                <input type="text" id="usr-phone" placeholder="09XX-XXX-XXXX" class="w-full border border-slate-200 rounded-xl px-3 py-2 text-sm">
              </div>
            </div>
            <div class="flex justify-end space-x-3 pt-3 border-t border-slate-100">
              <button type="button" onclick="document.getElementById('modal-add-user').remove()" class="px-4 py-2 text-slate-600 font-semibold rounded-xl bg-slate-100 hover:bg-slate-200 text-sm">Cancel</button>
              <button type="submit" class="px-5 py-2 bg-[#B4182C] hover:bg-[#981323] text-white font-bold rounded-xl text-sm shadow-md">Create Account</button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);

    const form = document.getElementById('form-add-user');
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      const u = document.getElementById('usr-name').value.trim();
      const p = document.getElementById('usr-pass').value;
      const fn = document.getElementById('usr-fullname').value.trim();
      const role = document.getElementById('usr-role').value;
      const phone = document.getElementById('usr-phone').value.trim();

      const hashed = await auth.hashPassword(p);
      const record = {
        Username: u,
        Password: hashed,
        PlainPassword: p,
        FullName: fn,
        Role: role,
        ContactNumber: phone,
        IsActive: 1,
        CreatedAt: new Date().toISOString().substring(0, 19).replace('T', ' ')
      };

      store.insert('Users', record, auth.getCurrentUser());
      toast.show(`Created user ${u}`, 'success');
      document.getElementById('modal-add-user').remove();
      renderUserManagement();
    });
  }

  // --- SETTINGS VIEW ---
  function renderSettings() {
    mainContent.innerHTML = `
      <div class="space-y-6 max-w-4xl">
        <div>
          <h1 class="text-2xl font-black text-slate-800">System Customization & Settings</h1>
          <p class="text-sm text-slate-500">Database backup, factory seed reset, and appearance preferences</p>
        </div>

        <div class="bg-white rounded-2xl border border-slate-200/80 shadow-sm p-6 space-y-6">
          <div>
            <h3 class="font-bold text-slate-800 text-base mb-1">Database Backup & Portability</h3>
            <p class="text-xs text-slate-500 mb-4">Export the complete client database with all dispatches and records to JSON, or restore a backup file.</p>
            <div class="flex flex-wrap gap-3">
              <button id="btn-export-db-json" class="px-4 py-2 bg-slate-800 hover:bg-slate-900 text-white rounded-xl text-xs font-bold transition shadow-sm">
                Export Database JSON
              </button>
              <label class="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-xl text-xs font-bold cursor-pointer transition">
                Import Database JSON
                <input type="file" id="input-import-db" accept=".json" class="hidden">
              </label>
              <button id="btn-factory-reset" class="px-4 py-2 bg-red-50 hover:bg-red-100 text-[#B4182C] border border-red-200 rounded-xl text-xs font-bold transition">
                Reset to Original SQLite Database Seed
              </button>
            </div>
          </div>

          <div class="pt-6 border-t border-slate-100">
            <h3 class="font-bold text-slate-800 text-base mb-1">Company Information</h3>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm mt-3">
              <div class="p-4 bg-slate-50 rounded-xl border border-slate-200">
                <span class="text-xs font-bold text-slate-500 uppercase">Enterprise Name</span>
                <p class="font-black text-slate-800 mt-1">M.J. Billones Transport Service</p>
              </div>
              <div class="p-4 bg-slate-50 rounded-xl border border-slate-200">
                <span class="text-xs font-bold text-slate-500 uppercase">Primary Client</span>
                <p class="font-black text-slate-800 mt-1">Red Ribbon Bakeshop Inc.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    `;

    document.getElementById('btn-export-db-json').addEventListener('click', () => {
      const json = JSON.stringify(store.db, null, 2);
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `mj_billones_backup_${new Date().toISOString().substring(0,10)}.json`;
      a.click();
      toast.show('Exported database backup JSON!', 'success');
    });

    document.getElementById('input-import-db').addEventListener('change', (e) => {
      const file = e.target.files[0];
      if (!file) return;
      const reader = new FileReader();
      reader.onload = (evt) => {
        try {
          const parsed = JSON.parse(evt.target.result);
          store.db = parsed;
          store.save();
          toast.show('Database restored from JSON file successfully!', 'success');
          renderSettings();
        } catch (err) {
          alert('Invalid JSON file format.');
        }
      };
      reader.readAsText(file);
    });

    document.getElementById('btn-factory-reset').addEventListener('click', () => {
      if (confirm('Are you sure you want to restore the database to the initial SQLite dataset? Any new records created in this session will be refreshed.')) {
        store.resetToSeed();
        toast.show('Database reset to original seed!', 'info');
        renderSettings();
      }
    });
  }

  // Window global bridge methods for inline onclick attributes
  window.navigateTo = navigateTo;

  window.dispatchOrder = (scheduleId) => {
    openDispatchModal(scheduleId);
  };

  window.cancelOrder = (scheduleId) => {
    if (confirm('Are you sure you want to cancel this loading schedule?')) {
      store.update('LoadingSchedules', scheduleId, { Status: 'Cancelled' }, auth.getCurrentUser());
      toast.show('Loading schedule cancelled.', 'info');
      renderLoadingManagement();
    }
  };

  window.openCompleteTripModal = (deliveryId) => {
    openCompleteTripModal(deliveryId);
  };

  window.printDispatchSlip = (deliveryId) => {
    const delivery = store.getById('Deliveries', deliveryId);
    if (delivery) printer.printDispatchSlip(delivery);
  };

  window.printInvoiceDoc = (invoiceId) => {
    const invoice = store.getById('Invoices', invoiceId);
    if (invoice) printer.printInvoice(invoice);
  };

  window.printReceiptDoc = (receiptId) => {
    const receipt = store.getById('CollectionReceipts', receiptId);
    if (receipt) {
      const invoice = store.getById('Invoices', receipt.InvoiceId);
      printer.printOfficialReceipt(receipt, invoice);
    }
  };

  window.openRecordPaymentModal = (invoiceId) => {
    openRecordPaymentModal(invoiceId);
  };

  window.editTruckStatus = (truckId) => {
    const truck = store.getById('Trucks', truckId);
    if (!truck) return;
    const newStatus = prompt(`Set status for ${truck.PlateNumber} (Available, In Use, Under Maintenance, Inactive):`, truck.Status);
    if (newStatus && ['Available', 'In Use', 'Under Maintenance', 'Inactive'].includes(newStatus)) {
      store.update('Trucks', truckId, { Status: newStatus }, auth.getCurrentUser());
      toast.show(`Updated ${truck.PlateNumber} status to ${newStatus}`, 'success');
      renderTruckManagement();
    }
  };

  window.toggleUserStatus = (userId) => {
    const user = store.getById('Users', userId);
    if (!user) return;
    const newStatus = user.IsActive ? 0 : 1;
    store.update('Users', userId, { IsActive: newStatus }, auth.getCurrentUser());
    toast.show(`User ${user.Username} is now ${newStatus ? 'Active' : 'Inactive'}`, 'info');
    renderUserManagement();
  };

  // Kick off application
  init();
});
