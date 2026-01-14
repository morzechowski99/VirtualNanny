import { DebugLogger } from './DebugLogger';

/**
 * RecordingManager - zarz¹dzanie nagrywaniem wideo
 */
export class RecordingManager {
  private logger: DebugLogger;
  private mediaRecorder: MediaRecorder | null = null;
  private recordedChunks: Blob[] = [];
  private androidClonedTracks: MediaStreamTrack[] = [];
  private isAndroid: boolean = false;

  constructor() {
    this.logger = DebugLogger.getInstance();
    this.isAndroid = typeof (window as any).Android !== 'undefined' || 
                     navigator.userAgent.includes('Android');
  }

  async toggleRecording(startRecording: boolean, stream: MediaStream): Promise<boolean> {
    this.logger.log('Toggle recording: ' + startRecording);
    
    try {
      if (startRecording) {
        return await this.startRecording(stream);
      } else {
        return await this.stopRecording();
      }
    } catch (error) {
      this.logger.log('Recording toggle error: ' + (error as Error).message);
      throw error;
    }
  }

  private async startRecording(stream: MediaStream): Promise<boolean> {
    if (!stream) {
      throw new Error('No camera stream available for recording');
    }

    this.recordedChunks = [];
    this.androidClonedTracks = [];
    
    if (!window.MediaRecorder) {
      throw new Error('MediaRecorder not supported on this device');
    }

    const video = document.getElementById('cameraVideo') as HTMLVideoElement;
    if (!video || video.videoWidth === 0 || video.videoHeight === 0) {
      this.logger.log('Warning: Video element not ready or has no dimensions');
    } else {
      this.logger.log(`Video element dimensions: ${video.videoWidth}x${video.videoHeight}`);
    }

    // Przygotowujemy stream do nagrywania
    let recordingStream = await this.prepareRecordingStream(stream);

    // Konfigurujemy opcje MediaRecorder
    const recorderOptions = this.getRecorderOptions();

    this.mediaRecorder = new MediaRecorder(recordingStream, recorderOptions);
    
    this.setupMediaRecorderListeners();
    
    const dataInterval = this.isAndroid ? 500 : 1000;
    this.mediaRecorder.start(dataInterval);
    this.logger.log('MediaRecorder.start() called with ' + dataInterval + 'ms interval');
    
    return true;
  }

  private async stopRecording(): Promise<boolean> {
    if (this.mediaRecorder) {
      this.logger.log('Stopping MediaRecorder, current state: ' + this.mediaRecorder.state);
      
      if (this.mediaRecorder.state === 'recording') {
        this.mediaRecorder.stop();
        this.logger.log('MediaRecorder.stop() called');
      } else {
        this.logger.log('MediaRecorder was not in recording state');
      }
    } else {
      this.logger.log('No MediaRecorder instance found');
    }

    return true;
  }

  private async prepareRecordingStream(stream: MediaStream): Promise<MediaStream> {
    if (this.isAndroid) {
      return await this.prepareAndroidRecordingStream(stream);
    } else {
      return await this.prepareStandardRecordingStream(stream);
    }
  }

  private async prepareAndroidRecordingStream(stream: MediaStream): Promise<MediaStream> {
    this.logger.log('Android detected - cloning video track for recording');
    try {
      const videoTrack = stream.getVideoTracks()[0];
      if (!videoTrack) {
        return stream;
      }

      const clonedVideoTrack = videoTrack.clone();
      this.androidClonedTracks.push(clonedVideoTrack);

      let clonedAudioTrack: MediaStreamTrack | null = null;
      const audioTracks = stream.getAudioTracks();
      
      if (audioTracks.length > 0) {
        clonedAudioTrack = audioTracks[0].clone();
        this.androidClonedTracks.push(clonedAudioTrack);
      } else {
        try {
          const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
          if (audioStream?.getAudioTracks().length > 0) {
            clonedAudioTrack = audioStream.getAudioTracks()[0];
            this.androidClonedTracks.push(clonedAudioTrack);
          }
        } catch (audioErr) {
          this.logger.log('Could not obtain audio for cloning: ' + (audioErr as Error).message);
        }
      }

      const tracks: MediaStreamTrack[] = [clonedVideoTrack];
      if (clonedAudioTrack) tracks.push(clonedAudioTrack);

      return new MediaStream(tracks);
    } catch (error) {
      this.logger.log('Error during cloning for Android recording: ' + (error as Error).message);
      return stream;
    }
  }

  private async prepareStandardRecordingStream(stream: MediaStream): Promise<MediaStream> {
    const videoTracks = stream.getVideoTracks();
    const audioTracks = stream.getAudioTracks();
    
    this.logger.log(`Current stream has ${videoTracks.length} video tracks and ${audioTracks.length} audio tracks`);

    if (audioTracks.length === 0) {
      try {
        this.logger.log('Attempting to add audio track for recording...');
        const audioStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const audioTrack = audioStream.getAudioTracks()[0];
        
        if (audioTrack) {
          return new MediaStream([...videoTracks, audioTrack]);
        }
      } catch (audioError) {
        this.logger.log('Could not get audio track: ' + (audioError as Error).message);
      }
    }

    return stream;
  }

