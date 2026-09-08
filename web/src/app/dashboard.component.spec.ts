import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads portfolio, positions and alerts', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;

    fixture.detectChanges();

    httpMock.expectOne('/health').flush({
      status: 'ok',
      mode: 'paper',
      persistence: true,
      marketProvider: 'demo'
    });
    httpMock.expectOne('/api/portfolio').flush({
      cash: 100000,
      unrealizedPnl: 1250.5,
      realizedPnl: 420,
      positions: [{ id: 'p1', symbol: 'TCS', quantity: 10, averageEntryPrice: 3200, stopLoss: 3000 }]
    });
    httpMock.expectOne('/api/alerts').flush([{
      key: 'p1: PriceStopLoss:20260908T1200Z',
      severity: 'Warning',
      message: 'TCS is approaching stop loss.',
      createdAt: '2026-09-08T12:00:00Z',
      symbol: 'TCS'
    }]);

    fixture.detectChanges();

    expect(component.error).toBe('');
    expect(component.portfolio?.cash).toBe(100000);
    expect(component.portfolio?.positions?.[0].symbol).toBe('TCS');
    expect(component.portfolio?.unrealizedPnl).toBe(1250.5);
    expect(component.alerts.length).toBe(1);
    expect(component.alerts[0].severity).toBe('Warning');
    expect(component.loadedAt).toEqual(jasmine.any(Date));
  });

  it('surfaces an unavailable API instead of assuming data is fresh', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;

    fixture.detectChanges();
    httpMock.expectOne('/health').flush('offline', { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(component.error).toContain('could not be reached');
    expect(component.health).toBeUndefined();
    expect(component.portfolio).toBeUndefined();
    expect(component.alerts).toEqual([]);
  });

  it('surfaces alert load failures without hiding portfolio data', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const component = fixture.componentInstance;

    fixture.detectChanges();
    httpMock.expectOne('/health').flush({
      status: 'ok',
      mode: 'paper',
      persistence: true,
      marketProvider: 'demo'
    });
    httpMock.expectOne('/api/portfolio').flush({ cash: 100000, positions: [] });
    httpMock.expectOne('/api/alerts').flush('offline', { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(component.portfolio?.cash).toBe(100000);
    expect(component.alerts).toEqual([]);
    expect(component.error).toContain('risk alerts could not be loaded');
  });
});
