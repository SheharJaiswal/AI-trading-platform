import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EvaluationComponent } from './evaluation.component';

describe('EvaluationComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EvaluationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads evaluation metrics without implying live performance', () => {
    const fixture = TestBed.createComponent(EvaluationComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.expectOne('/api/evaluations/metrics').flush({
      total: 5, evaluated: 4, wins: 3, losses: 1,
      directionalAccuracy: 0.75, cumulativeReturn: 0.08,
      maxDrawdown: 0.02, unitPnl: 0.08
    });
    fixture.detectChanges();
    expect(component.error).toBe('');
    expect(component.metrics?.evaluated).toBe(4);
    expect(component.metrics?.directionalAccuracy).toBe(0.75);
  });

  it('surfaces unavailable evaluation data instead of assuming performance', () => {
    const fixture = TestBed.createComponent(EvaluationComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    httpMock.expectOne('/api/evaluations/metrics').flush({ message: 'Evaluation persistence is unavailable.' }, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();
    expect(component.metrics).toBeUndefined();
    expect(component.error).toContain('Evaluation persistence is unavailable');
  });
});
