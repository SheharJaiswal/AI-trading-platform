import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { PaperTradeComponent } from './paper-trade.component';
import { ResearchComponent } from './research.component';
import { BacktestComponent } from './backtest.component';
import { PaperSessionComponent } from './paper-session.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'research', component: ResearchComponent },
  { path: 'portfolio', component: DashboardComponent },
  { path: 'paper-trade', component: PaperTradeComponent },
  { path: 'paper-session', component: PaperSessionComponent },
  { path: 'backtest', component: BacktestComponent },
  { path: '**', redirectTo: 'dashboard' }
];
