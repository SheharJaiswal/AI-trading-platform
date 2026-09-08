import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TradingApiService } from './core/api/trading-api.service';
import { BacktestResponse } from './core/api/trading-api.models';

@Component({
  selector: 'app-backtest',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="backtest">
      <header><h1>Backtest</h1><span class="simulation">HISTORICAL SIMULATION ONLY</span></header>
      <p>Replay historical candles with deterministic strategy, risk, fees and slippage.</p>
      <form (ngSubmit)="submit()">
        <label>Symbol <input name="symbol" [(ngModel)]="symbol" required></label>
        <label>Interval <input name="interval" [(ngModel)]="interval" required></label>
        <label>Start <input type="datetime-local" name="start" [(ngModel)]="start" required></label>
        <label>End <input type="datetime-local" name="end" [(ngModel)]="end" required></label>
        <label>Starting cash <input type="number" name="cash" [(ngModel)]="startingCash" min="1" required></label>
        <label>Quantity / trade <input type="number" name="quantity" [(ngModel)]="quantity" min="1" required></label>
        <button type="submit" [disabled]="loading">{{ loading ? 'Running…' : 'Run simulation' }}</button>
      </form>
      <p *ngIf="message" class="status">{{ message }}</p>
      <section *ngIf="result" class="results">
        <h2>Simulation result</h2>
        <div class="metrics">
          <div><span>Ending equity</span><strong>{{ result.result.endingCash | number:'1.2-2' }}</strong></div>
          <div><span>Return</span><strong>{{ result.result.returnPercent | number:'1.2-2' }}%</strong></div>
          <div><span>Max drawdown</span><strong>{{ result.result.maxDrawdownPercent | number:'1.2-2' }}%</strong></div>
          <div><span>Trades</span><strong>{{ result.result.trades.length }}</strong></div>
          <div><span>Risk events</span><strong>{{ result.result.riskEvents.length }}</strong></div>
        </div>
        <h3>Recent simulated trades</h3>
        <table *ngIf="result.result.trades.length; else noTrades"><thead><tr><th>Time</th><th>Side</th><th>Qty</th><th>Price</th><th>Fee</th><th>Risk</th></tr></thead>
          <tbody><tr *ngFor="let trade of result.result.trades.slice(-10)"><td>{{ trade.timestamp | date:'short' }}</td><td>{{ trade.side }}</td><td>{{ trade.quantity }}</td><td>{{ trade.price | number:'1.2-2' }}</td><td>{{ trade.fee | number:'1.2-2' }}</td><td>{{ trade.riskDecision }}</td></tr></tbody>
        </table>
        <ng-template #noTrades><p>No trades were approved in this simulation.</p></ng-template>
      </section>
    </section>
  `,
  styles: [`
    .backtest{max-width:1100px;margin:2rem auto;padding:1.5rem}header{display:flex;align-items:center;gap:1rem}.simulation{font-size:.75rem;padding:.35rem .6rem;border:1px solid currentColor}form{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem;margin-top:1.5rem}label{display:grid;gap:.35rem}input{padding:.6rem}button{padding:.7rem 1rem}.status{margin-top:1rem}.results{margin-top:2rem}.metrics{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:.75rem}.metrics div{border:1px solid #ddd;padding:1rem}.metrics span,.metrics strong{display:block}.metrics strong{font-size:1.15rem;margin-top:.35rem}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:.55rem;border-bottom:1px solid #ddd}@media(max-width:800px){form,.metrics{grid-template-columns:1fr 1fr}}@media(max-width:520px){form,.metrics{grid-template-columns:1fr}}
  `]
})
export class BacktestComponent {
  private readonly api = inject(TradingApiService);
  symbol = 'INFY'; interval = '1d'; start = '2026-01-01T00:00'; end = '2026-02-01T00:00'; startingCash = 100000; quantity = 10;
  message = ''; loading = false; result: BacktestResponse | null = null;

  submit() {
    this.loading = true; this.message = ''; this.result = null;
    this.api.backtest({ symbol: { value: this.symbol.trim().toUpperCase() }, interval: this.interval.trim(), start: new Date(this.start).toISOString(), end: new Date(this.end).toISOString(), configuration: { startingCash: this.startingCash, quantityPerTrade: this.quantity, feeRate: 0.001, slippageBasisPoints: 5, strategyVersion: 'baseline-v1' } }).subscribe({
      next: response => { this.result = response; this.message = `${response.status}: ${response.simulationLabel}`; this.loading = false; },
      error: error => { this.message = error?.error?.message ?? 'Backtest failed. Check historical data and configuration.'; this.loading = false; }
    });
  }
}
