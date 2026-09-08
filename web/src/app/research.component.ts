import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TradingApiService } from './core/api/trading-api.service';
import { MarketQuote, Recommendation } from './core/api/trading-api.models';

@Component({
  selector: 'app-research',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <section class="workspace">
      <div class="hero">
        <div><p class="eyebrow">MARKET RESEARCH</p><h1>Research a symbol.</h1><p class="muted">Deterministic market quote and recommendation evidence in one view. AI advisory research stays separate.</p></div>
        <span class="paper">PAPER TRADING ONLY</span>
      </div>

      <form class="search" (ngSubmit)="load()">
        <label for="symbol">Symbol</label>
        <input id="symbol" name="symbol" [(ngModel)]="symbol" placeholder="e.g. RELIANCE" autocomplete="off" />
        <button type="submit" [disabled]="loading">{{ loading ? 'Loading…' : 'Research' }}</button>
      </form>

      @if (error) { <div class="notice error-box">{{ error }}</div> }
      @if (loading) { <div class="notice">Loading quote and recommendation…</div> }

      @if (quote) {
        <article class="panel">
          <div class="panel-heading"><div><p class="eyebrow">MARKET QUOTE</p><h2>{{ quote.symbol }}</h2></div><span class="muted">{{ quote.provider ?? 'Provider unavailable' }}</span></div>
          <div class="quote"><strong>{{ quote.price | number:'1.2-2' }}</strong><span>Latest available price</span></div>
          @if (quote.timestamp) { <p class="freshness">Quote timestamp: {{ quote.timestamp | date:'medium' }}</p> }
        </article>
      }

      @if (recommendation) {
        <article class="panel recommendation">
          <div class="panel-heading"><div><p class="eyebrow">DETERMINISTIC RECOMMENDATION</p><h2>{{ recommendation.decision }}</h2></div><span class="confidence">{{ recommendation.confidence !== undefined ? (recommendation.confidence | percent:'1.0-1') : 'Confidence unavailable' }}</span></div>
          @if (recommendation.reason) { <p class="reason">{{ recommendation.reason }}</p> } @else { <div class="empty">Recommendation reason is not available from the API.</div> }
        </article>
      }

      @if (!loading && !error && !quote && !recommendation) { <div class="empty panel">Enter a symbol to load current research data.</div> }
      <div class="safety"><strong>Advisory boundary.</strong> This workspace does not authorize trades. Any future paper-trade submission must pass the server-side deterministic risk gate.</div>
    </section>
  `,
  styles: [`.workspace{max-width:1100px;margin:0 auto}.hero{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.paper{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:11px;font-weight:700}.search{display:flex;gap:10px;align-items:end;margin:28px 0}.search label{font-size:12px;font-weight:600}.search input{flex:1;padding:12px;border:1px solid #d7dce6;border-radius:9px;font:inherit}.search button{padding:12px 18px;border:0;border-radius:9px;cursor:pointer}.search button:disabled{cursor:wait}.panel{border:1px solid #e1e5ec;border-radius:14px;background:#fff;padding:20px;margin-top:18px}.panel-heading{display:flex;justify-content:space-between;align-items:center}.panel-heading h2{margin:4px 0}.quote{display:flex;gap:12px;align-items:baseline;margin-top:22px}.quote strong{font-size:32px}.quote span,.muted,.freshness{color:#687386;font-size:12px}.confidence{font-size:12px;font-weight:600}.reason{line-height:1.6}.empty{padding:24px;color:#687386}.notice,.safety{margin-top:18px;padding:14px 16px;border:1px solid #e1e5ec;border-radius:10px}.error-box{border-color:#d89b9b}.safety{background:#f7f9fb}.eyebrow{font-size:11px;letter-spacing:.08em;color:#687386;margin:0}.freshness{margin-top:14px}@media(max-width:700px){.hero,.search{display:block}.paper{display:inline-block;margin-top:16px}.search label{display:block;margin-bottom:7px}.search button{margin-top:10px;width:100%}}`]
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
    if (!requested) { this.error = 'Enter a symbol before researching.'; return; }
    this.symbol = requested;
    this.quote = undefined;
    this.recommendation = undefined;
    this.error = '';
    this.loading = true;
    let completed = 0;
    const fail = (message: string) => { if (!this.error) this.error = message; completed++; if (completed === 2) this.loading = false; };
    this.api.quote(requested).subscribe({ next: value => { this.quote = value; completed++; if (completed === 2) this.loading = false; }, error: () => fail('Market quote could not be loaded. Data is not assumed to be fresh.') });
    this.api.recommendation(requested).subscribe({ next: value => { this.recommendation = value; completed++; if (completed === 2) this.loading = false; }, error: () => fail('Recommendation could not be loaded. Review the symbol or provider status.') });
  }
}
