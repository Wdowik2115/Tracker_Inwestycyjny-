import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { timer, Subscription, merge, forkJoin, of } from 'rxjs';
import { switchMap, map } from 'rxjs/operators';
import { NgApexchartsModule } from 'ng-apexcharts';
import { ApexChart, ApexStroke, ApexFill, ApexTooltip } from 'ng-apexcharts';
import { WalletService } from '../../services/wallet.service';
import { TransactionService } from '../../services/transaction.service';
import { UserService } from '../../services/user.service';
import { ToastService } from '../../services/toast.service';
import { ReportService } from '../../services/report.service';
import { WalletDto } from '../../models';

interface WalletCardData {
  wallet: WalletDto;
  sparklineSeries: { x: number; y: number }[];
  sparklineColor: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [NgApexchartsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private walletService = inject(WalletService);
  private transactionService = inject(TransactionService);
  private userService = inject(UserService);
  private toastService = inject(ToastService);
  private reportService = inject(ReportService);
  private titleService = inject(Title);

  private refreshSub?: Subscription;

  loading = signal(true);
  walletCards = signal<WalletCardData[]>([]);
  currency = signal('USD');
  generatingReport = signal(false);

  readonly sparklineChart: ApexChart = {
    type: 'area',
    height: 64,
    sparkline: { enabled: true },
    animations: { enabled: false }
  };
  readonly sparklineStroke: ApexStroke = { curve: 'smooth', width: 1.5 };
  readonly sparklineFill: ApexFill = {
    type: 'gradient',
    gradient: { shadeIntensity: 1, opacityFrom: 0.25, opacityTo: 0, stops: [0, 100] }
  };
  readonly sparklineTooltip: ApexTooltip = { enabled: false };

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard — Investee');

    this.userService.getProfile().subscribe({
      next: u => this.currency.set(u.preferredCurrency),
      error: () => {}
    });

    this.refreshSub = merge(
      timer(0, 30000),
      this.transactionService.transactionAdded$
    ).pipe(
      switchMap(() => this.walletService.getWallets()),
      switchMap(wallets => {
        if (wallets.length === 0) return of({ wallets, histories: [] as any[] });
        return forkJoin(wallets.map(w => this.walletService.getWalletHistory(w.id, 30))).pipe(
          map(histories => ({ wallets, histories }))
        );
      })
    ).subscribe({
      next: ({ wallets, histories }) => {
        const historyMap = new Map(histories.map((h: any) => [h.walletId, h.points as { date: string; value: number }[]]));

        this.walletCards.set(wallets.map(w => {
          const points = historyMap.get(w.id) ?? [];
          const series = points.map(p => ({
            x: new Date(p.date).getTime(),
            y: p.value
          }));
          return {
            wallet: w,
            sparklineSeries: series,
            sparklineColor: w.pnl >= 0 ? '#26a17b' : '#e74c3c'
          };
        }));

        this.loading.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to load dashboard');
        this.loading.set(false);
      }
    });
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

  generateReport(): void {
    this.generatingReport.set(true);
    this.reportService.generateAccountReport().subscribe({
      next: report => {
        this.reportService.downloadReport(report.id).subscribe({
          next: blob => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${report.title}.pdf`;
            a.click();
            URL.revokeObjectURL(url);
            this.toastService.success('Account report downloaded.');
            this.generatingReport.set(false);
          },
          error: () => {
            this.toastService.error('Report generated but download failed. Find it in Reports.');
            this.generatingReport.set(false);
          }
        });
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to generate report');
        this.generatingReport.set(false);
      }
    });
  }
}
