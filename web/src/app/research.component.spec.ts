import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ResearchComponent } from './research.component';
import { TradingApiService } from './core/api/trading-api.service';

describe('ResearchComponent', () => {
  let fixture: ComponentFixture<ResearchComponent>;
  let component: ResearchComponent;
  let api: jasmine.SpyObj<TradingApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<TradingApiService>('TradingApiService', ['quote', 'recommendation']);
    await TestBed.configureTestingModule({ imports: [ResearchComponent], providers: [{ provide: TradingApiService, useValue: api }] }).compileComponents();
    fixture = TestBed.createComponent(ResearchComponent);
    component = fixture.componentInstance;
  });

  it('loads and normalizes the symbol and displays both research sources', () => {
    api.quote.and.returnValue(of({ symbol: 'RELIANCE', price: 2500, timestamp: '2026-09-08T10:00:00Z', provider: 'demo' }));
    api.recommendation.and.returnValue(of({ decision: 'BUY', reason: 'Trend is positive', confidence: 0.8 }));

    component.symbol = ' reliance ';
    component.load();
    fixture.detectChanges();

    expect(api.quote).toHaveBeenCalledWith('RELIANCE');
    expect(api.recommendation).toHaveBeenCalledWith('RELIANCE');
    expect(component.quote?.price).toBe(2500);
    expect(component.recommendation?.decision).toBe('BUY');
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
    api.quote.and.returnValue(of({ symbol: 'TCS', price: 4000, provider: 'demo' }));
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
