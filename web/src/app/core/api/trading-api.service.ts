import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AlertSnapshot, HealthStatus, MarketQuote, PortfolioSnapshot, Recommendation } from './trading-api.models';

@Injectable({ providedIn: 'root' })
export class TradingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '';

  health(): Observable<HealthStatus> {
    return this.http.get<HealthStatus>(`${this.baseUrl}/health`);
  }

  portfolio(): Observable<PortfolioSnapshot> {
    return this.http.get<PortfolioSnapshot>(`${this.baseUrl}/api/portfolio`);
  }

  alerts(): Observable<AlertSnapshot[]> {
    return this.http.get<AlertSnapshot[]>(`${this.baseUrl}/api/alerts`);
  }

  quote(symbol: string): Observable<MarketQuote> {
    return this.http.get<MarketQuote>(`${this.baseUrl}/api/market/${encodeURIComponent(symbol)}/quote`);
  }

  recommendation(symbol: string): Observable<Recommendation> {
    return this.http.get<Recommendation>(`${this.baseUrl}/api/recommendations/${encodeURIComponent(symbol)}`);
  }
}
