import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { WalletService } from '../../services/wallet.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../shared/modal/modal.component';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { CreateWalletDto, WalletDto } from '../../models';

function truncateAddress(addr: string): string {
  if (!addr || addr.length <= 12) return addr;
  return addr.slice(0, 6) + '…' + addr.slice(-4);
}

@Component({
  selector: 'app-wallets',
  standalone: true,
  imports: [ModalComponent, LoadingSpinnerComponent, RouterLink],
  templateUrl: './wallets.component.html',
  styleUrl: './wallets.component.css'
})
export class WalletsComponent implements OnInit {
  private walletService = inject(WalletService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);
  private router = inject(Router);

  loading = signal(true);
  wallets = signal<WalletDto[]>([]);
  addModalOpen = signal(false);
  submitting = signal(false);

  form = {
    name: signal(''),
    description: signal('')
  };

  ngOnInit(): void {
    this.titleService.setTitle('Wallets — Investee');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.walletService.getWallets().subscribe({
      next: data => { this.wallets.set(data); this.loading.set(false); },
      error: e => { this.toastService.error(e.error?.message ?? 'Failed to load wallets'); this.loading.set(false); }
    });
  }

  openAddModal(): void { this.addModalOpen.set(true); }

  closeAddModal(): void {
    this.addModalOpen.set(false);
    this.form.name.set('');
    this.form.description.set('');
  }

  submitAdd(): void {
    if (!this.form.name().trim()) {
      this.toastService.error('Wallet name is required');
      return;
    }
    const dto: CreateWalletDto = {
      name: this.form.name().trim(),
      description: this.form.description().trim() || undefined
    };
    this.submitting.set(true);
    this.walletService.createWallet(dto).subscribe({
      next: () => {
        this.toastService.success('Wallet created');
        this.closeAddModal();
        this.load();
        this.submitting.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to create wallet');
        this.submitting.set(false);
      }
    });
  }

  deleteWallet(id: string): void {
    if (!confirm('Delete this wallet? This cannot be undone.')) return;
    this.walletService.deleteWallet(id).subscribe({
      next: () => { this.toastService.success('Wallet deleted'); this.load(); },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to delete wallet')
    });
  }

  truncate = truncateAddress;

  formatCurrency(value: number): string {
    return '$' + value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
