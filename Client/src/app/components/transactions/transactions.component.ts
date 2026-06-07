import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TransactionService } from '../../services/transaction.service';
import { WalletService } from '../../services/wallet.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../shared/modal/modal.component';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { CoinSearchDto, TransactionCreateDto, TransactionDto, TransactionUpdateDto, WalletDto } from '../../models';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [ModalComponent, LoadingSpinnerComponent],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.css'
})
export class TransactionsComponent implements OnInit {
  private transactionService = inject(TransactionService);
  private walletService = inject(WalletService);
  private toastService = inject(ToastService);
  private titleService = inject(Title);
  private route = inject(ActivatedRoute);

  loading = signal(true);
  transactions = signal<TransactionDto[]>([]);
  totalCount = signal(0);
  wallets = toSignal(this.walletService.getWallets(), { initialValue: [] as WalletDto[] });

  filterAsset = signal('');
  filterWallet = signal('');
  filterStartDate = signal('');
  filterEndDate = signal('');

  page = signal(1);
  readonly PAGE_SIZE = 20;

  addModalOpen = signal(false);
  editModalOpen = signal(false);
  editingTx = signal<TransactionDto | null>(null);
  submitting = signal(false);

  // Coin search
  coinQuery = signal('');
  coinSuggestions = signal<CoinSearchDto[]>([]);
  coinSearching = signal(false);
  showDropdown = signal(false);
  private coinSearch$ = new Subject<string>();

  form = {
    symbol: signal(''),
    coinId: signal(''),
    imageUrl: signal(''),
    type: signal<'buy' | 'sell'>('buy'),
    quantity: signal(''),
    priceAtTime: signal(''),
    fee: signal('0'),
    feeCurrency: signal('USDT'),
    executedAt: signal(''),
    walletId: signal(''),
    notes: signal('')
  };

  editForm = {
    quantity: signal(''),
    priceAtTime: signal(''),
    fee: signal(''),
    feeCurrency: signal(''),
    executedAt: signal(''),
    notes: signal(''),
    costBasisPerUnit: signal('')
  };

  totalPages = computed(() => Math.ceil(this.totalCount() / this.PAGE_SIZE));

  constructor() {
    // Reload when filters or page change
    effect(() => {
      this.filterAsset();
      this.filterWallet();
      this.filterStartDate();
      this.filterEndDate();
      this.page();
      this.load();
    });

    // Debounced coin search
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
    this.titleService.setTitle('Transactions — Investee');
    this.handleQueryParams();
  }

  private handleQueryParams(): void {
    const walletId = this.route.snapshot.queryParamMap.get('walletId');
    const add = this.route.snapshot.queryParamMap.get('add');
    if (walletId) this.form.walletId.set(walletId);
    if (add === 'true') this.openAddModal();
  }

  load(): void {
    this.loading.set(true);

    const params: any = { page: this.page(), pageSize: this.PAGE_SIZE };
    if (this.filterAsset()) params.symbol = this.filterAsset();
    if (this.filterWallet()) params.walletId = this.filterWallet();
    if (this.filterStartDate()) params.startDate = this.filterStartDate();
    if (this.filterEndDate()) params.endDate = this.filterEndDate();

    this.transactionService.getTransactions(params).subscribe({
      next: data => {
        this.transactions.set(data.items);
        this.totalCount.set(data.totalCount);
        this.loading.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to load transactions');
        this.loading.set(false);
      }
    });
  }

  onCoinInput(value: string): void {
    this.coinQuery.set(value);
    this.form.symbol.set('');
    this.form.coinId.set('');
    this.form.imageUrl.set('');
    if (!value.trim()) {
      this.showDropdown.set(false);
      this.coinSuggestions.set([]);
    }
    this.coinSearch$.next(value.trim());
  }

  selectCoin(coin: CoinSearchDto): void {
    this.form.symbol.set(coin.symbol);
    this.form.coinId.set(coin.coinId);
    this.form.imageUrl.set(coin.imageUrl ?? '');
    this.coinQuery.set(`${coin.symbol} — ${coin.name}`);
    this.showDropdown.set(false);
    this.coinSuggestions.set([]);
  }

  closeDropdownDelayed(): void {
    setTimeout(() => this.showDropdown.set(false), 150);
  }

  openAddModal(): void {
    this.addModalOpen.set(true);
  }

