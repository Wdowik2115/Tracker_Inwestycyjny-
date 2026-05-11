import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { PortfolioService } from '../../services/portfolio.service';
import { WalletService } from '../../services/wallet.service';
import { TransactionService } from '../../services/transaction.service';
import { AlertService } from '../../services/alert.service';
import { PortfolioSummaryDto, WalletDto, TransactionDto, AlertDto } from '../../models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  portfolio = signal<PortfolioSummaryDto | null>(null);
  wallets = signal<WalletDto[]>([]);
  recentTransactions = signal<TransactionDto[]>([]);
  alerts = signal<AlertDto[]>([]);
  loading = signal(true);

  constructor(
    public authService: AuthService,
    private portfolioService: PortfolioService,
    private walletService: WalletService,
    private transactionService: TransactionService,
    private alertService: AlertService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);

    this.portfolioService.getSummary().subscribe({
      next: (data) => {
        this.portfolio.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading portfolio:', err);
        this.loading.set(false);
      }
    });

    this.walletService.getWallets().subscribe({
      next: (data) => this.wallets.set(data),
      error: (err) => console.error('Error loading wallets:', err)
    });

    this.transactionService.getTransactions().subscribe({
      next: (data) => this.recentTransactions.set(data.slice(0, 5)),
      error: (err) => console.error('Error loading transactions:', err)
    });

    this.alertService.getAlerts().subscribe({
      next: (data) => this.alerts.set(data),
      error: (err) => console.error('Error loading alerts:', err)
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2
    }).format(value);
  }

  formatPercent(value: number): string {
    return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`;
  }

  getPerformanceClass(value: number): string {
    return value >= 0 ? 'positive' : 'negative';
  }

  logout(): void {
    this.authService.logout();
  }
}