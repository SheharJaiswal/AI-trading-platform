import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { TradingApiService } from './core/api/trading-api.service';
import { AlertSnapshot, HealthStatus, PortfolioSnapshot } from './core/api/trading-api.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  template: `<section class="workspace">
    <div class="hero">
      <div>
        <p class="eyebrow">TRADING WORKSPACE</p>
        <h1>Portfolio at a glance.</h1>
        <p class="muted">Durable paper-trading state with explicit freshness, P&amp;L and risk alerts.</p>
      </div>
      <div class="status" [class.ok]="health?.status === 'ok'" [class.error]="!!error">
        <span class="dot"></span>{{ error ? 'Data issue' : health ? 'API online' : 'Checking API…' }}
      </div>
    </div>

    @if (error) { <div class="notice error-box">{{ error }} <button type="button" (click)="load()">Retry</button></div> }
    @if (!health && !error) { <div class="notice">Loading portfolio workspace…</div> }

    <div class="kpis">
      <article><span>Available cash</span><strong>{{ formatAmount(portfolio?.cash) }}</strong><small>Virtual funds</small></article>
      <article><span>Unrealized P&amp;L</span><strong [class.positive]="(portfolio?.unrealizedPnl ?? 0) >= 0" [class.negative]="(portfolio?.unrealizedPnl ?? 0) < 0">{{ formatAmount(portfolio?.unrealizedPnl) }}</strong><small>Open positions</small></article>
      <article><span>Realized P&amp;L</span><strong>{{ formatAmount(portfolio?.realizedPnl) }}</strong><small>Closed paper trades</small></article>
      <article><span>Open positions</span><strong>{{ portfolio?.positions?.length ?? '—' }}</strong><small>Persisted holdings</small></article>
    </div>

    <div class="panel">
      <div class="panel-heading"><div><p class="eyebrow">POSITIONS</p><h2>Current holdings</h2></div><span class="muted">{{ portfolio?.positions?.length ?? 0 }} open</span></div>
      @if (!portfolio?.positions?.length) {
        <div class="empty">No open positions yet. Paper trades will appear here after a durable execution.</div>
      } @else {
        <div class="table-wrap"><table>
          <thead><tr><th>Symbol</th><th>Quantity</th><th>Avg entry</th><th>Stop loss</th></tr></thead>
          <tbody>@for (position of portfolio?.positions; track position.id) {
            <tr><td><strong>{{ position.symbol }}</strong></td><td>{{ position.quantity }}</td><td>{{ position.averageEntryPrice | number:'1.2-2' }}</td><td>{{ position.stopLoss ?? '—' }}</td></tr>
          }</tbody>
        </table></div>
      }
    </div>

    <div class="panel">
      <div class="panel-heading"><div><p class="eyebrow">RISK ALERTS</p><h2>Alert center</h2></div><span class="muted">{{ alerts.length }} active records</span></div>
      @if (!alerts.length) {
        <div class="empty">No persisted risk alerts. This is a healthy empty state.</div>
      } @else {
        <div class="alerts">@for (alert of alerts; track alert.key) {
          <article class="alert"><div><strong>{{ alert.symbol ?? 'Portfolio' }}</strong><span class="severity">{{ alert.severity }}</span></div><p>{{ alert.message }}</p><small>{{ alert.createdAt | date:'medium' }}</small></article>
        }</div>
      }
    </div>

    <div class="safety"><strong>Paper trading only.</strong> AI is advisory; every execution remains subject to the deterministic server-side risk gate. No broker credentials are used by the browser.</div>
    @if (loadedAt) { <p class="freshness">Last successful refresh: {{ loadedAt | date:'medium' }}</p> }
  </section>`,
  styles: [`.hero{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}.status{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:12px}.status.ok{border-color:#8bbf9b}.status.error{border-color:#d89b9b}.dot{display:inline-block;width:7px;height:7px;border-radius:50%;background:#9aa3b2;margin-right:7px}.kpis{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin:28px 0}.kpis article,.panel{border:1px solid #e1e5ec;border-radius:14px;background:#fff}.kpis article{padding:18px}.kpis span,.kpis small{display:block;color:#687386;font-size:12px}.kpis strong{display:block;font-size:25px;margin:10px 0}.positive{color:#1b7f45}.negative{color:#b33a3a}.panel{padding:20px;margin-top:18px}.panel-heading{display:flex;justify-content:space-between;align-items:center}.panel-heading h2{margin:4px 0 0}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;margin-top:16px}th,td{text-align:left;padding:12px;border-bottom:1px solid #eef0f4}th{font-size:12px;color:#687386}.empty{padding:24px 8px;color:#687386}.alerts{display:grid;gap:10px;margin-top:16px}.alert{padding:14px;border:1px solid #eef0f4;border-radius:10px}.alert div{display:flex;gap:10px;align-items:center}.alert p{margin:8px 0}.alert small{color:#687386}.severity{font-size:11px;padding:3px 7px;border-radius:999px;background:#f1f3f6}.notice,.safety{margin-top:18px;padding:14px 16px;border:1px solid #e1e5ec;border-radius:10px}.notice button{float:right;border:0;border-radius:8px;padding:6px 10px;cursor:pointer}.error-box{border-color:#d89b9b}.safety{background:#f7f9fb}.freshness{font-size:12px;color:#687386;margin-top:12px}@media(max-width:900px){.kpis{grid-template-columns:repeat(2,1fr)}}@media(max-width:760px){.hero{display:block}.status{display:inline-block;margin-top:16px}.kpis{grid-template-columns:1fr}.panel{padding:16px}}`]
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(TradingApiService);
  health?: HealthStatus;
  portfolio?: PortfolioSnapshot;
  alerts: AlertSnapshot[] = [];
  error = '';
  loadedAt?: Date;

  ngOnInit(): void { this.load(); }

  formatAmount(value: number | undefined): string {
    return value === undefined ? '—' : new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
  }

  load(): void {
    this.error = '';
    this.health = undefined;
    this.portfolio = undefined;
    this.alerts = [];
    this.api.health().subscribe({
      next: value => { this.health = value; this.loadPortfolio(); this.loadAlerts(); },
      error: () => { this.error = 'The trading API could not be reached. Data is not assumed to be fresh.'; }
    });
  }

  private loadPortfolio(): void {
    this.api.portfolio().subscribe({
      next: value => { this.portfolio = value; this.loadedAt = new Date(); },
      error: () => { this.error = 'Health is available, but portfolio data could not be loaded.'; }
    });
  }

  private loadAlerts(): void {
    this.api.alerts().subscribe({
      next: value => { this.alerts = value; },
      error: () => { this.error = 'Portfolio is available, but risk alerts could not be loaded.'; }
    });
  }
}
