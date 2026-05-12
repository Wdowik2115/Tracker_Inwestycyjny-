import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { WalletService } from '../../services/wallet.service';
import { WalletDto, CreateWalletDto } from '../../models';

@Component({
  selector: 'app-wallets',
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="page-container">
      <header class="page-header">
        <h1>Wallets</h1>
        <button class="btn-primary" (click)="showCreateModal.set(true)">+ Create Wallet</button>
      </header>

      <div class="wallets-grid">
        @for (wallet of wallets(); track wallet.id) {
          <div class="wallet-card">
            <div class="wallet-header">
              <h3>{{ wallet.name }}</h3>
              <button class="btn-delete" (click)="deleteWallet(wallet.id)">🗑️</button>
            </div>
            <p class="wallet-description">{{ wallet.description || 'No description' }}</p>
            <div class="wallet-value">{{ formatCurrency(wallet.totalValue) }}</div>
          </div>
        }
      </div>

      @if (showCreateModal()) {
        <div class="modal-overlay" (click)="showCreateModal.set(false)">
          <div class="modal" (click)="$event.stopPropagation()">
            <h2>Create New Wallet</h2>
            <form (ngSubmit)="createWallet()">
              <div class="form-group">
                <label>Wallet Name</label>
                <input 
                  type="text" 
                  [value]="newWalletName()"
                  (input)="newWalletName.set($any($event.target).value)"
                  required
                />
              </div>
              <div class="form-group">
                <label>Description</label>
                <textarea 
                  [value]="newWalletDesc()"
                  (input)="newWalletDesc.set($any($event.target).value)"
                  rows="3"
                ></textarea>
              </div>
              <div class="modal-actions">
                <button type="button" class="btn-secondary" (click)="showCreateModal.set(false)">Cancel</button>
                <button type="submit" class="btn-primary">Create</button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-container {
      min-height: 100vh;
      background: #0a0e17;
      padding: 40px 24px;
      max-width: 1400px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }
    .page-header h1 {
      font-size: 32px;
      font-weight: 700;
      color: #ffffff;
      margin: 0;
    }
    .wallets-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 24px;
    }
    .wallet-card {
      background: #0f1419;
      border: 1px solid #1a1f2e;
      border-radius: 16px;
      padding: 24px;
      transition: all 0.3s ease;
    }
    .wallet-card:hover {
      border-color: #F5A623;
      transform: translateY(-4px);
    }
    .wallet-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .wallet-header h3 {
      color: #ffffff;
      font-size: 18px;
      font-weight: 600;
      margin: 0;
    }
    .btn-delete {
      background: transparent;
      border: none;
      font-size: 20px;
      cursor: pointer;
      opacity: 0.6;
      transition: opacity 0.3s;
    }
    .btn-delete:hover {
      opacity: 1;
    }
    .wallet-description {
      color: #8b92a6;
      font-size: 14px;
      margin: 0 0 16px 0;
    }
    .wallet-value {
      color: #F5A623;
      font-size: 24px;
      font-weight: 700;
    }
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.8);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }
    .modal {
      background: #0f1419;
      border: 1px solid #1a1f2e;
      border-radius: 16px;
      padding: 32px;
      max-width: 500px;
      width: 100%;
    }
    .modal h2 {
      color: #ffffff;
      margin: 0 0 24px 0;
    }
    .form-group {
      margin-bottom: 20px;
    }
    .form-group label {
      display: block;
      color: #8b92a6;
      font-size: 14px;
      margin-bottom: 8px;
    }
    .form-group input,
    .form-group textarea {
      width: 100%;
      padding: 12px;
      background: #1a1f2e;
      border: 1px solid #2a2f3e;
      border-radius: 8px;
      color: #ffffff;
      font-size: 14px;
      box-sizing: border-box;
    }
    .form-group input:focus,
    .form-group textarea:focus {
      outline: none;
      border-color: #F5A623;
    }
    .modal-actions {
      display: flex;
      gap: 12px;
      justify-content: flex-end;
      margin-top: 24px;
    }
    .btn-primary {
      background: #F5A623;
      color: #000000;
      border: none;
      padding: 12px 24px;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
    }
    .btn-secondary {
      background: transparent;
      color: #ffffff;
      border: 1px solid #2a2f3e;
      padding: 12px 24px;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
    }
  `]
})
export class WalletsComponent implements OnInit {
  wallets = signal<WalletDto[]>([]);
  showCreateModal = signal(false);
  newWalletName = signal('');
  newWalletDesc = signal('');

  constructor(private walletService: WalletService) {}

  ngOnInit(): void {
    this.loadWallets();
  }

  loadWallets(): void {
    this.walletService.getWallets().subscribe({
      next: (data) => this.wallets.set(data),
      error: (err) => console.error('Error loading wallets:', err)
    });
  }

  createWallet(): void {
    const dto: CreateWalletDto = {
      name: this.newWalletName(),
      description: this.newWalletDesc()
    };

    this.walletService.createWallet(dto).subscribe({
      next: () => {
        this.showCreateModal.set(false);
        this.newWalletName.set('');
        this.newWalletDesc.set('');
        this.loadWallets();
      },
      error: (err) => console.error('Error creating wallet:', err)
    });
  }

  deleteWallet(id: string): void {
    if (confirm('Are you sure you want to delete this wallet?')) {
      this.walletService.deleteWallet(id).subscribe({
        next: () => this.loadWallets(),
        error: (err) => console.error('Error deleting wallet:', err)
      });
    }
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(value);
  }
}