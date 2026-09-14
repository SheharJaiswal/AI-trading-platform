import {ComponentFixture,TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {AppComponent} from './app.component';

describe('AppComponent',()=>{
  let fixture:ComponentFixture<AppComponent>;

  beforeEach(async()=>{
    await TestBed.configureTestingModule({imports:[AppComponent],providers:[provideRouter([])]}).compileComponents();
    fixture=TestBed.createComponent(AppComponent);
    fixture.detectChanges();
  });

  it('exposes the paper session workspace in primary navigation',()=>{
    const link=fixture.nativeElement.querySelector('a[routerLink="/paper-session"]') as HTMLAnchorElement|null;
    expect(link).not.toBeNull();
    expect(link?.textContent).toContain('Paper Session');
  });

  it('keeps the paper-only boundary visible',()=>{
    expect(fixture.nativeElement.textContent).toContain('PAPER ONLY');
  });
});
