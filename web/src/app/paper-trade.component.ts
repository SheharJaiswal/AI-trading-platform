import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TradingApiService } from './core/api/trading-api.service';
import { PaperTradeResult, RiskResult } from './core/api/trading-api.models';

@Component({
  selector: 'app-paper-trade',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="workspace">
      <div class="hero">
        <div><p class="eyebrow">PAPER EXECUTION</p><h1>Submit a paper trade.</h1><p class="muted">Explicit confirmation is required. The server remains the final deterministic risk authority.</p></div>
        <span class="paper">PAPER ONLY</span>
      </div>

      <form class="panel form" (submit)="submit($event)">
        <label for="symbol">Symbol</label>
        <input id="symbol" name="symbol" [value]="symbol" (input)="onSymbolInput($event)" autocomplete="off" />
        <label for="quantity">Quantity</label>
        <input id="quantity" name="quantity" type="number" min="1" step="1" [value]="quantity" (input)="onQuantityInput($event)" />
        <label class="confirm"><input type="checkbox" [checked]="confirmed" (change)="onConfirmation($event)" /> I confirm this is a paper trade and want the server to evaluate it.</label>
        <button type="submit" [disabled]="submitting || !confirmed || !symbol.trim() || quantity <= 0">{{ submitting ? 'Submitting…' : 'Submit paper trade' }}</button>
      </form>

      @if (error) { <div class="notice error"><strong>{{ errorCode || 'Execution failed' }}</strong><span>{{ error }}</span></div> }
      @if (risk) {
        <article class="panel risk" [class.blocked]="risk.decision !== 'Approved'">
          <p class="eyebrow">SERVER RISK DECISION</p>
          <h2>{{ risk.decision }}</h2>
          <p>{{ risk.reason || 'Server approved this paper execution.' }}</p>
        </article>
      }
      @if (result?.fill; as fill) {
        <article class="panel result">
          <p class="eyebrow">DURABLE EXECUTION RESULT</p>
          <h2>Filled {{ fill.quantity }} {{ fill.symbol.value }}</h2>
          <div class="facts"><span>Price <b>{{ fill.price | number:'1.2-2' }}</b></span><span>Side <b>{{ fill.side }}</b></span><span>Source <b>{{ fill.source }}</b></span><span>Time <b>{{ fill.timestamp | date:'medium' }}</b></span></div>
          <p class="audit">The execution response is tied to the submitted idempotency key. Retrying the same request is expected to return the existing fill rather than create another one.</p>
        </article>
      }
      <div class="safety"><strong>Safety boundary.</strong> AI output cannot authorize this action. Every submission is evaluated by the server-side deterministic risk engine, and this workflow never sends a live broker order.</div>
    </section>
  `,
  styles: [`.workspace{max-width:900px;margin:0 auto}.hero{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.eyebrow{font-size:11px;letter-spacing:.08em;color:#687386;margin:0}.muted,.audit{color:#687386;font-size:13px}.paper{padding:8px 12px;border:1px solid #d7dce6;border-radius:999px;font-size:11px;font-weight:700}.panel,.notice,.safety{border:1px solid #e1e5ec;border-radius:14px;background:#fff;padding:20px;margin-top:18px}.form{display:grid;gap:8px}.form label{font-size:12px;font-weight:600;margin-top:8px}.form input[type=text],.form input[type=number],.form input:not([type]){padding:12px;border:1px solid #d7dce6;border-radius:9px;font:inherit}.form button{margin-top:10px;padding:12px 18px;border:0;border-radius:9px;cursor:pointer}.form button:disabled{cursor:not-allowed;opacity:.6}.confirm{display:flex!important;gap:8px;align-items:center}.confirm input{width:16px;height:16px}.notice{display:flex;gap:10px;flex-direction:column}.error{border-color:#d89b9b}.risk.blocked{border-color:#d89b9b}.facts{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-top:18px}.facts span{padding:10px;background:#f7f9fb;border-radius:8px;font-size:12px;color:#687386}.facts b{display:block;color:#202733;margin-top:4px}.safety{background:#f7f9fb}@media(max-width:700px){.hero{display:block}.paper{display:inline-block;margin-top:16px}.facts{grid-template-columns:repeat(2,1fr)}}`]
})
export class PaperTradeComponent {
  private readonly api = inject(TradingApiService);
  symbol = 'RELIANCE';
  quantity = 1;
  confirmed = false;
  submitting = false;
  error = '';
  errorCode = '';
  risk?: RiskResult;
  result?: PaperTradeResult;

  onSymbolInput(event: Event): void { this.symbol = (event.target as HTMLInputElement).value; }
  onQuantityInput(event: Event): void { this.quantity = Number((event.target as HTMLInputElement).value); }
  onConfirmation(event: Event): void { this.confirmed = (event.target as HTMLInputElement).checked; }

  submit(event: Event): void {
    event.preventDefault();
    if (!this.confirmed) { this.errorCode = 'CONFIRMATION_REQUIRED'; this.error = 'Confirm the paper-only execution before submitting.'; return; }
    if (!this.symbol.trim() || !Number.isInteger(this.quantity) || this.quantity <= 0) { this.errorCode = 'INVALID_ORDER'; this.error = 'Enter a positive whole-number quantity and symbol.'; return; }
    this.submitting = true; this.error = ''; this.errorCode = ''; this.risk = undefined; this.result = undefined;
    const idempotencyKey = crypto.randomUUID();
    this.api.paperTrade(this.symbol.trim().toUpperCase(), this.quantity, idempotencyKey).subscribe({
      next: value => { this.result = value; this.risk = value.risk; this.submitting = false; },
      error: (response: HttpErrorResponse) => {
        this.errorCode = response.error?.errorCode || `HTTP_${response.status}`;
        this.error = response.error?.message || 'The paper trade was not executed.';
        this.risk = response.error?.risk;
        this.submitting = false;
      }
    });
  }
}
