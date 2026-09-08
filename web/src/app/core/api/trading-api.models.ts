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
  exchange: string;
  instrumentToken: string;
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  lastTradedPrice: number;
  volume: number;
  source: string;
  [key: string]: unknown;
}

export interface Recommendation {
  symbol: string;
  action: 'Buy' | 'Hold' | 'Sell' | 'NoDecision' | string;
  referencePrice: number;
  expectedReturn?: number;
  confidence: number;
  horizonDays: number;
  supportingSignals: string[];
  riskFactors: string[];
  generatedAt: string;
  strategyVersion: string;
  [key: string]: unknown;
}

export interface AiResearchRequest {
  symbol: string;
  question: string;
  evidence: string[];
}

export interface AiResearchResult {
  provider: string;
  summary: string;
  risks: string[];
  confidence: number;
  generatedAt: string;
}

export interface RiskResult {
  decision: 'Approved' | 'RiskBlocked' | 'InsufficientData' | string;
  reason?: string | null;
}

export interface FillSnapshot {
  orderId: string;
  symbol: { value: string; instrumentToken?: string | null };
  side: 'Buy' | 'Sell' | string;
  quantity: number;
  price: number;
  timestamp: string;
  source: string;
}

export interface PaperTradeResult {
  risk: RiskResult;
  fill?: FillSnapshot | null;
}

export interface ApiError {
  errorCode?: string;
  message?: string;
  risk?: RiskResult;
  [key: string]: unknown;
}
