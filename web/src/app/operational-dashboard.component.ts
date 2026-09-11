import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { TradingApiService } from './core/api/trading-api.service';
import { AlertSnapshot, EvaluationMetrics, HealthStatus, PortfolioSnapshot } from './core/api/trading-api.models';

@Component({
  selector: 'app-operational-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  template: `<section class="workspace">
    <div class="hero">
      <div><p class="eyebrow">OPERATIONS</p><h1>Operational dashboard.</h1><p class="muted">Read-only view of durable paper-trading health, risk, alerts and evaluation.</p></div>
      <div class="status" [class.ok]="health?.status === 'ok'" [class.error]="!!error"><span class="dot"></span>{{ error ? 'Data issue' : health ? 'API online' : 'Checking API…' }}</div>
    </div>
    @if (error) { <div class="notice error-box">{{ error }} <button type="button" (click)="load()">Retry</button></div> }
    @if (!health && !error) { <div class="notice">Loading operational data…</div> }

    <div class="kpis">
      <article><span>API</span><strong>{{ health?.status ?? '—' }}</strong><small>{{ health?.mode ?? 'No response' }}</small></article>
      <article><span>Open positions</span><strong>{{ portfolio?.positions?.length ?? '—' }}</strong><small>Durable paper holdings</small></article>
      <article><span>Unrealized P&amp;L</span><strong [class.positive]="(portfolio?.unrealizedPnl ?? 0) >= 0" [class.negative]="(portfolio?.unrealizedPnl ?? 0) < 0">{{ amount(portfolio?.unrealizedPnl) }}</strong><small>Current portfolio</small></article>
      <article><span>Risk alerts</span><strong>{{ alerts.length }}</strong><small>Persisted alert records</small></article>
    </div>

    <div class="grid">
      <section class="panel"><div class="panel-heading"><div><p class="eyebrow">PORTFOLIO</p><h2>Positions</h2></div></div>
        @if (!portfolio?.positions?.length) { <div class="empty">No open paper positions.</div> } @else { <div class="table-wrap"><table><thead><tr><th>Symbol</th><th>Qty</th><th>Entry</th><th>Stop</th></tr></thead><tbody>@for (p of portfolio?.positions; track p.id) {<tr><td><strong>{{ p.symbol }}</strong></td><td>{{ p.quantity }}</td><td>{{ p.averageEntryPrice | number:'1.2-2' }}</td><td>{{ p.stopLoss ?? '—' }}</td></tr>}</tbody></table></div> }
      </section>

      <section class="panel"><div class="panel-heading"><div><p class="eyebrow">EVALUATION</p><h2>Prediction quality</h2></div></div>
        @if (!metrics) { <div class="empty">Evaluation metrics unavailable.</div> } @else { <div class="metric-list"><div><span>Evaluated</span><strong>{{ metrics.evaluated }} / {{ metrics.total }}</strong></div><div><span>Directional accuracy</span><strong>{{ metrics.directionalAccuracy | number:'1.0-2' }}%</strong></div><div><span>Unit P&amp;L</span><strong>{{ metrics.unitPnl | number:'1.2-2' }}</strong></div><div><span>Max drawdown</span><strong>{{ metrics.maxDrawdown | number:'1.2-2' }}</strong></div></div> }
      </section>
    </div>

    <section class="panel"><div class="panel-heading"><div><p class="eyebrow">RISK</p><h2>Recent alerts</h2></div><span class="muted">{{ alerts.length }} records</span></div>
      @if (!alerts.length) { <div class="empty">No persisted risk alerts. Empty state is intentional.</div> } @else { <div class="alerts">@for (a of alerts; track a.key) {<article class="alert"><div><strong>{{ a.symbol ?? 'Portfolio' }}</strong><span class="severity">{{ a.severity }}</span></div><p>{{ a.message }}</p><small>{{ a.createdAt | date:'medium' }}</small></article>}</div> }
    </section>

    <div class="safety"><strong>Paper trading only.</strong> This dashboard is read-only. AI remains advisory and deterministic server-side risk remains authoritative. No live execution or automatic liquidation is exposed here.</div>
    @if (loadedAt) { <p class="freshness">Last successful refresh: {{ loadedAt | date:'medium' }}</p> }
  </section>`,
  styles: [`.hero{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}.status{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:12px}.status.ok{border-color:#8bbf9b}.status.error{border-color:#d89b9b}.dot{display:inline-block;width:7px;height:7px;border-radius:50%;background:#9aa3b2;margin-right:7px}.kpis{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin:28px 0}.kpis article,.panel{border:1px solid #e1e5ec;border-radius:14px;background:#fff}.kpis article{padding:18px}.kpis span,.kpis small{display:block;color:#687386;font-size:12px}.kpis strong{display:block;font-size:25px;margin:10px 0}.positive{color:#1b7f45}.negative{color:#b33a3a}.grid{display:grid;grid-template-columns:1fr 1fr;gap:18px}.panel{padding:20px;margin-top:18px}.panel-heading{display:flex;justify-content:space-between;align-items:center}.panel-heading h2{margin:4px 0 0}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;margin-top:16px}th,td{text-align:left;padding:12px;border-bottom:1px solid #eef0f4}th{font-size:12px;color:#687386}.empty{padding:24px 8px;color:#687386}.metric-list{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-top:16px}.metric-list div{padding:14px;border:1px solid #eef0f4;border-radius:10px}.metric-list span{display:block;color:#687386;font-size:12px}.metric-list strong{display:block;margin-top:8px}.alerts{display:grid;gap:10px;margin-top:16px}.alert{padding:14px;border:1px solid #eef0f4;border-radius:10px}.alert div{display:flex;gap:10px;align-items:center}.alert p{margin:8px 0}.alert small,.freshness{color:#687386}.severity{font-size:11px;padding:3px 7px;border-radius:999px;background:#f1f3f6}.notice,.safety{margin-top:18px;padding:14px 16px;border:1px solid #e1e5ec;border-radius:10px}.notice button{float:right;border:0;border-radius:8px;padding:6px 10px;cursor:pointer}.error-box{border-color:#d89b9b}.safety{background:#f7f9fb}.freshness{font-size:12px;margin-top:12px}@media(max-width:900px){.kpis{grid-template-columns:repeat(2,1fr)}.grid{grid-template-columns:1fr}}@media(max-width:760px){.hero{display:block}.status{display:inline-block;margin-top:16px}.kpis{grid-template-columns:1fr}.panel{padding:16px}.metric-list{grid-template-columns:1fr}}`]
})
export class OperationalDashboardComponent implements OnInit {
  private readonly api = inject(TradingApiService);
  health?: HealthStatus;
  portfolio?: PortfolioSnapshot;
  alerts: AlertSnapshot[] = [];
  metrics?: EvaluationMetrics;
  error = '';
  loadedAt?: Date;

  ngOnInit(): void { this.load(); }

  amount(value: number | undefined): string { return value === undefined ? '—' : new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value); }

  load(): void {
    this.error = '';
    this.health = undefined;
    this.portfolio = undefined;
    this.alerts = [];
    this.metrics = undefined;
    this.api.health().subscribe({ next: value => { this.health = value; this.loadedAt = new Date(); this.loadPortfolio(); this.loadAlerts(); this.loadMetrics(); }, error: () => { this.error = 'The trading API could not be reached. Data is not assumed to be fresh.'; } });
  }
  private loadPortfolio(): void { this.api.portfolio().subscribe({ next: value => this.portfolio = value, error: () => this.error = 'Health is available, but portfolio data could not be loaded.' }); }
  private loadAlerts(): void { this.api.alerts().subscribe({ next: value => this.alerts = value, error: () => this.error = 'Risk alerts could not be loaded.' }); }
  private loadMetrics(): void { this.api.evaluationMetrics().subscribe({ next: value => this.metrics = value, error: () => this.metrics = undefined }); }
}
