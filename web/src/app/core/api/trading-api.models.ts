export interface HealthStatus {
  status: string;
  mode: 'paper' | string;
  marketProvider: string;
  persistence: boolean;
}

export interface PortfolioSnapshot {
  cash: number;
  positions?: unknown[];
  realizedPnl?: number;
  [key: string]: unknown;
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
