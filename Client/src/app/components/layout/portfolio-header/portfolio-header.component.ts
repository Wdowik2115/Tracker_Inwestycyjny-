import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { merge, Subscription, timer } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { PortfolioService } from '../../../services/portfolio.service';
import { TransactionService } from '../../../services/transaction.service';
import { UserService } from '../../../services/user.service';
import { PortfolioSummaryDto } from '../../../models';
import { AuthService } from '../../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-portfolio-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portfolio-header.component.html',
  styleUrl: './portfolio-header.component.css'
})
export class PortfolioHeaderComponent implements OnInit, OnDestroy {
  private portfolioService = inject(PortfolioService);
  private transactionService = inject(TransactionService);
  private userService = inject(UserService);
  public authService = inject(AuthService);

  private refreshSub?: Subscription;

  portfolio = signal<PortfolioSummaryDto | null>(null);
  currency = signal('USD');
  hideValues = signal(false);

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.loadData();
    }
  }

  loadData() {
    this.userService.getProfile().subscribe({
      next: u => this.currency.set(u.preferredCurrency),
      error: () => {}
    });

    this.refreshSub = merge(
      timer(0, 30000),
      this.transactionService.transactionAdded$
    ).pipe(
      switchMap(() => this.portfolioService.getSummary())
    ).subscribe({
      next: data => this.portfolio.set(data),
      error: () => {}
    });
  }

  logout() {
    this.authService.logout();
  }

  ngOnDestroy(): void {
    this.refreshSub?.unsubscribe();
  }

  get currencySymbol(): string {
    const symbols: Record<string, string> = { USD: '$', EUR: '€', GBP: '£', PLN: 'zł' };
    return symbols[this.currency()] ?? this.currency() + ' ';
  }

  formatCurrency(v: number): string {
    const sym = this.currencySymbol;
    const abs = Math.abs(v).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return (v < 0 ? '-' : '') + sym + abs;
  }

  formatPercent(v: number): string {
    return (v >= 0 ? '+' : '') + v.toFixed(2) + '%';
  }

  pnlClass(v: number): string {
    return v >= 0 ? 'badge-success' : 'badge-danger';
  }
}
