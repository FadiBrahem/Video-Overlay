// upload.component.ts

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.css'],
})
export class UploadComponent {
  videoFile: File | null = null;
  nomVideo: string = '';
  nombreReps: string = '';
  nbCalories: string = '';
  textColor: string = '#FFFFFF'; // Default color: White
  textSize: number = 24; // Default font size: 24

  isLoading: boolean = false; // For loading indicator

  onFileSelected(event: any) {
    this.videoFile = event.target.files[0];
  }

  async onSubmit(event: Event) {
    event.preventDefault();

    // Validate required fields
    if (!this.videoFile || !this.nomVideo) {
      alert('Please provide the required fields: Video and Nom de la vidéo.');
      return;
    }

    // Validate text size
    if (this.textSize < 10 || this.textSize > 100) {
      alert('Text size must be between 10 and 100.');
      return;
    }

    // Prepare FormData
    const formData = new FormData();
    formData.append('video', this.videoFile);
    formData.append('nomVideo', this.nomVideo);
    formData.append('nombreReps', this.nombreReps);
    formData.append('nbCalories', this.nbCalories);
    formData.append('textColor', this.textColor);
    formData.append('textSize', this.textSize.toString());

    try {
      this.isLoading = true; // Start loading

      // Send POST request to backend
      const response = await fetch('http://localhost:5063/api/video/upload', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Video processing failed.');
      }

      // Receive the processed video as a blob
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);

      // Trigger download of the processed video
      const a = document.createElement('a');
      a.href = url;
      a.download = 'processed_video.mp4';
      a.click();
      window.URL.revokeObjectURL(url);
    } catch (error: unknown) {
      console.error('Upload Error:', error);
      if (error instanceof Error) {
        alert(`An error occurred: ${error.message}`);
      } else {
        alert('An unknown error occurred.');
      }
    } finally {
      this.isLoading = false; // End loading
    }
  }
}
