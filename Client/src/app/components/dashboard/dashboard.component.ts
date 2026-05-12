import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { toSignal } from '@angular/core/rxjs-interop';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
  ApexAxisChartSeries, ApexChart, ApexXAxis, ApexStroke,
  ApexGrid, ApexTooltip, ApexFill, ApexDataLabels
} from 'ng-apexcharts';
import { PortfolioService } from '../../services/portfolio.service';
import { ToastService } from '../../services/toast.service';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';

function generatePlaceholderSeries(baseValue: number): { x: number; y: number }[] {
  const points: { x: number; y: number }[] = [];
  const now = Date.now();
  let val = baseValue * 0.85;
  for (let i = 29; i >= 0; i--) {
    val += (Math.random() - 0.46) * baseValue * 0.03;
    val = Math.max(val, baseValue * 0.5);
    points.push({ x: now - i * 86400000, y: Math.round(val * 100) / 100 });
  }
  points.push({ x: now, y: baseValue });
  return points;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [NgApexchartsModule, LoadingSpinnerComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private portfolioService = inject(PortfolioService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  loading = signal(true);
  portfolio = toSignal(this.portfolioService.getSummary(), { initialValue: null });

  chartSeries = signal<ApexAxisChartSeries>([{ name: 'Portfolio Value', data: [] }]);

  chartOptions: {
    chart: ApexChart;
    xaxis: ApexXAxis;
    stroke: ApexStroke;
    grid: ApexGrid;
    tooltip: ApexTooltip;
    fill: ApexFill;
    dataLabels: ApexDataLabels;
    colors: string[];
  } = {
    chart: { type: 'area', height: 180, background: 'transparent', toolbar: { show: false }, sparkline: { enabled: false }, animations: { enabled: true } },
    xaxis: { type: 'datetime', labels: { style: { colors: '#8892a4', fontSize: '11px' }, datetimeUTC: false }, axisBorder: { show: false }, axisTicks: { show: false } },
    stroke: { curve: 'smooth', width: 2 },
    grid: { borderColor: '#1e2d45', strokeDashArray: 3, xaxis: { lines: { show: false } } },
    tooltip: { theme: 'dark', x: { format: 'MMM dd' }, y: { formatter: (v: number) => '$' + v.toLocaleString('en-US', { minimumFractionDigits: 2 }) } },
    fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.25, opacityTo: 0, stops: [0, 100] } },
    dataLabels: { enabled: false },
    colors: ['#F5A623']
  };

  ngOnInit(): void {
    this.titleService.setTitle('Dashboard — Investee');
    this.portfolioService.getSummary().subscribe({
      next: data => {
        const base = data.totalValueUsdt > 0 ? data.totalValueUsdt : 10000;
        this.chartSeries.set([{ name: 'Portfolio Value', data: generatePlaceholderSeries(base) }]);
        this.loading.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to load portfolio');
        this.loading.set(false);
      }
    });
  }

  formatCurrency(v: number): string {
    return '$' + v.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatPercent(v: number): string {
    return (v >= 0 ? '+' : '') + v.toFixed(2) + '%';
  }

  pnlClass(v: number): string {
    return v >= 0 ? 'badge-success' : 'badge-danger';
  }
}
