import { Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { WatchlistService } from '../../services/watchlist.service';
import { TransactionService } from '../../services/transaction.service';
import { ToastService } from '../../services/toast.service';
import { CoinSearchDto, WatchlistItemDto, AddToWatchlistDto } from '../../models';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [],
  templateUrl: './watchlist.component.html',
  styleUrl: './watchlist.component.css'
})
export class WatchlistComponent implements OnInit {
  private watchlistService = inject(WatchlistService);
  private transactionService = inject(TransactionService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);

  watchlist = signal<WatchlistItemDto[]>([]);
  loading = signal(true);
  adding = signal(false);

  coinQuery = signal('');
  coinSuggestions = signal<CoinSearchDto[]>([]);
  coinSearching = signal(false);
  showDropdown = signal(false);
  selectedCoin = signal<CoinSearchDto | null>(null);

  private coinSearch$ = new Subject<string>();

  constructor() {
    this.coinSearch$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(q => {
        if (q.length < 1) {
          this.coinSuggestions.set([]);
          this.coinSearching.set(false);
          return of([]);
        }
        this.coinSearching.set(true);
        return this.transactionService.searchCoins(q).pipe(catchError(() => of([])));
      }),
      takeUntilDestroyed()
    ).subscribe((results: CoinSearchDto[]) => {
      this.coinSuggestions.set(results);
      this.coinSearching.set(false);
      this.showDropdown.set(results.length > 0);
    });
  }

  ngOnInit(): void {
    this.titleService.setTitle('Watchlist — Investee');
    this.loadWatchlist();
  }

  onCoinInput(value: string): void {
    this.coinQuery.set(value);
    this.selectedCoin.set(null);
    if (!value.trim()) {
      this.showDropdown.set(false);
      this.coinSuggestions.set([]);
    }
    this.coinSearch$.next(value.trim());
  }

  selectCoin(coin: CoinSearchDto): void {
    this.selectedCoin.set(coin);
    this.coinQuery.set(`${coin.symbol} — ${coin.name}`);
    this.showDropdown.set(false);
    this.coinSuggestions.set([]);
    this.addToWatchlist(coin);
  }

  closeDropdownDelayed(): void {
    setTimeout(() => this.showDropdown.set(false), 150);
  }

  loadWatchlist(): void {
    this.loading.set(true);
    this.watchlistService.getWatchlist().subscribe({
      next: items => { this.watchlist.set(items); this.loading.set(false); },
      error: err => { this.toastService.error(err.error?.message ?? 'Failed to load watchlist'); this.loading.set(false); }
    });
  }

  addToWatchlist(coin: CoinSearchDto): void {
    this.adding.set(true);
    const dto: AddToWatchlistDto = {
      symbol: coin.symbol,
      coinId: coin.coinId,
      imageUrl: coin.imageUrl
    };

    this.watchlistService.addToWatchlist(dto).subscribe({
      next: item => {
        this.watchlist.update(items => items.find(i => i.id === item.id) ? items : [item, ...items]);
        this.coinQuery.set('');
        this.selectedCoin.set(null);
        this.toastService.success(`${coin.symbol} added to watchlist`);
        this.adding.set(false);
      },
      error: err => {
        this.toastService.error(err.error?.message ?? `Failed to add ${coin.symbol}`);
        this.adding.set(false);
      }
    });
  }

  removeFromWatchlist(id: string): void {
    this.watchlistService.removeFromWatchlist(id).subscribe({
      next: () => { this.watchlist.update(items => items.filter(i => i.id !== id)); this.toastService.success('Removed from watchlist'); },
      error: err => this.toastService.error(err.error?.message ?? 'Failed to remove from watchlist')
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatCurrency(n: number): string {
    if (n >= 1) return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    if (n >= 0.001) return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 3 });
    if (n > 0) return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 8 });
    return '$0.00';
  }
}
