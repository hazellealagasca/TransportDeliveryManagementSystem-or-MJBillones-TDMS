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
        <div class="bg-[#F8F7F6] rounded-sm shadow-2xl w-full max-w-[290px] overflow-hidden border border-slate-400">
          <!-- Window Header -->
          <div class="bg-[#F1F1F1] px-3 py-2 flex items-center justify-between border-b border-slate-200">
            <span class="text-xs text-slate-500 font-normal">Select Departure Time</span>
            <button id="clock-close-x" class="text-slate-400 hover:text-slate-800 text-sm leading-none">✕</button>
          </div>

          <!-- Crimson Header Display -->
          <div class="bg-[#B4182C] px-6 py-3.5 text-white flex items-center justify-between">
            <div class="flex items-center text-[42px] font-bold tracking-tight leading-none">
              <span id="clock-disp-hour" class="cursor-pointer hover:opacity-90">08</span>
              <span class="opacity-75 mx-1.5">:</span>
              <span id="clock-disp-minute" class="cursor-pointer opacity-75 hover:opacity-100">00</span>
            </div>
            <div class="flex flex-col text-xs font-bold overflow-hidden space-y-1">
              <button id="clock-set-am" class="px-3 py-1 bg-white text-[#B4182C]">AM</button>
              <button id="clock-set-pm" class="px-3 py-1 bg-[#7F1D1D] text-white">PM</button>
            </div>
          </div>

          <!-- Dial Body -->
          <div class="p-4 bg-[#F8F7F6]">
            <div id="clock-dial-face" class="w-[210px] h-[210px] rounded-full bg-[#FAF9F8] border-[1.5px] border-[#D6D3D1] relative mx-auto cursor-pointer">
              <div class="w-3 h-3 bg-[#B4182C] rounded-full absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 z-10 pointer-events-none flex items-center justify-center">
                <div class="w-1 h-1 bg-white rounded-full"></div>
              </div>
              <div id="clock-pointer-line" class="absolute top-1/2 left-1/2 w-[2.5px] bg-[#B4182C] origin-top pointer-events-none z-10" style="height: 74px; transform: rotate(240deg);">
                <div id="clock-hand-dot" class="w-8 h-8 rounded-full bg-[#B4182C] text-white text-[13px] font-bold flex items-center justify-center absolute -bottom-4 left-1/2 -translate-x-1/2">
                  8
                </div>
              </div>
              <div id="clock-dial-hours" class="w-full h-full relative"></div>
            </div>

            <!-- Quick Presets Strip -->
            <div class="flex items-center space-x-1.5 overflow-x-auto mt-3 pb-1">
              <button type="button" class="clock-preset px-2.5 py-1 text-[11px] bg-white border border-slate-300 rounded text-slate-700 hover:bg-slate-100 whitespace-nowrap" data-h="6" data-m="0" data-p="AM">06:00 AM</button>
              <button type="button" class="clock-preset px-2.5 py-1 text-[11px] bg-white border border-slate-300 rounded text-slate-700 hover:bg-slate-100 whitespace-nowrap" data-h="8" data-m="0" data-p="AM">08:00 AM</button>
              <button type="button" class="clock-preset px-2.5 py-1 text-[11px] bg-white border border-slate-300 rounded text-slate-700 hover:bg-slate-100 whitespace-nowrap" data-h="12" data-m="0" data-p="PM">12:00 PM</button>
              <button type="button" class="clock-preset px-2.5 py-1 text-[11px] bg-white border border-slate-300 rounded text-slate-700 hover:bg-slate-100 whitespace-nowrap" data-h="2" data-m="0" data-p="PM">02:00 PM</button>
            </div>

            <!-- Footer Buttons -->
            <div class="flex items-center justify-end space-x-2 mt-3 pt-2 border-t border-slate-200">
              <button id="clock-btn-cancel" class="px-4 py-1.5 text-[13px] font-medium text-slate-800 bg-[#EFEFEF] hover:bg-slate-200 rounded-sm">
                CANCEL
              </button>
              <button id="clock-btn-set" class="px-5 py-1.5 text-[13px] font-medium text-white bg-[#B4182C] hover:bg-[#981323] rounded-sm">
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
    const radius = 74;
    const center = 105;

    for (let i = 1; i <= 12; i++) {
      const angle = (i * 30 - 90) * (Math.PI / 180);
      const x = center + radius * Math.cos(angle);
      const y = center + radius * Math.sin(angle);

      const num = document.createElement('div');
      num.className = 'clock-num text-[14px] font-semibold text-slate-900 hover:text-[#B4182C]';
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

    document.querySelectorAll('.clock-preset').forEach(btn => {
      btn.addEventListener('click', () => {
        this.hour = parseInt(btn.getAttribute('data-h'), 10);
        this.minute = parseInt(btn.getAttribute('data-m'), 10);
        this.period = btn.getAttribute('data-p');
        this.updateUI();
      });
    });

    // Clicking anywhere on dial face
    const dial = document.getElementById('clock-dial-face');
    dial.addEventListener('click', (e) => {
      const rect = dial.getBoundingClientRect();
      const x = e.clientX - rect.left - 105;
      const y = e.clientY - rect.top - 105;
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
      btnAm.className = 'px-3 py-1 bg-white text-[#B4182C] font-bold';
      btnPm.className = 'px-3 py-1 bg-[#7F1D1D] text-white font-medium hover:bg-[#6B0F1B]';
    } else {
      btnPm.className = 'px-3 py-1 bg-white text-[#B4182C] font-bold';
      btnAm.className = 'px-3 py-1 bg-[#7F1D1D] text-white font-medium hover:bg-[#6B0F1B]';
    }

    const angle = (this.hour % 12) * 30 + 180;
    const line = document.getElementById('clock-pointer-line');
    line.style.transform = `rotate(${angle}deg)`;
    document.getElementById('clock-hand-dot').innerText = this.hour;
  }
}

window.analogClockPicker = new AnalogClockPicker();
