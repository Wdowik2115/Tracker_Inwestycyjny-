import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { TransactionService } from '../../services/transaction.service';
import { WalletService } from '../../services/wallet.service';
import { ToastService } from '../../services/toast.service';
import { ModalComponent } from '../shared/modal/modal.component';
import { LoadingSpinnerComponent } from '../shared/loading-spinner/loading-spinner.component';
import { TransactionCreateDto, TransactionDto, TransactionUpdateDto, WalletDto } from '../../models';
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

  loading = signal(true);
  transactions = signal<TransactionDto[]>([]);
  wallets = toSignal(this.walletService.getWallets(), { initialValue: [] as WalletDto[] });

  filterAsset = signal('');
  filterSide = signal<'all' | 'buy' | 'sell'>('all');
  page = signal(0);
  readonly PAGE_SIZE = 20;

  addModalOpen = signal(false);
  editModalOpen = signal(false);
  editingTx = signal<TransactionDto | null>(null);
  submitting = signal(false);

  form = {
    symbol: signal(''),
    type: signal<'buy' | 'sell'>('buy'),
    quantity: signal(''),
    priceAtTime: signal(''),
    executedAt: signal(''),
    walletId: signal(''),
    notes: signal('')
  };

  editForm = {
    priceAtTime: signal(''),
    executedAt: signal(''),
    notes: signal(''),
    costBasisPerUnit: signal('')
  };

  filtered = computed(() => {
    const asset = this.filterAsset().toLowerCase();
    const side = this.filterSide();
    return this.transactions().filter(t => {
      const matchAsset = !asset || t.symbol.toLowerCase().includes(asset);
      const matchSide = side === 'all' || t.type.toLowerCase() === side;
      return matchAsset && matchSide;
    });
  });

  paged = computed(() => {
    const p = this.page();
    return this.filtered().slice(p * this.PAGE_SIZE, (p + 1) * this.PAGE_SIZE);
  });

  totalPages = computed(() => Math.ceil(this.filtered().length / this.PAGE_SIZE));

  ngOnInit(): void {
    this.titleService.setTitle('Transactions — Investee');
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.transactionService.getTransactions().subscribe({
      next: data => { this.transactions.set(data); this.loading.set(false); },
      error: e => { this.toastService.error(e.error?.message ?? 'Failed to load transactions'); this.loading.set(false); }
    });
  }

  openAddModal(): void {
    this.addModalOpen.set(true);
  }

  closeAddModal(): void {
    this.addModalOpen.set(false);
    this.resetForm();
  }

  resetForm(): void {
    this.form.symbol.set('');
    this.form.type.set('buy');
    this.form.quantity.set('');
    this.form.priceAtTime.set('');
    this.form.executedAt.set('');
    this.form.walletId.set('');
    this.form.notes.set('');
  }

  submitAdd(): void {
    const qty = parseFloat(this.form.quantity());
    const price = parseFloat(this.form.priceAtTime());
    if (!this.form.symbol() || isNaN(qty) || isNaN(price) || !this.form.walletId()) {
      this.toastService.error('Please fill in all required fields');
      return;
    }
    const dto: TransactionCreateDto = {
      symbol: this.form.symbol().toUpperCase(),
      coinId: this.form.symbol().toLowerCase(),
      type: this.form.type(),
      quantity: qty,
      priceAtTime: price,
      walletId: this.form.walletId(),
      executedAt: this.form.executedAt() || undefined,
      notes: this.form.notes()
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
    this.editForm.priceAtTime.set(String(tx.priceAtTime));
    this.editForm.executedAt.set(tx.executedAt ? new Date(tx.executedAt).toISOString().slice(0, 16) : '');
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

    const price = parseFloat(this.editForm.priceAtTime());
    if (isNaN(price) || price <= 0) {
      this.toastService.error('Price must be a positive number');
      return;
    }

    const dto: TransactionUpdateDto = {
      priceAtTime: price,
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

  prevPage(): void { this.page.update(p => Math.max(0, p - 1)); }
  nextPage(): void { this.page.update(p => Math.min(this.totalPages() - 1, p + 1)); }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  formatCurrency(n: number): string {
    return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
