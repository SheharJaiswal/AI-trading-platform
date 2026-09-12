import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { OperationalDashboardComponent } from './operational-dashboard.component';

describe('OperationalDashboardComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OperationalDashboardComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function flushDashboard(fixture: ReturnType<typeof TestBed.createComponent>, aiProvider: string) {
    fixture.detectChanges();
    http.expectOne('/health').flush({ status: 'ok', mode: 'paper', marketProvider: 'demo', aiProvider, persistence: true });
    http.expectOne('/api/portfolio').flush({ cash: 1000, positions: [], unrealizedPnl: 0, realizedPnl: 0 });
    http.expectOne('/api/alerts').flush([]);
    http.expectOne('/api/evaluations/metrics').flush({ total: 0, evaluated: 0, wins: 0, losses: 0, directionalAccuracy: 0, cumulativeReturn: 0, maxDrawdown: 0, unitPnl: 0 });
    fixture.detectChanges();
  }

  it('surfaces durable persistence state from the health contract', () => {
    const fixture = TestBed.createComponent(OperationalDashboardComponent);
    flushDashboard(fixture, 'disabled');
    expect(fixture.nativeElement.textContent).toContain('DURABLE');
    expect(fixture.nativeElement.textContent).toContain('MySQL-backed paper state');
  });

  it('shows in-memory persistence state when durable storage is disabled', () => {
    const fixture = TestBed.createComponent(OperationalDashboardComponent);
    fixture.detectChanges();
    http.expectOne('/health').flush({ status: 'ok', mode: 'paper', marketProvider: 'demo', aiProvider: 'disabled', persistence: false });
    http.expectOne('/api/portfolio').flush({ cash: 1000, positions: [], unrealizedPnl: 0, realizedPnl: 0 });
    http.expectOne('/api/alerts').flush([]);
    http.expectOne('/api/evaluations/metrics').flush({ total: 0, evaluated: 0, wins: 0, losses: 0, directionalAccuracy: 0, cumulativeReturn: 0, maxDrawdown: 0, unitPnl: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('IN-MEMORY');
    expect(fixture.nativeElement.textContent).toContain('Default paper configuration');
  });

  it('surfaces configured local AI mode as advisory-only', () => {
    const fixture = TestBed.createComponent(OperationalDashboardComponent);
    flushDashboard(fixture, 'local');
    expect(fixture.nativeElement.textContent).toContain('LOCAL');
    expect(fixture.nativeElement.textContent).toContain('Advisory research only');
  });

  it('defaults an absent AI provider to disabled', () => {
    const fixture = TestBed.createComponent(OperationalDashboardComponent);
    flushDashboard(fixture, '');
    expect(fixture.nativeElement.textContent).toContain('DISABLED');
    expect(fixture.nativeElement.textContent).toContain('Advisory research only');
  });
});
