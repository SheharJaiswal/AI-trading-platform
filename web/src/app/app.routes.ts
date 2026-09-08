import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { ResearchComponent } from './research.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'research', component: ResearchComponent },
  { path: 'portfolio', component: DashboardComponent },
  { path: '**', redirectTo: 'dashboard' }
];
