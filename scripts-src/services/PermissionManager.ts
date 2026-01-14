import { DebugLogger } from './DebugLogger';

/**
 * PermissionManager - zarz¹dzanie uprawnieniami dostêpu do kamery i mikrofonu
 */
export class PermissionManager {
  private logger: DebugLogger;

  constructor() {
    this.logger = DebugLogger.getInstance();
  }

  async checkMicrophonePermission(): Promise<boolean> {
    try {
      if (navigator.permissions) {
        const result = await navigator.permissions.query({ name: 'microphone' as PermissionName });
        this.logger.log('Microphone permission check result: ' + result.state);
        
        if (result.state === 'denied') {
          return false;
        }
      }
      
      // Próba dostêpu do mikrofonu
      const testStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      testStream.getTracks().forEach(track => track.stop());
      return true;
    } catch (error) {
      this.logger.log('Microphone permission error: ' + (error as Error).message);
      return false;
    }
  }

  async forcePermissionRequest(): Promise<boolean> {
    try {
      this.logger.log('Forcing permission request...');
      
      const stream = await navigator.mediaDevices.getUserMedia({
        video: true,
        audio: false
      });
      
      this.logger.log('Permission granted! Got stream with tracks: ' + stream.getTracks().length);
      stream.getTracks().forEach(track => track.stop());
      
      return true;
    } catch (error) {
      this.logger.log('Permission request failed: ' + (error as Error).message);
      return false;
    }
  }

  async testAllPermissionMethods(): Promise<boolean> {
    const results: string[] = [];
    
    // Metoda 1: Direct getUserMedia
    try {
      this.logger.log('Testing Method 1: Direct getUserMedia...');
      const stream1 = await navigator.mediaDevices.getUserMedia({ video: true });
      stream1.getTracks().forEach(track => track.stop());
      results.push('Method 1: Direct getUserMedia - SUCCESS');
      return true;
    } catch (error) {
      results.push('Method 1: Direct getUserMedia - FAILED: ' + (error as Error).message);
    }
    
    // Metoda 2: Enumerate devices first
    try {
      this.logger.log('Testing Method 2: Enumerate devices first...');
      const devices = await navigator.mediaDevices.enumerateDevices();
      const videoDevices = devices.filter(d => d.kind === 'videoinput');
      if (videoDevices.length > 0) {
        const stream2 = await navigator.mediaDevices.getUserMedia({ 
          video: { deviceId: videoDevices[0].deviceId } 
        });
        stream2.getTracks().forEach(track => track.stop());
        results.push('Method 2: Enumerate devices - SUCCESS');
        return true;
      } else {
        results.push('Method 2: Enumerate devices - NO VIDEO DEVICES FOUND');
      }
    } catch (error) {
      results.push('Method 2: Enumerate devices - FAILED: ' + (error as Error).message);
    }
    
    // Metoda 3: Minimal constraints
    try {
      this.logger.log('Testing Method 3: Minimal constraints...');
      const stream3 = await navigator.mediaDevices.getUserMedia({ 
        video: { width: 320, height: 240 } 
      });
      stream3.getTracks().forEach(track => track.stop());
      results.push('Method 3: Minimal constraints - SUCCESS');
      return true;
    } catch (error) {
      results.push('Method 3: Minimal constraints - FAILED: ' + (error as Error).message);
    }
    
    alert('Permission Test Results:\n\n' + results.join('\n'));
    return false;
  }

  async enhancedPermissionRequest(): Promise<boolean> {
    try {
      this.logger.log('Starting enhanced permission request...');
      
      // Metoda 1: Direct audio + video
      try {
        this.logger.log('Method 1: Direct audio+video request...');
        const stream1 = await navigator.mediaDevices.getUserMedia({ 
          video: { 
            facingMode: 'environment',
            width: { ideal: 640 },
            height: { ideal: 480 }
          }, 
          audio: true 
        });
        
        setTimeout(() => {
          stream1.getTracks().forEach(track => track.stop());
          this.logger.log('Method 1: SUCCESS - Enhanced permissions granted!');
        }, 1000);
        
        return true;
      } catch (error) {
        this.logger.log('Method 1 failed: ' + (error as Error).message);
      }
      
      // Metoda 2: Video first, then audio
      try {
        this.logger.log('Method 2: Video first, then audio...');
        const videoStream = await navigator.mediaDevices.getUserMedia({ video: true });
        this.logger.log('Video permission granted, now requesting audio...');
        
        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        this.logger.log('Audio permission also granted!');
        
        videoStream.getTracks().forEach(track => track.stop());
        audioStream.getTracks().forEach(track => track.stop());
        
        this.logger.log('Method 2: SUCCESS - Separate permissions granted!');
        return true;
      } catch (error) {
        this.logger.log('Method 2 failed: ' + (error as Error).message);
      }
      
      return false;
    } catch (error) {
      this.logger.log('Enhanced permission request error: ' + (error as Error).message);
      return false;
    }
  }

  async triggerWebViewPermissions(): Promise<boolean> {
    this.logger.log('Triggering WebView permission dialog...');
    
    const userEvent = new Event('click', { bubbles: true });
    document.dispatchEvent(userEvent);
    
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ 
        video: true,
        audio: false
      });
      
      this.logger.log('WebView permissions granted! Stream tracks: ' + stream.getTracks().length);
      stream.getTracks().forEach(track => track.stop());
      
      return true;
    } catch (error) {
      this.logger.log('WebView permission failed: ' + (error as Error).message);
      return false;
    }
  }
}