  private getRecorderOptions(): MediaRecorderOptions {
    let options: MediaCorderOptions = {};
    
    if (this.isAndroid) {
      const androidMimeTypes = [
        'video/mp4',
        'video/mp4;codecs=h264',
        'video/webm;codecs=vp8',
        'video/webm'
      ];
      
      for (const mimeType of androidMimeTypes) {
        if (MediaRecorder.isTypeSupported(mimeType)) {
          options = { 
            mimeType: mimeType,
            videoBitsPerSecond: 2500000
          };
          this.logger.log('Android: Using MIME type: ' + mimeType);
          break;
        }
      }
    } else {
      const supportedMimeTypes = [
        'video/webm;codecs=vp9,opus',
        'video/webm;codecs=vp8,opus', 
        'video/webm;codecs=h264,opus',
        'video/webm;codecs=vp9',
        'video/webm;codecs=vp8',
        'video/webm',
        'video/mp4;codecs=h264,aac',
        'video/mp4'
      ];

      for (const mimeType of supportedMimeTypes) {
        if (MediaRecorder.isTypeSupported(mimeType)) {
          options = { mimeType: mimeType };
          this.logger.log('Using MIME type: ' + mimeType);
          break;
        }
      }
    }

    return options;
  }

  private setupMediaRecorderListeners(): void {
    if (!this.mediaRecorder) return;

    this.mediaRecorder.ondataavailable = (event: BlobEvent) => {
      if (event.data && event.data.size > 0) {
        this.recordedChunks.push(event.data);
        this.logger.log('Recorded chunk: ' + event.data.size + ' bytes, total chunks: ' + this.recordedChunks.length);
      }
    };

    this.mediaRecorder.onstart = () => {
      this.logger.log('Recording started successfully');
    };

    this.mediaRecorder.onstop = () => {
      this.handleRecordingStop();
    };

    this.mediaRecorder.onerror = (error: Event) => {
      const errorEvent = error as any;
      this.logger.log('MediaRecorder error: ' + (errorEvent.error || errorEvent.message || 'Unknown error'));
    };

    this.mediaRecorder.onpause = () => {
      this.logger.log('Recording paused');
    };

    this.mediaRecorder.onresume = () => {
      this.logger.log('Recording resumed');
    };
  }

  private handleRecordingStop(): void {
    this.logger.log('Recording stopped, total chunks: ' + this.recordedChunks.length);
    
    // Zatrzymujemy sklonowane œcie¿ki dla Androida
    if (this.isAndroid && this.androidClonedTracks.length > 0) {
      this.logger.log('Stopping cloned Android tracks');
      this.androidClonedTracks.forEach(t => {
        try { t.stop(); } catch (e) { /* ignore */ }
      });
      this.androidClonedTracks = [];
    }

    if (this.recordedChunks.length > 0) {
      const totalSize = this.recordedChunks.reduce((sum, chunk) => sum + chunk.size, 0);
      this.logger.log('Total recording size: ' + totalSize + ' bytes');
      
      const blobMimeType = this.getBlobMimeType();
      const blob = new Blob(this.recordedChunks, { type: blobMimeType });
      
      const filename = this.generateFilename(blobMimeType);
      
      if (this.isAndroid) {
        this.logger.log('Android detected - using Android-specific file saving');
        this.saveFileOnAndroid(blob, filename, blobMimeType);
      } else {
        this.logger.log('Non-Android platform - using standard web download');
        this.downloadFileStandard(blob, filename);
      }
    } else {
      this.logger.log('No chunks recorded!');
    }
  }

  private getBlobMimeType(): string {
    if (this.mediaRecorder && (this.mediaRecorder as any).mimeType) {
      return (this.mediaRecorder as any).mimeType.split(';')[0];
    }
    return 'video/webm';
  }

  private generateFilename(mimeType: string): string {
    const extension = mimeType.includes('mp4') ? 'mp4' : 'webm';
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').substring(0, 19);
    return `virtualnanny_recording_${timestamp}.${extension}`;
  }

