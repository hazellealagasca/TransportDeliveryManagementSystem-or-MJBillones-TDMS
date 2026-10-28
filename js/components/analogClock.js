/**
 * Interactive Analog Clock Time Picker Component
 * Matches the WinForms AnalogClockTimePickerDialog functionality
 */

class AnalogClockPicker {
  constructor() {
    this.modal = null;
    this.targetInput = null;
    this.hour = 8;
    this.minute = 0;
    this.period = 'AM';
    this.mode = 'hour'; // 'hour' or 'minute'
    this.init();
  }

  init() {
    // Create modal DOM structure
    const modalHtml = `
      <div id="analog-clock-modal" class="fixed inset-0 z-50 flex items-center justify-center modal-overlay hidden">
        <div class="bg-white rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden transform transition-all border border-slate-200">
          <!-- Header -->
          <div class="bg-[#B4182C] px-6 py-4 text-white flex items-center justify-between">
            <div class="flex items-center space-x-2">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
              <h3 class="font-bold text-lg">Select Time (Analog Clock)</h3>
            </div>
            <button id="clock-close-btn" class="text-white hover:bg-[#8B1323] p-1.5 rounded-lg transition-colors">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
            </button>
          </div>

          <!-- Time Display & Mode Switches -->
          <div class="p-6">
            <div class="flex items-center justify-center space-x-3 mb-6 bg-slate-50 p-3 rounded-xl border border-slate-200">
              <div class="flex items-center text-4xl font-extrabold text-slate-800 tracking-tight">
                <button id="clock-btn-hour" class="px-2 py-1 rounded-lg text-[#B4182C] bg-red-50 hover:bg-red-100 transition">08</button>
                <span class="text-slate-400 mx-1">:</span>
                <button id="clock-btn-minute" class="px-2 py-1 rounded-lg text-slate-600 hover:bg-slate-200 transition">00</button>
              </div>
              <div class="flex flex-col space-y-1">
                <button id="clock-period-am" class="px-2.5 py-1 text-xs font-bold rounded-md bg-[#B4182C] text-white">AM</button>
                <button id="clock-period-pm" class="px-2.5 py-1 text-xs font-bold rounded-md bg-slate-200 text-slate-700 hover:bg-slate-300">PM</button>
              </div>
            </div>

            <!-- Clock Face -->
            <div id="clock-interactive-face" class="clock-face relative mb-6">
              <div class="clock-center-dot"></div>
              <div id="clock-hand-hour" class="clock-hand hour"></div>
              <div id="clock-hand-minute" class="clock-hand minute"></div>
              <!-- Number markers rendered dynamically -->
              <div id="clock-dial-numbers" class="w-full h-full relative"></div>
            </div>

            <!-- Quick Presets -->
            <div class="grid grid-cols-4 gap-2 mb-6 text-xs">
              <button class="clock-quick-btn py-1.5 px-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg font-medium" data-time="01:00 AM">01:00 AM</button>
              <button class="clock-quick-btn py-1.5 px-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg font-medium" data-time="08:00 AM">08:00 AM</button>
              <button class="clock-quick-btn py-1.5 px-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg font-medium" data-time="01:00 PM">01:00 PM</button>
              <button class="clock-quick-btn py-1.5 px-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg font-medium" data-time="08:00 PM">08:00 PM</button>
            </div>

            <!-- Action Buttons -->
            <div class="flex items-center space-x-3">
              <button id="clock-cancel-btn" class="w-1/2 py-2.5 px-4 rounded-xl font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 transition">Cancel</button>
              <button id="clock-confirm-btn" class="w-1/2 py-2.5 px-4 rounded-xl font-semibold text-white bg-[#B4182C] hover:bg-[#981323] transition shadow-md shadow-red-200">Confirm Time</button>
            </div>
          </div>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    this.modal = document.getElementById('analog-clock-modal');
    this.renderDialNumbers();
    this.bindEvents();
  }

  renderDialNumbers() {
    const dial = document.getElementById('clock-dial-numbers');
    dial.innerHTML = '';
    const radius = 88; // radius in px from center (110, 110)
    const centerX = 110;
    const centerY = 110;

    for (let i = 1; i <= 12; i++) {
      const angle = (i * 30 - 90) * (Math.PI / 180);
      const x = centerX + radius * Math.cos(angle);
      const y = centerY + radius * Math.sin(angle);

      const numEl = document.createElement('div');
      numEl.className = 'clock-number cursor-pointer hover:text-[#B4182C]';
      numEl.innerText = i;
      numEl.style.left = `${x}px`;
      numEl.style.top = `${y}px`;
      numEl.addEventListener('click', (e) => {
        e.stopPropagation();
        if (this.mode === 'hour') {
          this.hour = i;
          this.mode = 'minute';
        } else {
          this.minute = (i === 12 ? 0 : i * 5);
        }
        this.updateClockUI();
      });
      dial.appendChild(numEl);
    }
  }

  bindEvents() {
    const closeBtn = document.getElementById('clock-close-btn');
    const cancelBtn = document.getElementById('clock-cancel-btn');
    const confirmBtn = document.getElementById('clock-confirm-btn');
    const btnHour = document.getElementById('clock-btn-hour');
    const btnMinute = document.getElementById('clock-btn-minute');
    const btnAm = document.getElementById('clock-period-am');
    const btnPm = document.getElementById('clock-period-pm');
    const clockFace = document.getElementById('clock-interactive-face');

    closeBtn.addEventListener('click', () => this.close());
    cancelBtn.addEventListener('click', () => this.close());

    confirmBtn.addEventListener('click', () => {
      if (this.targetInput) {
        this.targetInput.value = this.getFormattedTime();
        this.targetInput.dispatchEvent(new Event('change'));
      }
      this.close();
    });

    btnHour.addEventListener('click', () => {
      this.mode = 'hour';
      this.updateClockUI();
    });

    btnMinute.addEventListener('click', () => {
      this.mode = 'minute';
      this.updateClockUI();
    });

    btnAm.addEventListener('click', () => {
      this.period = 'AM';
      this.updateClockUI();
    });

    btnPm.addEventListener('click', () => {
      this.period = 'PM';
      this.updateClockUI();
    });

    // Handle clicking anywhere on clock face to rotate hand
    clockFace.addEventListener('click', (e) => {
      const rect = clockFace.getBoundingClientRect();
      const clickX = e.clientX - rect.left - 110;
      const clickY = e.clientY - rect.top - 110;

      let angle = Math.atan2(clickY, clickX) * (180 / Math.PI) + 90;
      if (angle < 0) angle += 360;

      if (this.mode === 'hour') {
        let h = Math.round(angle / 30);
        if (h === 0) h = 12;
        this.hour = h;
        this.mode = 'minute';
      } else {
        let m = Math.round(angle / 6);
        if (m === 60) m = 0;
        this.minute = m;
      }
      this.updateClockUI();
    });

    // Preset buttons
    document.querySelectorAll('.clock-quick-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const timeStr = btn.getAttribute('data-time');
        this.parseTime(timeStr);
        this.updateClockUI();
      });
    });
  }

  open(inputElement) {
    this.targetInput = inputElement;
    if (inputElement && inputElement.value) {
      this.parseTime(inputElement.value);
    } else {
      this.parseTime('08:00 AM');
    }
    this.mode = 'hour';
    this.updateClockUI();
    this.modal.classList.remove('hidden');
  }

  close() {
    this.modal.classList.add('hidden');
    this.targetInput = null;
  }

  parseTime(str) {
    if (!str) return;
    const match = str.trim().match(/(\d+):(\d+)\s*(AM|PM)?/i);
    if (match) {
      this.hour = parseInt(match[1], 10);
      this.minute = parseInt(match[2], 10);
      this.period = match[3] ? match[3].toUpperCase() : 'AM';
    }
  }

  getFormattedTime() {
    const h = String(this.hour).padStart(2, '0');
    const m = String(this.minute).padStart(2, '0');
    return `${h}:${m} ${this.period}`;
  }

  updateClockUI() {
    const btnHour = document.getElementById('clock-btn-hour');
    const btnMinute = document.getElementById('clock-btn-minute');
    const btnAm = document.getElementById('clock-period-am');
    const btnPm = document.getElementById('clock-period-pm');
    const handHour = document.getElementById('clock-hand-hour');
    const handMinute = document.getElementById('clock-hand-minute');

    btnHour.innerText = String(this.hour).padStart(2, '0');
    btnMinute.innerText = String(this.minute).padStart(2, '0');

    if (this.mode === 'hour') {
      btnHour.className = 'px-2 py-1 rounded-lg text-[#B4182C] bg-red-50 ring-2 ring-[#B4182C] font-bold';
      btnMinute.className = 'px-2 py-1 rounded-lg text-slate-600 hover:bg-slate-200 transition';
    } else {
      btnMinute.className = 'px-2 py-1 rounded-lg text-[#B4182C] bg-red-50 ring-2 ring-[#B4182C] font-bold';
      btnHour.className = 'px-2 py-1 rounded-lg text-slate-600 hover:bg-slate-200 transition';
    }

    if (this.period === 'AM') {
      btnAm.className = 'px-2.5 py-1 text-xs font-bold rounded-md bg-[#B4182C] text-white';
      btnPm.className = 'px-2.5 py-1 text-xs font-bold rounded-md bg-slate-200 text-slate-700 hover:bg-slate-300';
    } else {
      btnPm.className = 'px-2.5 py-1 text-xs font-bold rounded-md bg-[#B4182C] text-white';
      btnAm.className = 'px-2.5 py-1 text-xs font-bold rounded-md bg-slate-200 text-slate-700 hover:bg-slate-300';
    }

    // Calculate rotation angles
    const hourAngle = (this.hour % 12) * 30 + (this.minute / 60) * 30;
    const minAngle = this.minute * 6;

    handHour.style.transform = `rotate(${hourAngle}deg)`;
    handMinute.style.transform = `rotate(${minAngle}deg)`;
  }
}

window.analogClockPicker = new AnalogClockPicker();
