import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Title } from '@angular/platform-browser';
import { ReportService } from '../../services/report.service';
import { ToastService } from '../../services/toast.service';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { ReportDto } from '../../models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css'
})
export class ReportsComponent implements OnInit {
  private reportService = inject(ReportService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  reports = signal<ReportDto[]>([]);
  loading = signal(true);
  generating = signal(false);
  deletingId = signal<string | null>(null);

  ngOnInit(): void {
    this.titleService.setTitle('Reports — Investee');
    this.loadReports();
  }

  loadReports(): void {
    this.loading.set(true);
    this.reportService.getReports().subscribe({
      next: data => { this.reports.set(data); this.loading.set(false); },
      error: () => { this.toastService.error('Failed to load reports'); this.loading.set(false); }
    });
  }

  download(report: ReportDto): void {
    this.reportService.downloadReport(report.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = report.fileName;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toastService.error('Failed to download report')
    });
  }

  delete(report: ReportDto): void {
    if (!confirm(`Delete "${report.title}"? This cannot be undone.`)) return;
    this.deletingId.set(report.id);
    this.reportService.deleteReport(report.id).subscribe({
      next: () => {
        this.reports.update(list => list.filter(r => r.id !== report.id));
        this.toastService.success('Report deleted');
        this.deletingId.set(null);
      },
      error: () => {
        this.toastService.error('Failed to delete report');
        this.deletingId.set(null);
      }
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleString('en-US', {
      month: 'short', day: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
