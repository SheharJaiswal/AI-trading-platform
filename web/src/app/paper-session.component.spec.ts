import {ComponentFixture,TestBed} from '@angular/core/testing';
import {HttpClientTestingModule,HttpTestingController} from '@angular/common/http/testing';
import {PaperSessionComponent} from './paper-session.component';

describe('PaperSessionComponent',()=>{
  let fixture:ComponentFixture<PaperSessionComponent>;
  let component:PaperSessionComponent;
  let http:HttpTestingController;

  beforeEach(async()=>{
    await TestBed.configureTestingModule({imports:[PaperSessionComponent,HttpClientTestingModule]}).compileComponents();
    fixture=TestBed.createComponent(PaperSessionComponent);
    component=fixture.componentInstance;
    http=TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(()=>http.verify());

  it('renders the explicit paper-only safety boundary',()=>{
    expect(fixture.nativeElement.textContent).toContain('PAPER ONLY');
    expect(fixture.nativeElement.textContent).toContain('no live broker execution is exposed');
  });

  it('creates a session and exposes lifecycle controls for the returned state',()=>{
    component.create(new Event('submit'));
    const request=http.expectOne('/api/paper-sessions');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.symbols[0].value).toBe('INFY');
    request.flush({id:'session-1',status:'Draft',configuration:{symbols:[{value:'INFY'}],interval:'1m',strategyVersion:'baseline-v1',startingCash:100000},createdAt:'2026-09-09T00:00:00Z',updatedAt:'2026-09-09T00:00:00Z'});
    fixture.detectChanges();
    expect(component.session?.status).toBe('Draft');
    expect(fixture.nativeElement.textContent).toContain('Start');
  });

  it('surfaces event API failures without implying execution succeeded',()=>{
    component.session={id:'session-1',status:'Running',configuration:{symbols:[{value:'INFY'}],interval:'1m',strategyVersion:'baseline-v1',startingCash:100000},createdAt:'2026-09-09T00:00:00Z',updatedAt:'2026-09-09T00:00:00Z'};
    component.processEvent();
    const request=http.expectOne('/api/paper-sessions/session-1/events');
    request.flush({message:'Risk limit rejected event.'},{status:409,statusText:'Conflict'});
    expect(component.message).toContain('Risk limit rejected event');
  });
});
