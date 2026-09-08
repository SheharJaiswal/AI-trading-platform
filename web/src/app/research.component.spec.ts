import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ResearchComponent } from './research.component';
import { TradingApiService } from './core/api/trading-api.service';

describe('ResearchComponent', () => {
  let fixture: ComponentFixture<ResearchComponent>;
  let component: ResearchComponent;
  let api: jasmine.SpyObj<TradingApiService>;

  const quote = (symbol: string) => ({ symbol, exchange: 'NSE', instrumentToken: '123', timestamp: '2026-09-08T10:00:00Z', open: 2480, high: 2520, low: 2470, close: 2500, lastTradedPrice: 2500, volume: 100000, source: 'demo' });
  const recommendation = { symbol: 'RELIANCE', action: 'Buy' as const, referencePrice: 2500, expectedReturn: 0.03, confidence: 0.8, horizonDays: 1, supportingSignals: ['PRICE_ABOVE_SMA20', 'RSI_NOT_OVERBOUGHT'], riskFactors: [], generatedAt: '2026-09-08T10:00:00Z', strategyVersion: 'baseline-v1' };

  beforeEach(async () => {
    api = jasmine.createSpyObj<TradingApiService>('TradingApiService', ['quote', 'recommendation']);
    await TestBed.configureTestingModule({ imports: [ResearchComponent], providers: [{ provide: TradingApiService, useValue: api }] }).compileComponents();
    fixture = TestBed.createComponent(ResearchComponent);
    component = fixture.componentInstance;
  });

  it('loads and normalizes the symbol and displays both research sources', () => {
    api.quote.and.returnValue(of(quote('RELIANCE')));
    api.recommendation.and.returnValue(of(recommendation));
    component.symbol = ' reliance ';
    component.load();
    fixture.detectChanges();
    expect(api.quote).toHaveBeenCalledWith('RELIANCE');
    expect(api.recommendation).toHaveBeenCalledWith('RELIANCE');
    expect(component.quote?.lastTradedPrice).toBe(2500);
    expect(component.recommendation?.action).toBe('Buy');
    expect(component.loading).toBeFalse();
  });

  it('shows a validation error for an empty symbol without calling the API', () => {
    component.symbol = '   ';
    component.load();
    expect(component.error).toBe('Enter a symbol before researching.');
    expect(api.quote).not.toHaveBeenCalled();
    expect(api.recommendation).not.toHaveBeenCalled();
  });

  it('keeps partial quote data visible when recommendation loading fails', () => {
    api.quote.and.returnValue(of(quote('TCS')));
    api.recommendation.and.returnValue(throwError(() => new Error('unavailable')));
    component.symbol = 'TCS';
    component.load();
    fixture.detectChanges();
    expect(component.quote?.symbol).toBe('TCS');
    expect(component.recommendation).toBeUndefined();
    expect(component.error).toContain('Recommendation could not be loaded');
    expect(component.loading).toBeFalse();
  });
});
