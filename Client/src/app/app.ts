import { Component, signal, HostListener, inject } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { ChatComponent } from './components/chat/chat';
import { CryptoBackgroundComponent } from './components/shared/crypto-background/crypto-background.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ChatComponent, CryptoBackgroundComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Client');
  private router = inject(Router);

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    const cursor = document.getElementById('custom-cursor');
    const follower = document.getElementById('custom-cursor-follower');
    
    if (cursor && follower) {
      // Use requestAnimationFrame for smoother cursor
      requestAnimationFrame(() => {
        cursor.style.left = `${e.clientX}px`;
        cursor.style.top = `${e.clientY}px`;
        
        follower.style.left = `${e.clientX}px`;
        follower.style.top = `${e.clientY}px`;
      });
    }
  }

  @HostListener('document:mousedown')
  onMouseDown() {
    const follower = document.getElementById('custom-cursor-follower');
    if (follower) follower.style.transform = 'translate(-50%, -50%) scale(0.7)';
  }

  @HostListener('document:mouseup')
  onMouseUp() {
    const follower = document.getElementById('custom-cursor-follower');
    if (follower) follower.style.transform = 'translate(-50%, -50%) scale(1)';
  }
}
