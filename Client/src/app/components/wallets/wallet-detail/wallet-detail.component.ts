import { Component, inject, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { forkJoin, Subscription } from 'rxjs';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  ApexChart, ApexStroke, ApexFill, ApexTooltip,
  ApexDataLabels, ApexLegend, ApexPlotOptions, ApexNonAxisChartSeries,
  ApexXAxis, ApexYAxis, ApexGrid, ApexTheme
} from 'ng-apexcharts';
import { WalletService } from '../../../services/wallet.service';
import { TransactionService } from '../../../services/transaction.service';
import { ToastService } from '../../../services/toast.service';
import { LoadingSpinnerComponent } from '../../shared/loading-spinner/loading-spinner.component';
import { WalletDetailsDto, TransactionDto, HistoryPoint } from '../../../models';

export interface PeriodOption { label: string; days: number; }

@Component({
  selector: 'app-wallet-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingSpinnerComponent, NgApexchartsModule],
  templateUrl: './wallet-detail.component.html',
  styleUrl: './wallet-detail.component.css'
})
export class WalletDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private walletService = inject(WalletService);
  private transactionService = inject(TransactionService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  wallet = signal<WalletDetailsDto | null>(null);
  history = signal<HistoryPoint[]>([]);
  transactions = signal<TransactionDto[]>([]);
  loading = signal(true);
  historyLoading = signal(false);
  selectedPeriod = signal(90);

  private walletId = '';
  private routeSub?: Subscription;

  readonly periods: PeriodOption[] = [
    { label: '1W', days: 7 },
    { label: '1M', days: 30 },
    { label: '3M', days: 90 },
    { label: '6M', days: 180 },
    { label: '1Y', days: 365 },
    { label: 'ALL', days: 365 },
  ];

  totalInvested = computed(() => {
    const w = this.wallet();
    if (!w) return 0;
    return w.totalValue - w.pnl;
  });

  allocationSeries = computed<ApexNonAxisChartSeries>(() => {
    const assets = this.wallet()?.assets ?? [];
    return assets.map(a => Number(a.value.toFixed(2)));
  });

  allocationLabels = computed<string[]>(() => {
    return (this.wallet()?.assets ?? []).map(a => a.symbol);
  });

  historySeries = computed(() => {
    return this.history().map(p => ({
      x: new Date(p.date).getTime(),
      y: Number(p.value.toFixed(2))
    }));
  });

  // --- Donut chart config ---
  readonly donutChart: ApexChart = {
    type: 'donut',
    height: 260,
    background: 'transparent',
    animations: { enabled: false },
    accessibility: { enabled: false }
  };
  readonly donutDataLabels: ApexDataLabels = { enabled: false };
  readonly donutLegend: ApexLegend = { show: false };
  readonly donutPlotOptions: ApexPlotOptions = {
    pie: {
      donut: {
        size: '68%',
        labels: {
          show: true,
          total: {
            show: true,
            label: 'TOTAL',
            fontSize: '11px',
            fontWeight: 600,
            color: '#8a9bb0',
            formatter: () => {
              const w = this.wallet();
              return w ? '$' + w.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '$0.00';
            }
          },
          value: { fontSize: '18px', fontWeight: 700, color: '#ffffff' }
        }
      }
    }
  };
  readonly donutTheme: ApexTheme = { mode: 'dark' };

  // --- Area chart config ---
  readonly areaChart: ApexChart = {
    type: 'area',
    height: 260,
    background: 'transparent',
    toolbar: { show: false },
    zoom: { enabled: false },
    animations: { enabled: false },
    accessibility: { enabled: false }
  };
  readonly areaStroke: ApexStroke = { curve: 'smooth', width: 2 };
  readonly areaFill: ApexFill = {
    type: 'gradient',
    gradient: { shadeIntensity: 1, opacityFrom: 0.2, opacityTo: 0, stops: [0, 100] }
  };
  readonly areaXAxis: ApexXAxis = {
    type: 'datetime',
    labels: { style: { colors: '#8a9bb0', fontSize: '11px' } },
    axisBorder: { show: false },
    axisTicks: { show: false }
  };
  readonly areaYAxis: ApexYAxis = {
    labels: {
      style: { colors: '#8a9bb0', fontSize: '11px' },
      formatter: (v: number) => '$' + v.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
    }
  };
  readonly areaDataLabels: ApexDataLabels = { enabled: false };
  readonly areaGrid: ApexGrid = {
    borderColor: '#1e2d40',
    strokeDashArray: 4,
    yaxis: { lines: { show: true } },
    xaxis: { lines: { show: false } }
  };
  readonly areaTooltip: ApexTooltip = {
    theme: 'dark',
    x: { format: 'MMM dd, yyyy' },
    y: { formatter: (v: number) => '$' + v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
  };
  readonly areaTheme: ApexTheme = { mode: 'dark' };

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.walletId = id;
        this.load(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  load(id: string): void {
    this.loading.set(true);
    forkJoin({
      wallet: this.walletService.getWallet(id),
      history: this.walletService.getWalletHistory(id, this.selectedPeriod()),
      txs: this.transactionService.getTransactions({ walletId: id, pageSize: 50 })
    }).subscribe({
      next: ({ wallet, history, txs }) => {
        this.wallet.set(wallet);
        this.history.set(history.points);
        this.transactions.set(txs.items);
        this.titleService.setTitle(`${wallet.name} — Investee`);
        this.loading.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to load wallet');
        this.loading.set(false);
      }
    });
  }

  changePeriod(days: number): void {
    this.selectedPeriod.set(days);
    this.historyLoading.set(true);
    this.walletService.getWalletHistory(this.walletId, days).subscribe({
      next: h => { this.history.set(h.points); this.historyLoading.set(false); },
      error: () => this.historyLoading.set(false)
    });
  }

  refresh(): void {
    if (this.walletId) this.load(this.walletId);
  }

  deleteWallet(): void {
    const w = this.wallet();
    if (!w) return;
    const msg = w.assetCount > 0
      ? `Delete wallet "${w.name}"? It contains ${w.assetCount} asset(s) and all transaction history will be lost. This cannot be undone.`
      : `Delete wallet "${w.name}"? This cannot be undone.`;
    if (!confirm(msg)) return;
    this.walletService.deleteWallet(w.id).subscribe({
      next: () => { this.toastService.success('Wallet deleted'); this.router.navigate(['/wallets']); },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to delete wallet')
    });
  }

  deleteTransaction(id: string): void {
    if (!confirm('Delete this transaction? This cannot be undone.')) return;
    this.transactionService.deleteTransaction(id).subscribe({
      next: () => {
        this.transactions.set(this.transactions().filter(t => t.id !== id));
        this.toastService.success('Transaction deleted');
        this.load(this.walletId);
      },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to delete transaction')
    });
  }

  private readonly palette = [
    '#3b82f6', // blue
    '#10b981', // emerald
    '#f59e0b', // amber
    '#8b5cf6', // violet
    '#ef4444', // red
    '#06b6d4', // cyan
    '#f97316', // orange
    '#ec4899', // pink
    '#84cc16', // lime
    '#14b8a6', // teal
  ];

  getAssetColor(symbol: string): string {
    const assets = this.wallet()?.assets ?? [];
    const idx = assets.findIndex(a => a.symbol === symbol);
    return this.palette[idx >= 0 ? idx % this.palette.length : 0];
  }

  getAssetPercentage(value: number): string {
    const total = this.wallet()?.totalValue ?? 0;
    if (total === 0) return '0.0%';
    return (value / total * 100).toFixed(1) + '%';
  }

  formatCurrency(value: number): string {
    return '$' + value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatPercent(value: number): string {
    return (value >= 0 ? '+' : '') + value.toFixed(2) + '%';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' });
  }

  formatQuantity(qty: number): string {
    return qty.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 8 });
  }
}
