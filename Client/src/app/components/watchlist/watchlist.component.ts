import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Title } from '@angular/platform-browser';
import { WatchlistService } from '../../services/watchlist.service';
import { ToastService } from '../../services/toast.service';
import { WatchlistItemDto, AddToWatchlistDto } from '../../models';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, switchMap, catchError, of } from 'rxjs';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './watchlist.component.html',
  styleUrl: './watchlist.component.css'
})
export class WatchlistComponent implements OnInit {
  private watchlistService = inject(WatchlistService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  watchlist = signal<WatchlistItemDto[]>([]);
  loading = signal(true);
  adding = signal(false);

  newCoinSymbol = signal('');
  suggestions = signal<string[]>([]);
  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.titleService.setTitle('Watchlist — Investee');
    this.loadWatchlist();

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => this.watchlistService.getSuggestions(query).pipe(
        catchError(() => of([]))
      ))
    ).subscribe(results => {
      this.suggestions.set(results);
    });
  }

  onSymbolInput(value: string): void {
    this.newCoinSymbol.set(value);
    this.searchSubject.next(value);
  }

  selectSuggestion(symbol: string): void {
    this.newCoinSymbol.set(symbol);
    this.suggestions.set([]);
    this.addToWatchlist();
  }

  loadWatchlist(): void {
    this.loading.set(true);
    this.watchlistService.getWatchlist().subscribe({
      next: (items) => {
        this.watchlist.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.toastService.error(err.error?.message ?? 'Failed to load watchlist');
        this.loading.set(false);
      }
    });
  }

  addToWatchlist(): void {
    const symbol = this.newCoinSymbol().trim().toUpperCase();
    if (!symbol) return;

    this.adding.set(true);
    const dto: AddToWatchlistDto = {
      symbol: symbol,
      coinId: symbol.toLowerCase() // Simple mapping for now
    };

    this.watchlistService.addToWatchlist(dto).subscribe({
      next: (item) => {
        // If it was already on the list, the service returns the existing item
        this.watchlist.update(items => {
          const exists = items.find(i => i.id === item.id);
          if (exists) return items;
          return [item, ...items];
        });
        this.newCoinSymbol.set('');
        this.searchSubject.next(''); // Reset the search stream state
        this.suggestions.set([]);
        this.toastService.success(`${symbol} added to watchlist`);
        this.adding.set(false);
      },
      error: (err) => {
        this.toastService.error(err.error?.message ?? `Failed to add ${symbol}`);
        this.adding.set(false);
      }
    });
  }

  removeFromWatchlist(id: string): void {
    this.watchlistService.removeFromWatchlist(id).subscribe({
      next: () => {
        this.watchlist.update(items => items.filter(i => i.id !== id));
        this.toastService.success('Removed from watchlist');
      },
      error: (err) => {
        this.toastService.error(err.error?.message ?? 'Failed to remove from watchlist');
      }
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatCurrency(n: number): string {
    if (n >= 1) {
      return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    } else if (n >= 0.001) {
      // For DOGE ($0.081506) this will show $0.081 or $0.082
      return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 3 });
    } else if (n > 0) {
      // For extremely small coins, keep precision to avoid showing $0.000
      return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 8 });
    }
    return '$0.00';
  }
}
