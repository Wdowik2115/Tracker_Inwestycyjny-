import { Component, inject } from '@angular/core';
import { ToastService } from '../../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-container">
      @for (toast of toastService.toasts(); track toast.id) {
        <div class="toast toast--{{ toast.type }}">
          <span class="toast-message">{{ toast.message }}</span>
          <button class="toast-close" (click)="toastService.dismiss(toast.id)">✕</button>
        </div>
      }
    </div>
  `,
  styleUrl: './toast.component.css'
})
export class ToastComponent {
  toastService = inject(ToastService);
}
