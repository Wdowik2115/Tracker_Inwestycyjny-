import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { WalletService } from '../../../services/wallet.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { LoadingSpinnerComponent } from '../../shared/loading-spinner/loading-spinner.component';
import { WalletDetailsDto } from '../../../models';

@Component({
  selector: 'app-wallet-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingSpinnerComponent],
  templateUrl: './wallet-detail.component.html',
  styleUrl: './wallet-detail.component.css'
})
export class WalletDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private walletService = inject(WalletService);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  wallet = signal<WalletDetailsDto | null>(null);
  loading = signal(true);
  private routeSub?: Subscription;

  ngOnInit(): void {
    // Watch for route parameter changes (e.g., when switching wallets in sidebar)
    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.load(id);
      }
    });
  }

  isOwner(): boolean {
    const w = this.wallet();
    return w?.ownerId === this.authService.currentUser()?.userId;
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  load(id: string): void {
    this.loading.set(true);
    this.walletService.getWallet(id).subscribe({
      next: data => {
        this.wallet.set(data);
        this.titleService.setTitle(`${data.name} — Investee`);
        this.loading.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to load wallet details');
        this.loading.set(false);
      }
    });
  }

  formatCurrency(value: number): string {
    return '$' + value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatPercent(value: number): string {
    return (value >= 0 ? '+' : '') + value.toFixed(2) + '%';
  }
}
