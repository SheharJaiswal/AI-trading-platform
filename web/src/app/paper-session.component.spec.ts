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

  it('submits explicit short-cycle risk and freshness inputs to the paper-only endpoint',()=>{
    component.session={id:'session-1',status:'Running',configuration:{symbols:[{value:'INFY'}],interval:'1m',strategyVersion:'baseline-v1',startingCash:100000},createdAt:'2026-09-09T00:00:00Z',updatedAt:'2026-09-09T00:00:00Z'};
    component.shortQuantity=3;
    component.stopLossPercent=.03;
    component.targetPercent=.07;
    component.maxMarketDataAgeSeconds=120;
    component.runShortCycle();
    const request=http.expectOne('/api/paper-sessions/session-1/run-short-cycle');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({quantity:3,stopLossPercent:.03,targetPercent:.07,maxMarketDataAgeSeconds:120});
    request.flush({execution:{status:'executed',risk:{decision:'Approved'},fill:{orderId:'order-1',quantity:3,price:100}},position:{id:'position-1',quantity:3,entryPrice:100,stopLoss:103,targetPrice:93,state:'SHORT_OPEN'}});
    expect(component.shortCycle?.execution.status).toBe('executed');
    expect(component.shortCycle?.position?.id).toBe('position-1');
  });

  it('surfaces short-cycle rejection without implying a trade occurred',()=>{
    component.session={id:'session-1',status:'Running',configuration:{symbols:[{value:'INFY'}],interval:'1m',strategyVersion:'baseline-v1',startingCash:100000},createdAt:'2026-09-09T00:00:00Z',updatedAt:'2026-09-09T00:00:00Z'};
    component.runShortCycle();
    const request=http.expectOne('/api/paper-sessions/session-1/run-short-cycle');
    request.flush({message:'STALE_OR_INVALID_MARKET_DATA'},{status:409,statusText:'Conflict'});
    expect(component.message).toContain('STALE_OR_INVALID_MARKET_DATA');
    expect(component.shortCycle).toBeNull();
  });
});
