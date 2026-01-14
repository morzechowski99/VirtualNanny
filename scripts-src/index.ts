import { CameraManager } from './services/CameraManager';
import { RecordingManager } from './services/RecordingManager';
import { DebugLogger } from './services/DebugLogger';
import { PermissionManager } from './services/PermissionManager';

/**
 * VirtualNanny - g³ówna klasa aplikacji
 * Eksportuje interfejs publiczny dla aplikacji Blazor
 */
class VirtualNanny {
  private cameraManager: CameraManager;
  private recordingManager: RecordingManager;
  private logger: DebugLogger;
  private permissionManager: PermissionManager;

  constructor() {
    this.logger = DebugLogger.getInstance();
    this.cameraManager = new CameraManager();
    this.recordingManager = new RecordingManager();
    this.permissionManager = new PermissionManager();

    this.setupGlobalFunctions();
    this.setupPageUnloadHandler();
  }

  private setupGlobalFunctions(): void {
    // Eksportujemy funkcje dla kompatybilnoœci z istniej¹cym kodem
    (window as any).initializeCamera = (useFrontCamera: boolean) => this.initializeCamera(useFrontCamera);
    (window as any).switchCamera = (useFrontCamera: boolean) => this.switchCamera(useFrontCamera);
    (window as any).toggleRecording = (startRecording: boolean) => this.toggleRecording(startRecording);
    (window as any).debugLog = (message: string) => this.logger.log(message);
    (window as any).showAndroidDebug = () => this.logger.showAlert();
    (window as any).showRecentLogs = () => this.logger.showRecentStatus();
    
    // Dodatkowe funkcje diagnostyczne
    (window as any).checkMicrophonePermission = () => this.permissionManager.checkMicrophonePermission();
    (window as any).checkRecordingSupport = () => this.recordingManager.checkRecordingSupport();
    (window as any).showAvailableCameras = () => this.cameraManager.showAvailableCameras();
    (window as any).verifyCameraStream = () => this.cameraManager.verifyCameraStream();
    (window as any).testRecording = (durationSeconds: number) => this.testRecording(durationSeconds);
    (window as any).testAllPermissionMethods = () => this.permissionManager.testAllPermissionMethods();
    (window as any).checkDownloadCapabilities = () => this.checkDownloadCapabilities();
  }

  private setupPageUnloadHandler(): void {
    window.addEventListener('beforeunload', () => {
      this.logger.log('Page unloading, cleaning up');
      this.cameraManager.stopStream();
    });
  }

  async initializeCamera(useFrontCamera: boolean): Promise<boolean> {
    try {
      return await this.cameraManager.initialize(useFrontCamera);
    } catch (error) {
      this.logger.log('Camera initialization failed: ' + (error as Error).message);
      return false;
    }
  }

  async switchCamera(useFrontCamera: boolean): Promise<boolean> {
    try {
      return await this.cameraManager.switchCamera(useFrontCamera);
    } catch (error) {
      this.logger.log('Camera switch failed: ' + (error as Error).message);
      return false;
    }
  }

  async toggleRecording(startRecording: boolean): Promise<boolean> {
    try {
      const stream = this.cameraManager.getMediaStream();
      if (!stream) {
        throw new Error('No active camera stream');
      }
      return await this.recordingManager.toggleRecording(startRecording, stream);
    } catch (error) {
      this.logger.log('Recording toggle failed: ' + (error as Error).message);
      throw error;
    }
  }

  async testRecording(durationSeconds: number = 5): Promise<boolean> {
    try {
      const stream = this.cameraManager.getMediaStream();
      if (!stream) {
        throw new Error('No active camera stream');
      }
      return await this.recordingManager.testRecording(durationSeconds, stream);
    } catch (error) {
      this.logger.log('Test recording failed: ' + (error as Error).message);
      return false;
    }
  }

  private checkDownloadCapabilities(): void {
    const info: string[] = [];
    
    info.push('=== Download Capabilities Check ===');
    info.push('Platform: ' + navigator.platform);
    info.push('User Agent: ' + navigator.userAgent.substring(0, 100) + '...');
    info.push('Is Android: ' + (navigator.userAgent.includes('Android') || typeof (window as any).Android !== 'undefined'));
    
    info.push('File System Access API: ' + ('showSaveFilePicker' in window ? 'YES' : 'NO'));
    info.push('Web Share API: ' + ('share' in navigator ? 'YES' : 'NO'));
    
    if ('share' in navigator) {
      info.push('Can Share Files: ' + ('canShare' in navigator ? 'YES' : 'NO'));
    }
    
    info.push('Blob URLs supported: ' + ('URL' in window && 'createObjectURL' in URL ? 'YES' : 'NO'));
    info.push('Download attribute supported: ' + ('download' in document.createElement('a') ? 'YES' : 'NO'));
    
    if ('storage' in navigator && 'estimate' in navigator.storage) {
      (navigator.storage as any).estimate().then((estimate: any) => {
        const storage = estimate.quota ? (estimate.quota / (1024 * 1024 * 1024)).toFixed(2) + ' GB' : 'Unknown';
        const used = estimate.usage ? (estimate.usage / (1024 * 1024)).toFixed(2) + ' MB' : 'Unknown';
        alert(info.join('\n') + '\n\nStorage Quota: ' + storage + '\nStorage Used: ' + used);
      }).catch(() => {
        alert(info.join('\n') + '\n\nStorage info: Not available');
      });
    } else {
      info.push('Storage API: NO');
      alert(info.join('\n'));
    }
  }
}

// Inicjalizujemy aplikacjê po za³adowaniu DOM
let virtualNannyInstance: VirtualNanny | null = null;

window.addEventListener('DOMContentLoaded', () => {
  virtualNannyInstance = new VirtualNanny();
  const logger = DebugLogger.getInstance();
  
  logger.log('DOM loaded, VirtualNanny initialized');
  logger.checkEnvironment();
  
  // Eksportujemy instancjê na window
  (window as any).VirtualNanny = virtualNannyInstance;
});

export default VirtualNanny;
