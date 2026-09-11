import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { OperationalDashboardComponent } from './operational-dashboard.component';
import { TradingApiService } from './core/api/trading-api.service';

describe('OperationalDashboardComponent', () => {
  let fixture: ComponentFixture<OperationalDashboardComponent>;
  const api = {
    health: () => of({ status: 'ok', mode: 'paper', marketProvider: 'demo', persistence: true }),
    portfolio: () => of({ cash: 100000, positions: [], unrealizedPnl: 0, realizedPnl: 120 }),
    alerts: () => of([]),
    evaluationMetrics: () => of({ total: 10, evaluated: 8, wins: 5, losses: 3, directionalAccuracy: 0.625, cumulativeReturn: 0.02, maxDrawdown: 0.01, unitPnl: 200 })
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [OperationalDashboardComponent], providers: [{ provide: TradingApiService, useValue: api }] }).compileComponents();
    fixture = TestBed.createComponent(OperationalDashboardComponent);
    fixture.detectChanges();
  });

  it('shows healthy paper operations and empty alert state', () => {
    expect(fixture.nativeElement.textContent).toContain('API online');
    expect(fixture.nativeElement.textContent).toContain('paper');
    expect(fixture.nativeElement.textContent).toContain('No persisted risk alerts');
    expect(fixture.nativeElement.textContent).toContain('62.5%');
  });

  it('shows unavailable state when health cannot be reached', () => {
    const failingApi = { ...api, health: () => throwError(() => new Error('offline')) };
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ imports: [OperationalDashboardComponent], providers: [{ provide: TradingApiService, useValue: failingApi }] });
    const failingFixture = TestBed.createComponent(OperationalDashboardComponent);
    failingFixture.detectChanges();
    expect(failingFixture.nativeElement.textContent).toContain('The trading API could not be reached');
  });
});
