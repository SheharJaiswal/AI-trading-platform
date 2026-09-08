export interface HealthStatus {
  status: string;
  mode: 'paper' | string;
  marketProvider: string;
  persistence: boolean;
}

export interface PositionSnapshot {
  id: string;
  symbol: string;
  quantity: number;
  averageEntryPrice: number;
  stopLoss?: number;
}

export interface PortfolioSnapshot {
  cash: number;
  positions?: PositionSnapshot[];
  unrealizedPnl?: number;
  realizedPnl?: number;
  [key: string]: unknown;
}

export interface AlertSnapshot {
  key: string;
  severity: 'Info' | 'Warning' | 'High' | 'Critical' | string;
  message: string;
  createdAt: string;
  symbol?: string;
}

export interface MarketQuote {
  symbol: string;
  price: number;
  timestamp?: string;
  provider?: string;
  [key: string]: unknown;
}

export interface Recommendation {
  decision: string;
  reason?: string;
  confidence?: number;
  [key: string]: unknown;
}

export interface ApiError {
  errorCode?: string;
  message?: string;
  [key: string]: unknown;
}
