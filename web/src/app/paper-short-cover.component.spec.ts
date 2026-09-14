import {ComponentFixture,TestBed} from '@angular/core/testing';
import {HttpClientTestingModule,HttpTestingController} from '@angular/common/http/testing';
import {provideRouter} from '@angular/router';
import {PaperShortCoverComponent} from './paper-short-cover.component';

describe('PaperShortCoverComponent',()=>{
  let fixture:ComponentFixture<PaperShortCoverComponent>;
  let component:PaperShortCoverComponent;
  let http:HttpTestingController;

  beforeEach(async()=>{
    await TestBed.configureTestingModule({imports:[PaperShortCoverComponent,HttpClientTestingModule],providers:[provideRouter([])]}).compileComponents();
    fixture=TestBed.createComponent(PaperShortCoverComponent);
    component=fixture.componentInstance;
    http=TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(()=>http.verify());

  it('renders the paper-only cover boundary',()=>{
    expect(fixture.nativeElement.textContent).toContain('PAPER ONLY');
    expect(fixture.nativeElement.textContent).toContain('No automatic liquidation');
  });

  it('submits explicit cover inputs with an idempotency key',()=>{
    component.positionId='position-1';
    component.coverPrice=98;
    component.coverQuantity=2;
    component.expectedVersion=3;
    component.idempotencyKey='cover-1';
    component.cover(new Event('submit'));
    const request=http.expectOne('/api/paper-shorts/position-1/cover');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Idempotency-Key')).toBe('cover-1');
    expect(request.request.body).toEqual({coverPrice:98,coverQuantity:2,expectedVersion:3});
    request.flush({executionMode:'PAPER_ONLY',position:{id:'position-1',remainingQuantity:0,lastCoverPrice:98,realizedPnl:4, state:'SHORT_CLOSED',version:4}});
    expect(component.result?.position.state).toBe('SHORT_CLOSED');
    expect(component.message).toContain('Paper short cover');
    expect(fixture.nativeElement.textContent).toContain('Inspect recovery diagnostics');
  });

  it('surfaces server rejection without implying a cover occurred',()=>{
    component.positionId='position-1';
    component.idempotencyKey='cover-2';
    component.cover(new Event('submit'));
    const request=http.expectOne('/api/paper-shorts/position-1/cover');
    request.flush({message:'SHORT_POSITION_CONCURRENCY_CONFLICT'},{status:409,statusText:'Conflict'});
    expect(component.result).toBeNull();
    expect(component.message).toContain('SHORT_POSITION_CONCURRENCY_CONFLICT');
  });
});
