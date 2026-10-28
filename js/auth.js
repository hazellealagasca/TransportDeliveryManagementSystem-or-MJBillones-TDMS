/**
 * Authentication & Session Management
 */

class AuthService {
  constructor() {
    this.SESSION_KEY = 'mj_billones_session';
    this.currentUser = null;
    this.init();
  }

  init() {
    try {
      const stored = sessionStorage.getItem(this.SESSION_KEY);
      if (stored) {
        this.currentUser = JSON.parse(stored);
      }
    } catch (e) {
      this.currentUser = null;
    }
  }

  getCurrentUser() {
    return this.currentUser;
  }

  isAdmin() {
    if (!this.currentUser) return false;
    const role = (this.currentUser.Role || '').toLowerCase();
    return role.includes('admin') || role.includes('owner');
  }

  async hashPassword(password) {
    const enc = new TextEncoder();
    const hashBuf = await crypto.subtle.digest('SHA-256', enc.encode(password));
    const hashArr = Array.from(new Uint8Array(hashBuf));
    return hashArr.map(b => b.toString(16).padStart(2, '0')).join('');
  }

  async login(username, password) {
    const users = window.appStore.get('Users');
    const user = users.find(u => u.Username.toLowerCase() === username.trim().toLowerCase());

    if (!user) {
      throw new Error('Invalid username or password.');
    }

    if (user.IsActive === 0) {
      throw new Error('This account has been deactivated. Please contact an administrator.');
    }

    const hashedInput = await this.hashPassword(password);
    const plainMatch = (user.PlainPassword && user.PlainPassword === password);
    const hashMatch = (user.Password && (user.Password === hashedInput || user.Password === password));

    if (!plainMatch && !hashMatch) {
      throw new Error('Invalid username or password.');
    }

    // Set active session
    this.currentUser = {
      Id: user.Id,
      Username: user.Username,
      Role: user.Role,
      FullName: user.FullName,
      ContactNumber: user.ContactNumber
    };

    sessionStorage.setItem(this.SESSION_KEY, JSON.stringify(this.currentUser));

    // Audit log
    window.appStore.logAudit(this.currentUser, 'LOGIN', 'Users', user.Id, `User ${user.Username} logged in successfully.`);

    return this.currentUser;
  }

  logout() {
    if (this.currentUser) {
      window.appStore.logAudit(this.currentUser, 'LOGOUT', 'Users', this.currentUser.Id, `User ${this.currentUser.Username} logged out.`);
    }
    this.currentUser = null;
    sessionStorage.removeItem(this.SESSION_KEY);
  }

  async resetPasswordWithSecurityAnswer(username, answer, newPassword) {
    const users = window.appStore.get('Users');
    const user = users.find(u => u.Username.toLowerCase() === username.trim().toLowerCase());

    if (!user) throw new Error('User not found.');
    if (!user.SecurityAnswer) throw new Error('No security answer set for this user.');

    if (user.SecurityAnswer.trim().toLowerCase() !== answer.trim().toLowerCase()) {
      throw new Error('Security answer does not match our records.');
    }

    const newHash = await this.hashPassword(newPassword);
    window.appStore.update('Users', user.Id, {
      Password: newHash,
      PlainPassword: newPassword
    }, 'SecurityReset');

    return true;
  }
}

window.auth = new AuthService();
