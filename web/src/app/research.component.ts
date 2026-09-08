import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TradingApiService } from './core/api/trading-api.service';
import { MarketQuote, Recommendation } from './core/api/trading-api.models';

@Component({
  selector: 'app-research',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="workspace">
      <div class="hero">
        <div>
          <p class="eyebrow">MARKET RESEARCH</p>
          <h1>Research a symbol.</h1>
          <p class="muted">Deterministic quote, technical evidence and recommendation in one view. AI advisory research stays separate.</p>
        </div>
        <span class="paper">PAPER TRADING ONLY</span>
      </div>

      <form class="search" (ngSubmit)="load()">
        <label for="symbol">Symbol</label>
        <input id="symbol" name="symbol" [(ngModel)]="symbol" placeholder="e.g. RELIANCE" autocomplete="off" />
        <button type="submit" [disabled]="loading">{{ loading ? 'Loading…' : 'Research' }}</button>
      </form>

      @if (error) {
        <div class="notice error-box">{{ error }}</div>
      }
      @if (loading) {
        <div class="notice">Loading quote and recommendation…</div>
      }

      @if (quote; as currentQuote) {
        <article class="panel">
          <div class="panel-heading">
            <div><p class="eyebrow">MARKET QUOTE</p><h2>{{ currentQuote.symbol }}</h2></div>
            <span class="muted">{{ currentQuote.source }}</span>
          </div>
          <div class="quote"><strong>{{ currentQuote.lastTradedPrice | number:'1.2-2' }}</strong><span>Latest traded price</span></div>
          <div class="ohlc">
            <span>Open <b>{{ currentQuote.open | number:'1.2-2' }}</b></span>
            <span>High <b>{{ currentQuote.high | number:'1.2-2' }}</b></span>
            <span>Low <b>{{ currentQuote.low | number:'1.2-2' }}</b></span>
            <span>Volume <b>{{ currentQuote.volume | number:'1.0-0' }}</b></span>
          </div>
          <p class="freshness">Observed: {{ currentQuote.timestamp | date:'medium' }}</p>
        </article>
      }

      @if (recommendation; as currentRecommendation) {
        <article class="panel recommendation">
          <div class="panel-heading">
            <div><p class="eyebrow">DETERMINISTIC RECOMMENDATION</p><h2>{{ currentRecommendation.action }}</h2></div>
            <span class="confidence">{{ currentRecommendation.confidence | percent:'1.0-1' }} confidence</span>
          </div>
          <div class="facts">
            <span>Horizon <b>{{ currentRecommendation.horizonDays }} day{{ currentRecommendation.horizonDays === 1 ? '' : 's' }}</b></span>
            <span>Strategy <b>{{ currentRecommendation.strategyVersion }}</b></span>
            <span>Reference <b>{{ currentRecommendation.referencePrice | number:'1.2-2' }}</b></span>
          </div>
          @if (currentRecommendation.expectedReturn !== undefined && currentRecommendation.expectedReturn !== null) {
            <p>Expected return: <strong>{{ currentRecommendation.expectedReturn | percent:'1.1-1' }}</strong></p>
          }
          <h3>Supporting signals</h3>
          @if (currentRecommendation.supportingSignals.length > 0) {
            <ul>@for (signal of currentRecommendation.supportingSignals; track signal) { <li>{{ signal }}</li> }</ul>
          } @else {
            <div class="empty">No supporting signals were produced.</div>
          }
          <h3>Risk factors</h3>
          @if (currentRecommendation.riskFactors.length > 0) {
            <ul>@for (risk of currentRecommendation.riskFactors; track risk) { <li>{{ risk }}</li> }</ul>
          } @else {
            <div class="empty">No recommendation risk factors were returned.</div>
          }
        </article>
      }

      @if (!loading && !error && !quote && !recommendation) {
        <div class="empty panel">Enter a symbol to load current research data.</div>
      }
      <div class="safety"><strong>Advisory boundary.</strong> This workspace does not authorize trades. Any paper-trade submission must pass the server-side deterministic risk gate.</div>
    </section>
  `,
  styles: [`.workspace{max-width:1100px;margin:0 auto}.hero{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.paper{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:11px;font-weight:700}.search{display:flex;gap:10px;align-items:end;margin:28px 0}.search label{font-size:12px;font-weight:600}.search input{flex:1;padding:12px;border:1px solid #d7dce6;border-radius:9px;font:inherit}.search button{padding:12px 18px;border:0;border-radius:9px;cursor:pointer}.search button:disabled{cursor:wait}.panel{border:1px solid #e1e5ec;border-radius:14px;background:#fff;padding:20px;margin-top:18px}.panel-heading{display:flex;justify-content:space-between;align-items:center}.panel-heading h2{margin:4px 0}.quote{display:flex;gap:12px;align-items:baseline;margin-top:22px}.quote strong{font-size:32px}.quote span,.muted,.freshness{color:#687386;font-size:12px}.ohlc,.facts{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-top:18px}.ohlc span,.facts span{padding:10px;background:#f7f9fb;border-radius:8px;font-size:12px;color:#687386}.ohlc b,.facts b{display:block;color:#202733;margin-top:4px}.confidence{font-size:12px;font-weight:600}.recommendation h3{font-size:13px;margin-top:20px}.recommendation li{margin:6px 0}.empty{padding:18px;color:#687386}.notice,.safety{margin-top:18px;padding:14px 16px;border:1px solid #e1e5ec;border-radius:10px}.error-box{border-color:#d89b9b}.safety{background:#f7f9fb}.eyebrow{font-size:11px;letter-spacing:.08em;color:#687386;margin:0}.freshness{margin-top:14px}@media(max-width:700px){.hero,.search{display:block}.paper{display:inline-block;margin-top:16px}.search label{display:block;margin-bottom:7px}.search button{margin-top:10px;width:100%}.ohlc,.facts{grid-template-columns:repeat(2,1fr)}}`]
})
export class ResearchComponent {
  private readonly api = inject(TradingApiService);
  symbol = 'RELIANCE';
  quote?: MarketQuote;
  recommendation?: Recommendation;
  loading = false;
  error = '';

  load(): void {
    const requested = this.symbol.trim().toUpperCase();
    if (!requested) {
      this.error = 'Enter a symbol before researching.';
      return;
    }

    this.symbol = requested;
    this.quote = undefined;
    this.recommendation = undefined;
    this.error = '';
    this.loading = true;
    let completed = 0;
    const finish = () => {
      completed += 1;
      if (completed === 2) this.loading = false;
    };

    this.api.quote(requested).subscribe({
      next: value => { this.quote = value; finish(); },
      error: () => { if (!this.error) this.error = 'Market quote could not be loaded. Data is not assumed to be fresh.'; finish(); }
    });

    this.api.recommendation(requested).subscribe({
      next: value => { this.recommendation = value; finish(); },
      error: () => { if (!this.error) this.error = 'Recommendation could not be loaded. Review the symbol or provider status.'; finish(); }
    });
  }
}
