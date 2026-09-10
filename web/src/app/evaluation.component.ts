import { DecimalPipe, PercentPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { EvaluationMetrics } from './core/api/trading-api.models';
import { TradingApiService } from './core/api/trading-api.service';

@Component({
  selector: 'app-evaluation',
  standalone: true,
  imports: [DecimalPipe, PercentPipe],
  template: `<section class="workspace">
    <div class="hero">
      <div><p class="eyebrow">MODEL PERFORMANCE</p><h1>Prediction evaluation.</h1><p class="muted">Outcome-based performance of recorded predictions, separated from paper execution.</p></div>
      <span class="paper-badge">PAPER / SIMULATION</span>
    </div>
    @if (error) { <div class="notice error-box">{{ error }} <button type="button" (click)="load()">Retry</button></div> }
    @if (!metrics && !error) { <div class="notice">Loading evaluation metrics…</div> }
    @if (metrics) {
      <div class="kpis">
        <article><span>Directional accuracy</span><strong>{{ metrics.directionalAccuracy | percent:'1.1-1' }}</strong><small>{{ metrics.evaluated }} evaluated predictions</small></article>
        <article><span>Wins / losses</span><strong>{{ metrics.wins }} / {{ metrics.losses }}</strong><small>Outcome return sign</small></article>
        <article><span>Cumulative return</span><strong [class.positive]="metrics.cumulativeReturn >= 0" [class.negative]="metrics.cumulativeReturn < 0">{{ metrics.cumulativeReturn | number:'1.2-4' }}</strong><small>Unit-return basis</small></article>
        <article><span>Max drawdown</span><strong [class.negative]="metrics.maxDrawdown > 0">{{ metrics.maxDrawdown | number:'1.2-4' }}</strong><small>Peak-to-trough unit return</small></article>
      </div>
      <div class="panel"><p class="eyebrow">INTERPRETATION</p><h2>What these metrics mean</h2><p>Accuracy measures whether the realized return direction matched the recorded prediction. Cumulative return and drawdown use the evaluated outcome sequence and do not imply live profitability.</p><p class="muted">{{ metrics.total }} prediction records exist; {{ metrics.evaluated }} have outcomes. Unevaluated predictions are excluded from performance calculations.</p></div>
      <div class="safety"><strong>Advisory only.</strong> Evaluation results do not change strategies, risk rules, or execute trades. All trading remains paper-only and server-side risk gated.</div>
    }
  </section>`,
  styles: [`.hero{display:flex;justify-content:space-between;align-items:flex-start;gap:20px}.kpis{display:grid;grid-template-columns:repeat(4,1fr);gap:16px;margin:28px 0}.kpis article,.panel{border:1px solid #e1e5ec;border-radius:14px;background:#fff}.kpis article{padding:18px}.kpis span,.kpis small{display:block;color:#687386;font-size:12px}.kpis strong{display:block;font-size:25px;margin:10px 0}.positive{color:#1b7f45}.negative{color:#b33a3a}.panel{padding:20px;margin-top:18px}.notice,.safety{margin-top:18px;padding:14px 16px;border:1px solid #e1e5ec;border-radius:10px}.notice button{float:right;border:0;border-radius:8px;padding:6px 10px;cursor:pointer}.error-box{border-color:#d89b9b}.safety{background:#f7f9fb}@media(max-width:900px){.kpis{grid-template-columns:repeat(2,1fr)}}@media(max-width:760px){.hero{display:block}.kpis{grid-template-columns:1fr}.panel{padding:16px}}`]
})
export class EvaluationComponent implements OnInit {
  private readonly api = inject(TradingApiService);
  metrics?: EvaluationMetrics;
  error = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.error = '';
    this.metrics = undefined;
    this.api.evaluationMetrics().subscribe({
      next: value => { this.metrics = value; },
      error: response => {
        this.error = response?.error?.message ?? 'Evaluation metrics are unavailable. No performance is assumed when the evaluation service cannot be reached.';
      }
    });
  }
}