  private async saveFileOnAndroid(blob: Blob, filename: string, mimeType: string): Promise<void> {
    try {
      this.logger.log('Android file save - trying multiple methods...');
      
      // Metoda 1: Native C# file saving
      try {
        const base64Data = await this.blobToBase64(blob);
        const nativeResult = await this.saveFileNatively(base64Data, filename, mimeType);
        if (nativeResult) {
          this.logger.log('Native save successful!');
          return;
        }
      } catch (nativeError) {
        this.logger.log('Native save error: ' + (nativeError as Error).message);
      }
      
      // Metoda 2: File System Access API
      if ('showSaveFilePicker' in window) {
        try {
          this.logger.log('Method 2: File System Access API...');
          const fileHandle = await (window as any).showSaveFilePicker({
            suggestedName: filename,
            types: [{
              description: 'Video files',
              accept: { [mimeType]: [`.${filename.split('.').pop()}`] }
            }]
          });
          
          const writable = await fileHandle.createWritable();
          await writable.write(blob);
          await writable.close();
          this.logger.log('File System Access API successful!');
          return;
        } catch (fsError) {
          this.logger.log('File System Access API failed: ' + (fsError as Error).message);
        }
      }
      
      // Metoda 3: Web Share API
      if (navigator.share && navigator.canShare) {
        try {
          const file = new File([blob], filename, { type: mimeType });
          if (navigator.canShare({ files: [file] })) {
            this.logger.log('Method 3: Web Share API...');
            await navigator.share({
              files: [file],
              title: 'VirtualNanny Recording',
              text: 'Recorded video from VirtualNanny'
            });
            this.logger.log('Web Share API successful!');
            return;
          }
        } catch (shareError) {
          this.logger.log('Web Share API failed: ' + (shareError as Error).message);
        }
      }
      
      // Metoda 4: Fallback
      this.fallbackAndroidSave(blob, filename);
      
    } catch (error) {
      this.logger.log('All Android save methods failed: ' + (error as Error).message);
    }
  }

  private fallbackAndroidSave(blob: Blob, filename: string): void {
    try {
      this.logger.log('Using fallback Android save method');
      const reader = new FileReader();
      
      reader.onload = () => {
        try {
          const a = document.createElement('a');
          a.href = reader.result as string;
          a.download = filename;
          a.style.display = 'none';
          document.body.appendChild(a);
          
          const clickEvent = new MouseEvent('click', {
            view: window,
            bubbles: true,
            cancelable: true
          });
          a.dispatchEvent(clickEvent);
          
          document.body.removeChild(a);
          this.logger.log('Fallback download triggered');
          
          setTimeout(() => {
            try {
              const newWindow = window.open(reader.result as string, '_blank');
              if (newWindow) {
                this.logger.log('Opened file in new window');
              }
            } catch (e) {
              this.logger.log('New window open failed: ' + (e as Error).message);
            }
          }, 1000);
        } catch (linkError) {
          this.logger.log('Link download failed: ' + (linkError as Error).message);
        }
      };
      
      reader.onerror = () => {
        this.logger.log('FileReader error');
      };
      
      reader.readAsDataURL(blob);
    } catch (error) {
      this.logger.log('Fallback save failed: ' + (error as Error).message);
    }
  }

  private downloadFileStandard(blob: Blob, filename: string): void {
    try {
      this.logger.log('Using standard web download');
      const url = URL.createObjectURL(blob);
      
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.style.display = 'none';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      this.logger.log('Standard download completed');
    } catch (error) {
      this.logger.log('Standard download error: ' + (error as Error).message);
    }
  }

  private async blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => {
        const result = reader.result as string;
        const base64 = result.split(',')[1];
        resolve(base64);
      };
      reader.onerror = reject;
      reader.readAsDataURL(blob);
    });
  }

  private async saveFileNatively(base64Data: string, filename: string, mimeType: string): Promise<boolean> {
    try {
      this.logger.log('Attempting native file save: ' + filename);
      const result = await (window as any).DotNet?.invokeMethodAsync('VirtualNanny', 'SaveFileFromJavaScript', base64Data, filename, mimeType);
      this.logger.log('Native file save result: ' + result);
      return result;
    } catch (error) {
      this.logger.log('Native file save error: ' + (error as Error).message);
      return false;
    }
  }

  checkRecordingSupport(): void {
    const info: string[] = [];
    
    info.push('=== Recording Support Check ===');
    info.push('MediaRecorder supported: ' + !!window.MediaRecorder);
    
    if (window.MediaRecorder) {
      const supportedTypes = [
        'video/webm;codecs=vp9,opus',
        'video/webm;codecs=vp8,opus', 
        'video/webm;codecs=h264,opus',
        'video/webm;codecs=vp9',
        'video/webm;codecs=vp8',
        'video/webm',
        'video/mp4;codecs=h264,aac',
        'video/mp4'
      ];
      
      info.push('Supported MIME types:');
      supportedTypes.forEach(type => {
        const supported = MediaRecorder.isTypeSupported(type);
        info.push(`  ${type}: ${supported ? 'YES' : 'NO'}`);
      });
    }
    
    alert(info.join('\n'));
  }

  async testRecording(durationSeconds: number = 5, stream: MediaStream): Promise<boolean> {
    this.logger.log('Starting test recording for ' + durationSeconds + ' seconds...');
    
    try {
      await this.toggleRecording(true, stream);
      this.logger.log('Test recording started');
      
      setTimeout(() => {
        this.toggleRecording(false, stream).catch(error => {
          this.logger.log('Error stopping test recording: ' + (error as Error).message);
        });
      }, durationSeconds * 1000);
      
      return true;
    } catch (error) {
      this.logger.log('Test recording failed: ' + (error as Error).message);
      return false;
    }
  }
}

// TypeScript interface for MediaRecorder options
interface MediaCorderOptions {
  mimeType?: string;
  videoBitsPerSecond?: number;
}
