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

// User models
export interface UserDto {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  preferredCurrency: string;
  createdAt: string;
}

export interface UpdateProfileDto {
  firstName?: string;
  lastName?: string;
  preferredCurrency: string;
}

export interface ChangePasswordDto {
  oldPassword: string;
  newPassword: string;
}

// Wallet models
export interface WalletDto {
  id: string;
  name: string;
  description: string;
  totalValue: number;
  assetCount: number;
  pnl: number;
  pnlPercent: number;
}

export interface WalletDetailsDto extends WalletDto {
  assets: PositionDto[];
  realizedPnl: number;
}

export interface CreateWalletDto {
  name: string;
  description?: string;
}

export interface UpdateWalletDto {
  name: string;
  description?: string;
}

// Transaction models
export interface TransactionDto {
  id: string;
  walletId: string;
  walletName: string;
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
  name: string;
  quantity: number;
  avgCostBasis: number;
  currentPrice: number;
  value: number;
  pnl: number;
  pnlPercent: number;
}

export interface PortfolioSummaryDto {
  positions: PositionDto[];
  totalValue: number;
  totalPnl: number;
  totalInvested: number;
}

// History models
export interface HistoryPoint {
  date: string;   // ISO date string from backend
  value: number;
}

export interface WalletHistoryDto {
  walletId: string;
  points: HistoryPoint[];
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

export interface UpdateAlertDto {
  targetPrice?: number;
  direction?: AlertDirection;
}
