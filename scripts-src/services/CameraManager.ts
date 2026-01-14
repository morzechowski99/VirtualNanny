import { DebugLogger } from './DebugLogger';
import { PermissionManager } from './PermissionManager';

/**
 * CameraManager - zarz¹dzanie kamer¹, prze³¹czanie obiektywów
 */
export class CameraManager {
  private logger: DebugLogger;
  private permissionManager: PermissionManager;
  private currentStream: MediaStream | null = null;
  private currentCameraType: string | null = null;
  private isInitializing: boolean = false;
  private videoElement: HTMLVideoElement | null = null;

  constructor() {
    this.logger = DebugLogger.getInstance();
    this.permissionManager = new PermissionManager();
  }

  async initialize(useFrontCamera: boolean = false): Promise<boolean> {
    if (this.isInitializing) {
      this.logger.log('Camera already initializing, skipping...');
      return false;
    }

    this.isInitializing = true;
    this.logger.log('Starting camera initialization, front camera: ' + useFrontCamera);

    try {
      this.stopStream();

      if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error('getUserMedia not supported on this device');
      }

      // Czekamy na element video
      this.videoElement = await this.waitForVideoElement();
      this.logger.log('Video element found: ' + this.videoElement.tagName);

      // Czyœcimy poprzedni stream
      this.videoElement.srcObject = null;
      this.videoElement.src = '';
      this.videoElement.load();

      // Pobieramy dostêpne kamery
      const availableDevices = await this.getAvailableCameras();
      
      // Budujemy constraints
      const constraints = this.buildConstraints(availableDevices, useFrontCamera);
      
      this.logger.log('Requesting camera stream with constraints: ' + JSON.stringify(constraints));

      // Próbujemy uzyskaæ stream
      this.currentStream = await this.getStream(constraints);

      if (!this.currentStream) {
        throw new Error('No stream received');
      }

      // Próbujemy dodaæ audio
      await this.addAudioTrack();

      this.logStreamInfo();
      
      // Przypisujemy stream do elementu video
      this.videoElement.srcObject = this.currentStream;
      this.logger.log('Video srcObject set');
      
      // Czekamy na za³adowanie metadanych
      return await this.waitForVideoPlayback();

    } catch (error) {
      this.logger.log('Camera initialization error: ' + (error as Error).message);
      this.isInitializing = false;
      throw error;
    }
  }

  async switchCamera(useFrontCamera: boolean): Promise<boolean> {
    this.logger.log(`Switching camera, front camera: ${useFrontCamera}, current: ${this.currentCameraType}`);
    
    if (this.isInitializing) {
      this.logger.log('Camera is initializing, cannot switch now');
      return false;
    }
    
    try {
      const targetFacingMode = useFrontCamera ? 'user' : 'environment';
      if (this.currentCameraType === targetFacingMode) {
        this.logger.log(`Already using ${targetFacingMode} camera, no switch needed`);
        return true;
      }
      
      const result = await this.initialize(useFrontCamera);
      this.logger.log('Camera switch result: ' + result);
      
      if (result) {
        const cameraTypeText = useFrontCamera ? 'front' : 'back';
        this.logger.log(`Successfully switched to ${cameraTypeText} camera`);
      }
      
      return result;
    } catch (error) {
      this.logger.log('Camera switch error: ' + (error as Error).message);
      return false;
    }
  }

  getMediaStream(): MediaStream | null {
    return this.currentStream;
  }

  getCurrentCameraType(): string | null {
    return this.currentCameraType;
  }

  stopStream(): void {
    if (this.currentStream) {
      this.logger.log('Stopping existing stream');
      this.currentStream.getTracks().forEach(track => {
        track.stop();
        this.logger.log('Stopped track: ' + track.kind);
      });
      this.currentStream = null;
    }
  }

  private async waitForVideoElement(retries: number = 15): Promise<HTMLVideoElement> {
    let retryCount = 0;
    
    while (retryCount < retries) {
      const video = document.getElementById('cameraVideo') as HTMLVideoElement;
      if (video) {
        this.logger.log(`Video element found on attempt ${retryCount + 1}`);
        return video;
      }
      
      this.logger.log(`Waiting for video element... attempt ${retryCount + 1}/${retries}`);
      await new Promise(resolve => setTimeout(resolve, 300));
      retryCount++;
    }

    // Fallback: szukamy pierwszego elementu video
    const videos = document.querySelectorAll('video');
    if (videos.length > 0) {
      this.logger.log('Using first video element found by tagName');
      return videos[0] as HTMLVideoElement;
    }

    throw new Error('Video element not found after ' + retries + ' attempts');
  }

  private async getAvailableCameras(): Promise<MediaDeviceInfo[]> {
    try {
      const devices = await navigator.mediaDevices.enumerateDevices();
      const cameras = devices.filter(device => device.kind === 'videoinput');
      this.logger.log('Found ' + cameras.length + ' video input devices');
      cameras.forEach((device, index) => {
        this.logger.log(`Device ${index}: ${device.label || 'Unknown camera'} (${device.deviceId.substring(0, 8)}...)`);
      });
      return cameras;
    } catch (error) {
      this.logger.log('Error enumerating devices: ' + (error as Error).message);
      return [];
    }
  }

  private buildConstraints(availableDevices: MediaDeviceInfo[], useFrontCamera: boolean): MediaStreamConstraints {
    if (availableDevices.length > 1) {
      return this.buildDeviceSpecificConstraints(availableDevices, useFrontCamera);
    } else {
      return this.buildFacingModeConstraints(useFrontCamera);
    }
  }

  private buildDeviceSpecificConstraints(devices: MediaDeviceInfo[], useFrontCamera: boolean): MediaStreamConstraints {
    let targetDevice = null;
    
    if (useFrontCamera) {
      targetDevice = devices.find(device => 
        device.label.toLowerCase().includes('front') || 
        device.label.toLowerCase().includes('user') ||
        device.label.toLowerCase().includes('face')
      );
      
      if (!targetDevice && devices.length > 1) {
        targetDevice = devices[1];
      }
    } else {
      targetDevice = devices.find(device => 
        device.label.toLowerCase().includes('back') || 
        device.label.toLowerCase().includes('rear') ||
        device.label.toLowerCase().includes('environment')
      );
      
      if (!targetDevice) {
        targetDevice = devices[0];
      }
    }
    
    if (targetDevice) {
      this.logger.log(`Using specific device: ${targetDevice.label || 'Unknown'}`);
      return {
        video: {
          deviceId: { exact: targetDevice.deviceId },
          width: { ideal: 640, max: 1280 },
          height: { ideal: 480, max: 720 }
        }
      };
    }

    return this.buildFacingModeConstraints(useFrontCamera);
  }

  private buildFacingModeConstraints(useFrontCamera: boolean): MediaStreamConstraints {
    this.logger.log('Using facingMode approach');
    return {
      video: {
        facingMode: useFrontCamera ? 'user' : 'environment',
        width: { ideal: 640, max: 1280 },
        height: { ideal: 480, max: 720 }
      }
    };
  }

  private async getStream(constraints: MediaStreamConstraints): Promise<MediaStream | null> {
    try {
      return await navigator.mediaDevices.getUserMedia(constraints);
    } catch (error) {
      const errorMsg = (error as Error).message;
      this.logger.log('Video constraints failed: ' + errorMsg);
      
      if (errorMsg.includes('Permission denied') || 
          errorMsg.includes('denied') ||
          (error as any).name === 'NotAllowedError') {
        
        this.logger.log('Permission denied - trying enhanced permission request...');
        const enhancedResult = await this.permissionManager.enhancedPermissionRequest();
        
        if (enhancedResult) {
          this.logger.log('Enhanced permissions granted, retrying camera...');
          await new Promise(resolve => setTimeout(resolve, 1000));
          
          try {
            return await navigator.mediaDevices.getUserMedia(constraints);
          } catch (retryError) {
            this.logger.log('Camera still failed after enhanced permissions: ' + (retryError as Error).message);
            throw retryError;
          }
        }
      }

      // Fallback constraints
      return await this.tryFallbackConstraints();
    }
  }

  private async tryFallbackConstraints(): Promise<MediaStream | null> {
    const fallbackConstraints: MediaStreamConstraints[] = [
      { video: { facingMode: 'environment' } },
      { video: { facingMode: 'user' } },
      { video: true }
    ];
    
    for (let i = 0; i < fallbackConstraints.length; i++) {
      try {
        this.logger.log(`Trying fallback constraint ${i + 1}`);
        return await navigator.mediaDevices.getUserMedia(fallbackConstraints[i]);
      } catch (error) {
        this.logger.log(`Fallback ${i + 1} failed: ${(error as Error).message}`);
      }
    }

    return null;
  }

  private async addAudioTrack(): Promise<void> {
    try {
      if (!this.currentStream) return;

      const audioTracks = this.currentStream.getAudioTracks();
      if (audioTracks.length === 0) {
        this.logger.log('Attempting to add audio track...');
        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const audioTrack = audioStream.getAudioTracks()[0];
        
        if (audioTrack) {
          this.currentStream.addTrack(audioTrack);
          this.logger.log('Added audio track to current stream');
        }
      }
    } catch (error) {
      this.logger.log('Could not add audio track: ' + (error as Error).message + ' - continuing with video only');
    }
  }

  private logStreamInfo(): void {
    if (!this.currentStream) return;

    this.logger.log('Stream received, tracks: ' + this.currentStream.getTracks().length);
    this.currentStream.getTracks().forEach((track, index) => {
      this.logger.log(`Track ${index}: ${track.kind}, enabled: ${track.enabled}, readyState: ${track.readyState}`);
      
      if (track.kind === 'video') {
        const settings = track.getSettings();
        this.logger.log(`Video track settings: ${JSON.stringify(settings)}`);
        
        if (settings.facingMode) {
          this.logger.log(`Camera facing mode: ${settings.facingMode}`);
          this.currentCameraType = settings.facingMode;
        }
      }
    });
  }

  private async waitForVideoPlayback(): Promise<boolean> {
    return new Promise((resolve, reject) => {
      if (!this.videoElement) {
        reject(new Error('Video element is null'));
        return;
      }

      const timeoutId = setTimeout(() => {
        this.logger.log('Video load timeout');
        this.isInitializing = false;
        reject(new Error('Video load timeout'));
      }, 20000);

      this.videoElement.onloadedmetadata = () => {
        clearTimeout(timeoutId);
        this.logger.log('Video metadata loaded, dimensions: ' + this.videoElement!.videoWidth + 'x' + this.videoElement!.videoHeight);
        
        this.videoElement!.play().then(() => {
          this.logger.log('Video playing successfully');
          this.isInitializing = false;
          resolve(true);
        }).catch(playError => {
          this.logger.log('Video play error: ' + (playError as Error).message);
          this.isInitializing = false;
          reject(playError);
        });
      };

      this.videoElement.onerror = () => {
        clearTimeout(timeoutId);
        this.logger.log('Video error');
        this.isInitializing = false;
        reject(new Error('Video load error'));
      };

      // Jeœli metadata jest ju¿ za³adowana
      if (this.videoElement.readyState >= 1) {
        this.logger.log('Video already has metadata, triggering onloadedmetadata');
        this.videoElement.onloadedmetadata!({} as Event);
      }
    });
  }

  showAvailableCameras(): void {
    this.getAvailableCameras().then(cameras => {
      const cameraInfo: string[] = [];
      cameraInfo.push('=== Available Cameras ===');
      cameraInfo.push(`Total cameras found: ${cameras.length}`);
      
      cameras.forEach((camera, index) => {
        cameraInfo.push(`Camera ${index + 1}:`);
        cameraInfo.push(`  Label: ${camera.label || 'Unknown camera'}`);
        cameraInfo.push(`  Device ID: ${camera.deviceId.substring(0, 12)}...`);
        cameraInfo.push('');
      });
      
      if (this.currentStream) {
        const videoTrack = this.currentStream.getVideoTracks()[0];
        if (videoTrack) {
          const settings = videoTrack.getSettings();
          cameraInfo.push('=== Current Camera ===');
          cameraInfo.push(`Facing mode: ${settings.facingMode || 'Unknown'}`);
          cameraInfo.push(`Resolution: ${settings.width}x${settings.height}`);
          cameraInfo.push(`Frame rate: ${settings.frameRate || 'Unknown'}`);
          cameraInfo.push(`Current camera type: ${this.currentCameraType || 'Unknown'}`);
        }
      }
      
      alert(cameraInfo.join('\n'));
    });
  }

  verifyCameraStream(): void {
    const info: string[] = [];
    
    info.push('=== Camera Stream Verification ===');
    
    const video = document.getElementById('cameraVideo') as HTMLVideoElement;
    if (!video) {
      info.push('? Video element not found');
      return alert(info.join('\n'));
    }
    
    info.push('? Video element found');
    info.push(`Video dimensions: ${video.videoWidth}x${video.videoHeight}`);
    info.push(`Video ready state: ${video.readyState}`);
    info.push(`Video paused: ${video.paused}`);
    info.push(`Video src: ${video.srcObject ? 'Stream assigned' : 'No stream'}`);
    
    if (this.currentStream) {
      info.push('? Current stream exists');
      info.push(`Stream tracks: ${this.currentStream.getTracks().length}`);
      
      this.currentStream.getTracks().forEach((track, i) => {
        info.push(`Track ${i}: ${track.kind} - ${track.readyState}`);
        
        if (track.kind === 'video') {
          const settings = track.getSettings();
          info.push(`  Video settings: ${settings.width}x${settings.height} @ ${settings.frameRate}fps`);
        }
      });
    } else {
      info.push('? No current stream available');
    }
    
    if (video.videoWidth > 0 && video.videoHeight > 0 && !video.paused) {
      info.push('? Video appears to be playing correctly');
    } else {
      info.push('?? Video may not be displaying correctly');
    }
    
    alert(info.join('\n'));
  }
}
