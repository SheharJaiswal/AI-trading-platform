import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

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
        <button type="submit">Run simulation</button>
      </form>
      <p *ngIf="message" class="status">{{ message }}</p>
    </section>
  `,
  styles: [`
    .backtest{max-width:900px;margin:2rem auto;padding:1.5rem} header{display:flex;align-items:center;gap:1rem}.simulation{font-size:.75rem;padding:.35rem .6rem;border:1px solid currentColor}form{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem;margin-top:1.5rem}label{display:grid;gap:.35rem}input{padding:.6rem}button{padding:.7rem 1rem}.status{margin-top:1rem}
  `]
})
export class BacktestComponent {
  symbol = 'INFY'; interval = '1d'; start = '2026-01-01T00:00'; end = '2026-02-01T00:00'; startingCash = 100000; quantity = 10; message = '';
  submit() { this.message = 'Simulation request prepared. The API will validate historical data before execution.'; }
}
