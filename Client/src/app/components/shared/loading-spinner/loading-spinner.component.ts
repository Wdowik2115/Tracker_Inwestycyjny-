import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `<div class="spinner spinner--{{ size() }}"></div>`,
  styleUrl: './loading-spinner.component.css'
})
export class LoadingSpinnerComponent {
  size = input<'sm' | 'md' | 'lg'>('md');
}
