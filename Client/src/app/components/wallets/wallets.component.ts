import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { WalletService } from '../../services/wallet.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../shared/modal/modal.component';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { CreateWalletDto, UpdateWalletDto, WalletDto } from '../../models';

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
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);
  private router = inject(Router);

  loading = signal(true);
  wallets = signal<WalletDto[]>([]);
  addModalOpen = signal(false);
  editModalOpen = signal(false);
  shareModalOpen = signal(false);
  editingWalletId = signal<string | null>(null);
  sharingWallet = signal<WalletDto | null>(null);
  submitting = signal(false);

  form = {
    name: signal(''),
    description: signal('')
  };

  editForm = {
    name: signal(''),
    description: signal('')
  };

  shareEmail = signal('');

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

  isOwner(wallet: WalletDto): boolean {
    return wallet.ownerId === this.authService.currentUser()?.userId;
  }

  openAddModal(): void { this.addModalOpen.set(true); }

  closeAddModal(): void {
    this.addModalOpen.set(false);
    this.form.name.set('');
    this.form.description.set('');
  }

  openEditModal(wallet: WalletDto): void {
    this.editingWalletId.set(wallet.id);
    this.editForm.name.set(wallet.name);
    this.editForm.description.set(wallet.description);
    this.editModalOpen.set(true);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
    this.editingWalletId.set(null);
    this.editForm.name.set('');
    this.editForm.description.set('');
  }

  openShareModal(wallet: WalletDto): void {
    this.sharingWallet.set(wallet);
    this.shareEmail.set('');
    this.shareModalOpen.set(true);
  }

  closeShareModal(): void {
    this.shareModalOpen.set(false);
    this.sharingWallet.set(null);
    this.shareEmail.set('');
  }

  addShare(): void {
    const email = this.shareEmail().trim();
    const wallet = this.sharingWallet();
    if (!email || !wallet) return;

    this.submitting.set(true);
    this.walletService.shareWallet(wallet.id, email).subscribe({
      next: () => {
        this.toastService.success(`Shared with ${email}`);
        this.shareEmail.set('');
        this.submitting.set(false);
        this.reloadSharingWallet(wallet.id);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to share wallet');
        this.submitting.set(false);
      }
    });
  }

  removeShare(email: string): void {
    const wallet = this.sharingWallet();
    if (!wallet) return;

    this.walletService.unshareWallet(wallet.id, email).subscribe({
      next: () => {
        this.toastService.success(`Removed ${email}`);
        this.reloadSharingWallet(wallet.id);
      },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to remove share')
    });
  }

  private reloadSharingWallet(id: string): void {
    this.walletService.getWallets().subscribe(data => {
      this.wallets.set(data);
      const updated = data.find(w => w.id === id);
      if (updated) this.sharingWallet.set(updated);
    });
  }

  submitEdit(): void {
    if (!this.editForm.name().trim()) {
      this.toastService.error('Wallet name is required');
      return;
    }
    const id = this.editingWalletId();
    if (!id) return;

    const dto: UpdateWalletDto = {
      name: this.editForm.name().trim(),
      description: this.editForm.description().trim() || undefined
    };
    this.submitting.set(true);
    this.walletService.updateWallet(id, dto).subscribe({
      next: () => {
        this.toastService.success('Wallet updated');
        this.closeEditModal();
        this.load();
        this.submitting.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to update wallet');
        this.submitting.set(false);
      }
    });
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
    const wallet = this.wallets().find(w => w.id === id);
    const assetInfo = wallet && wallet.assetCount > 0
      ? ` It contains ${wallet.assetCount} asset${wallet.assetCount === 1 ? '' : 's'} and all transaction history will be permanently lost.`
      : '';
    if (!confirm(`Delete wallet "${wallet?.name}"?${assetInfo} This cannot be undone.`)) return;
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
