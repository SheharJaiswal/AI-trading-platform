import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { PaperTradeComponent } from './paper-trade.component';
import { TradingApiService } from './core/api/trading-api.service';

describe('PaperTradeComponent', () => {
  let api: jasmine.SpyObj<TradingApiService>;
  let component: PaperTradeComponent;

  beforeEach(async () => {
    api = jasmine.createSpyObj<TradingApiService>('TradingApiService', ['paperTrade']);
    await TestBed.configureTestingModule({ imports: [PaperTradeComponent], providers: [{ provide: TradingApiService, useValue: api }] }).compileComponents();
    component = TestBed.createComponent(PaperTradeComponent).componentInstance;
  });

  it('does not submit without explicit confirmation', () => {
    component.confirmed = false;
    component.submit(new Event('submit'));
    expect(api.paperTrade).not.toHaveBeenCalled();
    expect(component.errorCode).toBe('CONFIRMATION_REQUIRED');
  });

  it('submits a confirmed paper trade and displays the durable fill', () => {
    api.paperTrade.and.returnValue(of({ risk: { decision: 'Approved', reason: null }, fill: { orderId: 'o1', symbol: { value: 'RELIANCE' }, side: 'Buy', quantity: 2, price: 2500, timestamp: '2026-09-09T00:00:00Z', source: 'paper' } }));
    component.symbol = 'RELIANCE';
    component.quantity = 2;
    component.confirmed = true;
    component.submit(new Event('submit'));
    expect(api.paperTrade).toHaveBeenCalledWith('RELIANCE', 2, jasmine.any(String));
    expect(component.result?.fill?.quantity).toBe(2);
    expect(component.risk?.decision).toBe('Approved');
  });

  it('renders a server risk block and does not claim a fill', () => {
    api.paperTrade.and.returnValue(throwError(() => new HttpErrorResponse({ status: 422, error: { errorCode: 'RISKBLOCKED', message: 'Order exceeds available virtual cash.', risk: { decision: 'RiskBlocked', reason: 'Order exceeds available virtual cash.' } } })));
    component.confirmed = true;
    component.quantity = 10;
    component.submit(new Event('submit'));
    expect(component.errorCode).toBe('RISKBLOCKED');
    expect(component.risk?.decision).toBe('RiskBlocked');
    expect(component.result).toBeUndefined();
  });
});
