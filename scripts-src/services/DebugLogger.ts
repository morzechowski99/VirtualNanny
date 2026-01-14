/**
 * DebugLogger - centralizowany system logowania
 */
export class DebugLogger {
  private static instance: DebugLogger;
  private logs: string[] = [];
  private maxLogs: number = 10;
  private isAndroid: boolean = false;

  private constructor() {
    this.isAndroid = typeof (window as any).Android !== 'undefined' || 
                     navigator.userAgent.includes('Android');
  }

  static getInstance(): DebugLogger {
    if (!DebugLogger.instance) {
      DebugLogger.instance = new DebugLogger();
    }
    return DebugLogger.instance;
  }

  log(message: string): void {
    const timestamp = new Date().toLocaleTimeString();
    const logMessage = `[VirtualNanny Camera] ${message}`;
    
    console.log(logMessage);

    // Na Androidzie przechowujemy logi dla debugowania
    if (this.isAndroid) {
      if (message.includes('error') || message.includes('Error') || message.includes('denied')) {
        this.logs.push(`${timestamp}: ${message}`);
        
        // Zapamiêtujemy tylko ostatnie 10 logów
        if (this.logs.length > this.maxLogs) {
          this.logs = this.logs.slice(-this.maxLogs);
        }
      }
    }
  }

  getLogs(): string[] {
    return this.logs;
  }

  clearLogs(): void {
    this.logs = [];
  }

  showAlert(): void {
    if (this.logs.length > 0) {
      alert('Android Debug Logs:\n' + this.logs.join('\n'));
    } else {
      alert('No debug logs available');
    }
  }

  showRecentStatus(): void {
    const logs: string[] = [];
    
    if (this.logs.length > 0) {
      logs.push('=== Recent Debug Logs ===');
      logs.push(...this.logs.slice(-5));
    }
    
    logs.push('=== Current Status ===');
    logs.push('URL: ' + window.location.href);
    logs.push('Secure Context: ' + window.isSecureContext);
    logs.push('MediaDevices: ' + !!navigator.mediaDevices);
    logs.push('getUserMedia: ' + !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia));
    logs.push('Video elements: ' + document.querySelectorAll('video').length);
    
    alert(logs.join('\n'));
  }

  checkEnvironment(): void {
    this.log('Location: ' + window.location.protocol + '//' + window.location.host);
    this.log('Is secure context: ' + window.isSecureContext);
    this.log('navigator.mediaDevices: ' + !!navigator.mediaDevices);
    this.log('getUserMedia: ' + !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia));
    this.log('MediaRecorder: ' + !!window.MediaRecorder);

    // Sprawdzenie uprawnieñ
    if (navigator.permissions) {
      navigator.permissions.query({ name: 'camera' as PermissionName }).then((result) => {
        this.log('Camera permission state: ' + result.state);
      }).catch((error) => {
        this.log('Error checking camera permission: ' + error.message);
      });

      navigator.permissions.query({ name: 'microphone' as PermissionName }).then((result) => {
        this.log('Microphone permission state: ' + result.state);
      }).catch((error) => {
        this.log('Error checking microphone permission: ' + error.message);
      });
    }

    // Lista dostêpnych urz¹dzeñ
    if (navigator.mediaDevices && navigator.mediaDevices.enumerateDevices) {
      navigator.mediaDevices.enumerateDevices().then((devices) => {
        this.log('Available media devices:');
        devices.forEach((device, index) => {
          this.log(`Device ${index}: ${device.kind} - ${device.label || 'No label'}`);
        });
      }).catch((error) => {
        this.log('Error enumerating devices: ' + error.message);
      });
    }
  }
}
