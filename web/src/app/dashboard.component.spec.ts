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

  it('loads health and portfolio and records a successful refresh', () => {
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

    fixture.detectChanges();

    expect(component.error).toBe('');
    expect(component.health?.status).toBe('ok');
    expect(component.portfolio?.cash).toBe(100000);
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
  });
});
