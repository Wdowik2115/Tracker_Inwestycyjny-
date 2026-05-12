import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AlertService } from '../../services/alert.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../shared/modal/modal.component';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { AlertDirection, AlertDto, CreateAlertDto } from '../../models';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [ModalComponent, LoadingSpinnerComponent],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.css'
})
export class AlertsComponent implements OnInit {
  private alertService = inject(AlertService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  readonly AlertDirection = AlertDirection;

  loading = signal(true);
  alerts = signal<AlertDto[]>([]);
  submitting = signal(false);
  addModalOpen = signal(false);

  form = {
    symbol: signal(''),
    targetPrice: signal(''),
    direction: signal<AlertDirection>(AlertDirection.Above)
  };

  active = computed(() => this.alerts().filter(a => !a.isTriggered));
  triggered = computed(() => this.alerts().filter(a => a.isTriggered));

  ngOnInit(): void {
    this.titleService.setTitle('Alerts — Investee');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.alertService.getAlerts().subscribe({
      next: data => { this.alerts.set(data); this.loading.set(false); },
      error: e => { this.toastService.error(e.error?.message ?? 'Failed to load alerts'); this.loading.set(false); }
    });
  }

  openAddModal(): void { this.addModalOpen.set(true); }

  closeAddModal(): void {
    this.addModalOpen.set(false);
    this.form.symbol.set('');
    this.form.targetPrice.set('');
    this.form.direction.set(AlertDirection.Above);
  }

  submitAdd(): void {
    const price = parseFloat(this.form.targetPrice());
    if (!this.form.symbol() || isNaN(price)) {
      this.toastService.error('Please fill in all required fields');
      return;
    }
    const dto: CreateAlertDto = {
      symbol: this.form.symbol().toUpperCase(),
      targetPrice: price,
      direction: this.form.direction()
    };
    this.submitting.set(true);
    this.alertService.createAlert(dto).subscribe({
      next: () => {
        this.toastService.success('Alert created');
        this.closeAddModal();
        this.load();
        this.submitting.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to create alert');
        this.submitting.set(false);
      }
    });
  }

  deleteAlert(id: string): void {
    if (!confirm('Delete this alert?')) return;
    this.alertService.deleteAlert(id).subscribe({
      next: () => { this.toastService.success('Alert deleted'); this.load(); },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to delete alert')
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatPrice(n: number): string {
    return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
