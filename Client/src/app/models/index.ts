// Auth models
export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  password: string;
}

export interface AuthResponseDto {
  token: string;
  userId: string;
  email: string;
}

// Wallet models
export interface WalletDto {
  id: string;
  name: string;
  description: string;
  totalValue: number;
}

export interface CreateWalletDto {
  name: string;
  description?: string;
}

// Transaction models
export interface TransactionDto {
  id: string;
  walletId: string;
  coinId: string;
  symbol: string;
  type: string;
  quantity: number;
  priceAtTime: number;
  totalValue: number;
  fee: number;
  feeCurrency: string;
  costBasisPerUnit?: number;
  costBasisSource?: string;
  executedAt: string;
  notes: string;
}

export interface TransactionCreateDto {
  walletId: string;
  coinId: string;
  symbol: string;
  type: string;
  quantity: number;
  priceAtTime: number;
  fee?: number;
  feeCurrency?: string;
  costBasisPerUnit?: number;
  executedAt?: string;
  notes: string;
}

export interface TransactionUpdateDto {
  quantity?: number;
  priceAtTime?: number;
  fee?: number;
  feeCurrency?: string;
  costBasisPerUnit?: number;
  executedAt?: string;
  notes?: string;
}

// Portfolio models
export interface PositionDto {
  symbol: string;
  quantity: number;
  avgCostBasis: number;
  currentPrice: number;
  valueUsdt: number;
  pnlUsdt: number;
  pnlPercent: number;
}

export interface PortfolioSummaryDto {
  positions: PositionDto[];
  totalValueUsdt: number;
  totalPnlUsdt: number;
}

// Alert models
export enum AlertDirection {
  Above = 0,
  Below = 1
}

export interface AlertDto {
  id: string;
  symbol: string;
  targetPrice: number;
  direction: AlertDirection;
  isTriggered: boolean;
  triggeredAt?: string;
  createdAt: string;
}

export interface CreateAlertDto {
  symbol: string;
  targetPrice: number;
  direction: AlertDirection;
}