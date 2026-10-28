/**
 * Toast Notifications Component
 */

class ToastService {
  constructor() {
    this.container = null;
    this.init();
  }

  init() {
    let el = document.getElementById('toast-container');
    if (!el) {
      el = document.createElement('div');
      el.id = 'toast-container';
      el.className = 'fixed bottom-5 right-5 z-50 flex flex-col space-y-2 pointer-events-none';
      document.body.appendChild(el);
    }
    this.container = el;
  }

  show(message, type = 'success', duration = 3500) {
    const toast = document.createElement('div');
    toast.className = 'pointer-events-auto transform transition-all duration-300 ease-out translate-y-4 opacity-0 flex items-center p-3.5 rounded-xl shadow-xl border text-sm max-w-sm';

    let bg = 'bg-white text-slate-800 border-slate-200';
    let icon = `<svg class="w-5 h-5 text-emerald-500 mr-2.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`;

    if (type === 'error') {
      bg = 'bg-white text-slate-800 border-red-200';
      icon = `<svg class="w-5 h-5 text-red-500 mr-2.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>`;
    } else if (type === 'warning') {
      bg = 'bg-white text-slate-800 border-amber-200';
      icon = `<svg class="w-5 h-5 text-amber-500 mr-2.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path></svg>`;
    } else if (type === 'info') {
      bg = 'bg-white text-slate-800 border-blue-200';
      icon = `<svg class="w-5 h-5 text-blue-500 mr-2.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>`;
    }

    toast.className += ` ${bg}`;
    toast.innerHTML = `${icon}<span class="font-medium">${message}</span>`;
    this.container.appendChild(toast);

    // Animate in
    setTimeout(() => {
      toast.classList.remove('translate-y-4', 'opacity-0');
      toast.classList.add('translate-y-0', 'opacity-100');
    }, 10);

    // Auto dismiss
    setTimeout(() => {
      toast.classList.add('opacity-0', 'translate-y-2');
      setTimeout(() => toast.remove(), 300);
    }, duration);
  }
}

window.toast = new ToastService();
