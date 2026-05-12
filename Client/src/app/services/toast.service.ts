import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  toasts = signal<Toast[]>([]);

  success(message: string): void {
    this.add('success', message);
  }

  error(message: string): void {
    this.add('error', message);
  }

  info(message: string): void {
    this.add('info', message);
  }

  dismiss(id: number): void {
    this.toasts.update(t => t.filter(x => x.id !== id));
  }

  private add(type: ToastType, message: string): void {
    const id = this.nextId++;
    this.toasts.update(t => [...t, { id, type, message }]);
    setTimeout(() => this.dismiss(id), 4000);
  }
}
