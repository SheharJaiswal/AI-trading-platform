import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: `<section class="workspace"><div><p class="eyebrow">TRADING WORKSPACE</p><h1>Research before you trade.</h1><p class="muted">Market evidence, recommendations and durable paper-trading controls in one place.</p></div><div class="grid"><article><h2>Portfolio</h2><strong>₹1,000,000</strong><p class="muted">Starting paper cash</p></article><article><h2>Market data</h2><strong>Ready</strong><p class="muted">Provider status will appear here</p></article><article><h2>Risk controls</h2><strong>Server enforced</strong><p class="muted">AI cannot authorize trades</p></article></div><div class="notice"><strong>Paper trading only.</strong> Every trade must be explicitly initiated and approved by the deterministic server-side risk gate.</div></section>`
})
export class DashboardComponent {}