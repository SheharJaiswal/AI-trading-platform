import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `<div class="app-shell"><header><a class="brand" routerLink="/dashboard">AI Trading Platform</a><nav><a routerLink="/dashboard" routerLinkActive="active">Dashboard</a><a routerLink="/research" routerLinkActive="active">Research</a><a routerLink="/portfolio" routerLinkActive="active">Portfolio</a><a routerLink="/paper-trade" routerLinkActive="active">Paper Trade</a><a routerLink="/backtest" routerLinkActive="active">Backtest</a></nav><span class="paper-badge">PAPER ONLY</span></header><main><router-outlet /></main></div>`
})
export class AppComponent {}