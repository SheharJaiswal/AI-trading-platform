import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { TradingApiService } from './core/api/trading-api.service';
import { HealthStatus, PortfolioSnapshot } from './core/api/trading-api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe],
  template: `<section class="workspace">
    <div class="hero">
      <div>
        <p class="eyebrow">TRADING WORKSPACE</p>
        <h1>Research before you trade.</h1>
        <p class="muted">Live application state with explicit freshness and safety boundaries.</p>
      </div>
      <div class="status" [class.ok]="health?.status === 'ok'" [class.error]="!!error">
        <span class="dot"></span>{{ error ? 'API unavailable' : health ? 'API online' : 'Checking API…' }}
      </div>
    </div>
    @if (error) { <div class="notice error-box">{{ error }} <button type="button" (click)="load()">Retry</button></div> }
    @if (!health && !error) { <div class="notice">Loading application health…</div> }
    <div class="grid">
      <article><h2>Paper portfolio</h2><strong>{{ portfolio?.cash ?? '—' }}</strong><p class="muted">Available virtual cash</p></article>
      <article><h2>Market provider</h2><strong>{{ health?.marketProvider ?? '—' }}</strong><p class="muted">Server-reported provider</p></article>
      <article><h2>Persistence</h2><strong>{{ health ? (health.persistence ? 'MySQL' : 'In-memory') : '—' }}</strong><p class="muted">Durability mode</p></article>
    </div>
    <div class="notice"><strong>Paper trading only.</strong> AI is advisory; every execution remains subject to the deterministic server-side risk gate.</div>
    @if (loadedAt) { <p class="freshness">Last successful refresh: {{ loadedAt | date:'medium' }}</p> }
  </section>`,
  styles: [`.hero{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}.status{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:12px}.status.ok{border-color:#8bbf9b}.status.error{border-color:#d89b9b}.dot{display:inline-block;width:7px;height:7px;border-radius:50%;background:#9aa3b2;margin-right:7px}.grid{margin-top:32px}.notice button{float:right;border:0;border-radius:8px;padding:6px 10px;cursor:pointer}.error-box{border-color:#d89b9b}.freshness{font-size:12px;color:#687386;margin-top:12px}@media(max-width:760px){.hero{display:block}.status{display:inline-block;margin-top:16px}}`]
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(TradingApiService);
  health?: HealthStatus;
  portfolio?: PortfolioSnapshot;
  error = '';
  loadedAt?: Date;
  ngOnInit(): void { this.load(); }
  load(): void {
    this.error = '';
    this.health = undefined;
    this.portfolio = undefined;
    this.api.health().subscribe({
      next: value => { this.health = value; this.loadedAt = new Date(); this.loadPortfolio(); },
      error: () => { this.error = 'The trading API could not be reached. Data is not assumed to be fresh.'; }
    });
  }
  private loadPortfolio(): void {
    this.api.portfolio().subscribe({
      next: value => { this.portfolio = value; this.loadedAt = new Date(); },
      error: () => { this.error = 'Health is available, but portfolio data could not be loaded.'; }
    });
  }
}
