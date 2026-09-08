import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'research', component: DashboardComponent },
  { path: 'portfolio', component: DashboardComponent },
  { path: '**', redirectTo: 'dashboard' }
];