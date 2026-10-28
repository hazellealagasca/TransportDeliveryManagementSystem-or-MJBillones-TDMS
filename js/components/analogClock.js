/**
 * Exact Analog Clock Departure Time Picker
 * Replicates 05 - Departure Time Picker.png
 */

class AnalogClockPicker {
  constructor() {
    this.modal = null;
    this.targetInput = null;
    this.hour = 8;
    this.minute = 0;
    this.period = 'AM';
    this.mode = 'hour'; // 'hour' or 'minute'
    this.onSelectedCallback = null;
    this.init();
  }

  init() {
    const modalHtml = `
      <div id="analog-clock-modal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 hidden select-none">
        <div class="bg-white rounded-md shadow-2xl w-full max-w-[280px] overflow-hidden border border-slate-300">
          <!-- Window Header -->
          <div class="bg-white px-3 py-1.5 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-600 font-medium">Select Departure Time</span>
            <button id="clock-close-x" class="text-slate-400 hover:text-slate-700 text-sm leading-none">✕</button>
          </div>

          <!-- Crimson Header Display -->
          <div class="bg-[#B4182C] px-5 py-3 text-white flex items-center justify-between">
            <div class="flex items-center text-4xl font-bold tracking-tight">
              <span id="clock-disp-hour" class="cursor-pointer hover:opacity-90">08</span>
              <span class="opacity-70 mx-1">:</span>
              <span id="clock-disp-minute" class="cursor-pointer opacity-70 hover:opacity-100">00</span>
            </div>
            <div class="flex flex-col text-xs font-bold rounded overflow-hidden border border-white/30">
              <button id="clock-set-am" class="px-2 py-0.5 bg-white text-[#B4182C]">AM</button>
              <button id="clock-set-pm" class="px-2 py-0.5 bg-[#8B1323] text-white">PM</button>
            </div>
          </div>

          <!-- Dial Body -->
          <div class="p-3 bg-white">
            <div id="clock-dial-face" class="w-[200px] h-[200px] rounded-full bg-slate-50 border border-slate-200 relative mx-auto cursor-pointer">
              <div class="w-2.5 h-2.5 bg-[#B4182C] rounded-full absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-10 pointer-events-none"></div>
              <div id="clock-pointer-line" class="absolute top-1/2 left-1/2 w-0.5 bg-[#B4182C] origin-top pointer-events-none z-10" style="height: 70px; transform: rotate(240deg);">
                <div id="clock-hand-dot" class="w-6 h-6 rounded-full bg-[#B4182C] text-white text-[11px] font-bold flex items-center justify-center absolute -bottom-3 left-1/2 -translate-x-1/2">
                  8
                </div>
              </div>
              <div id="clock-dial-hours" class="w-full h-full relative"></div>
            </div>

            <!-- Footer Buttons -->
            <div class="flex items-center justify-end space-x-2 mt-4 pt-2 border-t border-slate-100">
              <button id="clock-btn-cancel" class="px-4 py-1.5 text-xs font-bold text-slate-700 hover:bg-slate-100 rounded">
                CANCEL
              </button>
              <button id="clock-btn-set" class="px-4 py-1.5 text-xs font-bold text-white bg-[#B4182C] hover:bg-[#981323] rounded">
                SET TIME
              </button>
            </div>
          </div>
        </div>
      </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHtml);
    this.modal = document.getElementById('analog-clock-modal');
    this.renderHours();
    this.bindEvents();
  }

  renderHours() {
    const container = document.getElementById('clock-dial-hours');
    container.innerHTML = '';
    const radius = 72;
    const center = 100;

    for (let i = 1; i <= 12; i++) {
      const angle = (i * 30 - 90) * (Math.PI / 180);
      const x = center + radius * Math.cos(angle);
      const y = center + radius * Math.sin(angle);

      const num = document.createElement('div');
      num.className = 'clock-num text-xs font-semibold text-slate-700 hover:text-[#B4182C]';
      num.innerText = i;
      num.style.left = `${x}px`;
      num.style.top = `${y}px`;
      num.addEventListener('click', (e) => {
        e.stopPropagation();
        this.hour = i;
        this.updateUI();
      });
      container.appendChild(num);
    }
  }

  bindEvents() {
    document.getElementById('clock-close-x').addEventListener('click', () => this.close());
    document.getElementById('clock-btn-cancel').addEventListener('click', () => this.close());

    document.getElementById('clock-btn-set').addEventListener('click', () => {
      const timeStr = this.getFormattedTime();
      if (this.targetInput) {
        this.targetInput.value = timeStr;
        this.targetInput.dispatchEvent(new Event('change'));
      }
      if (this.onSelectedCallback) {
        this.onSelectedCallback(timeStr);
      }
      this.close();
    });

    document.getElementById('clock-set-am').addEventListener('click', () => {
      this.period = 'AM';
      this.updateUI();
    });

    document.getElementById('clock-set-pm').addEventListener('click', () => {
      this.period = 'PM';
      this.updateUI();
    });

    // Clicking anywhere on dial face
    const dial = document.getElementById('clock-dial-face');
    dial.addEventListener('click', (e) => {
      const rect = dial.getBoundingClientRect();
      const x = e.clientX - rect.left - 100;
      const y = e.clientY - rect.top - 100;
      let angle = Math.atan2(y, x) * (180 / Math.PI) + 90;
      if (angle < 0) angle += 360;

      let h = Math.round(angle / 30);
      if (h === 0) h = 12;
      this.hour = h;
      this.updateUI();
    });
  }

  open(inputElement, callback = null) {
    this.targetInput = inputElement;
    this.onSelectedCallback = callback;
    if (inputElement && inputElement.value) {
      this.parseTime(inputElement.value);
    }
    this.updateUI();
    this.modal.classList.remove('hidden');
  }

  close() {
    this.modal.classList.add('hidden');
  }

  parseTime(str) {
    const match = str.trim().match(/(\d+):(\d+)\s*(AM|PM)?/i);
    if (match) {
      this.hour = parseInt(match[1], 10) || 8;
      this.minute = parseInt(match[2], 10) || 0;
      this.period = match[3] ? match[3].toUpperCase() : 'AM';
    }
  }

  getFormattedTime() {
    const h = String(this.hour).padStart(2, '0');
    const m = String(this.minute).padStart(2, '0');
    return `${h}:${m} ${this.period}`;
  }

  updateUI() {
    document.getElementById('clock-disp-hour').innerText = String(this.hour).padStart(2, '0');
    document.getElementById('clock-disp-minute').innerText = String(this.minute).padStart(2, '0');

    const btnAm = document.getElementById('clock-set-am');
    const btnPm = document.getElementById('clock-set-pm');

    if (this.period === 'AM') {
      btnAm.className = 'px-2 py-0.5 bg-white text-[#B4182C] font-bold';
      btnPm.className = 'px-2 py-0.5 bg-[#8B1323] text-white font-medium hover:bg-[#720E1C]';
    } else {
      btnPm.className = 'px-2 py-0.5 bg-white text-[#B4182C] font-bold';
      btnAm.className = 'px-2 py-0.5 bg-[#8B1323] text-white font-medium hover:bg-[#720E1C]';
    }

    const angle = (this.hour % 12) * 30 + 180;
    const line = document.getElementById('clock-pointer-line');
    line.style.transform = `rotate(${angle}deg)`;
    document.getElementById('clock-hand-dot').innerText = this.hour;
  }
}

window.analogClockPicker = new AnalogClockPicker();
