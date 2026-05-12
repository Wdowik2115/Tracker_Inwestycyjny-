import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../services/auth.service';
import { WalletService } from '../../../services/wallet.service';
import { PortfolioService } from '../../../services/portfolio.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  private authService = inject(AuthService);
  private walletService = inject(WalletService);
  private portfolioService = inject(PortfolioService);

  wallets = toSignal(this.walletService.getWallets(), { initialValue: [] });
  portfolio = toSignal(this.portfolioService.getSummary(), { initialValue: null });

  currentUser = this.authService.currentUser;

  selectedSourceId = signal<string | null>(null);
  selectedAsset = signal<string | null>(null);

  selectSource(id: string): void {
    this.selectedSourceId.update(v => v === id ? null : id);
  }

  selectAsset(symbol: string): void {
    this.selectedAsset.update(v => v === symbol ? null : symbol);
  }

  logout(): void {
    this.authService.logout();
  }

  getPnlClass(pnl: number): string {
    return pnl >= 0 ? 'badge-success' : 'badge-danger';
  }

  formatPnl(pnl: number): string {
    return (pnl >= 0 ? '+' : '') + pnl.toFixed(2) + '%';
  }
}