  closeAddModal(): void {
    this.addModalOpen.set(false);
    this.resetForm();
  }

  resetForm(): void {
    this.coinQuery.set('');
    this.coinSuggestions.set([]);
    this.showDropdown.set(false);
    this.form.symbol.set('');
    this.form.coinId.set('');
    this.form.imageUrl.set('');
    this.form.type.set('buy');
    this.form.quantity.set('');
    this.form.priceAtTime.set('');
    this.form.fee.set('0');
    this.form.feeCurrency.set('USDT');
    this.form.executedAt.set('');
    this.form.walletId.set('');
    this.form.notes.set('');
  }

  submitAdd(): void {
    const qty = parseFloat(this.form.quantity());
    const price = parseFloat(this.form.priceAtTime());
    const fee = parseFloat(this.form.fee());

    if (!this.form.symbol() || isNaN(qty) || isNaN(price) || !this.form.walletId()) {
      this.toastService.error('Please select a coin and fill in all required fields');
      return;
    }
    const dto: TransactionCreateDto = {
      symbol: this.form.symbol(),
      coinId: this.form.coinId(),
      type: this.form.type(),
      quantity: qty,
      priceAtTime: price,
      fee: isNaN(fee) ? 0 : fee,
      feeCurrency: this.form.feeCurrency(),
      walletId: this.form.walletId(),
      executedAt: this.form.executedAt() || undefined,
      notes: this.form.notes(),
      imageUrl: this.form.imageUrl() || undefined
    };
    this.submitting.set(true);
    this.transactionService.addTransaction(dto).subscribe({
      next: () => {
        this.toastService.success('Transaction added');
        this.closeAddModal();
        this.load();
        this.submitting.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to add transaction');
        this.submitting.set(false);
      }
    });
  }

  openEditModal(tx: TransactionDto): void {
    this.editingTx.set(tx);
    this.editForm.quantity.set(String(tx.quantity));
    this.editForm.priceAtTime.set(String(tx.priceAtTime));
    this.editForm.fee.set(String(tx.fee));
    this.editForm.feeCurrency.set(tx.feeCurrency);
    this.editForm.executedAt.set(tx.executedAt ?? '');
    this.editForm.notes.set(tx.notes ?? '');
    this.editForm.costBasisPerUnit.set(tx.costBasisPerUnit != null ? String(tx.costBasisPerUnit) : '');
    this.editModalOpen.set(true);
  }

  closeEditModal(): void {
    this.editModalOpen.set(false);
    this.editingTx.set(null);
  }

  submitEdit(): void {
    const tx = this.editingTx();
    if (!tx) return;

    const qty = parseFloat(this.editForm.quantity());
    const price = parseFloat(this.editForm.priceAtTime());
    const fee = parseFloat(this.editForm.fee());

    if (isNaN(qty) || qty <= 0 || isNaN(price) || price <= 0) {
      this.toastService.error('Quantity and price must be positive numbers');
      return;
    }

    const dto: TransactionUpdateDto = {
      quantity: qty,
      priceAtTime: price,
      fee: isNaN(fee) ? 0 : fee,
      feeCurrency: this.editForm.feeCurrency(),
      executedAt: this.editForm.executedAt() || undefined,
      notes: this.editForm.notes(),
      costBasisPerUnit: this.editForm.costBasisPerUnit() ? parseFloat(this.editForm.costBasisPerUnit()) : undefined
    };

    this.submitting.set(true);
    this.transactionService.updateTransaction(tx.id, dto).subscribe({
      next: () => {
        this.toastService.success('Transaction updated');
        this.closeEditModal();
        this.load();
        this.submitting.set(false);
      },
      error: e => {
        this.toastService.error(e.error?.message ?? 'Failed to update transaction');
        this.submitting.set(false);
      }
    });
  }

  deleteTx(id: string): void {
    if (!confirm('Delete this transaction? This cannot be undone.')) return;
    this.transactionService.deleteTransaction(id).subscribe({
      next: () => { this.toastService.success('Transaction deleted'); this.load(); },
      error: e => this.toastService.error(e.error?.message ?? 'Failed to delete transaction')
    });
  }

  prevPage(): void {
    if (this.page() > 1) this.page.update(p => p - 1);
  }

  nextPage(): void {
    if (this.page() < this.totalPages()) this.page.update(p => p + 1);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatCurrency(n: number): string {
    return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
