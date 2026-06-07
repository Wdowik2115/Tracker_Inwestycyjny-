import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { WalletsComponent } from './components/wallets/wallets.component';
import { WalletDetailComponent } from './components/wallets/wallet-detail/wallet-detail.component';
import { TransactionsComponent } from './components/transactions/transactions.component';
import { AlertsComponent } from './components/alerts/alerts.component';
import { SettingsComponent } from './components/settings/settings.component';
import { ReportsComponent } from './components/reports/reports.component';
import { WatchlistComponent } from './components/watchlist/watchlist.component';
import { SharedLayoutComponent } from './components/layout/shared-layout.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    component: SharedLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'wallets', component: WalletsComponent },
      { path: 'wallets/:id', component: WalletDetailComponent },
      { path: 'transactions', component: TransactionsComponent },
      { path: 'watchlist', component: WatchlistComponent },
      { path: 'alerts', component: AlertsComponent },
      { path: 'settings', component: SettingsComponent },
      { path: 'reports', component: ReportsComponent },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
