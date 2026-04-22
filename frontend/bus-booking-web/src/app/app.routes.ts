import { Routes } from '@angular/router';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'admin',
    canMatch: [roleGuard(['admin'])],
    loadComponent: () =>
      import('./features/admin/admin-dashboard/admin-dashboard.component')
        .then(m => m.AdminDashboardComponent)
  },
  { path: '**', redirectTo: '' }
];
