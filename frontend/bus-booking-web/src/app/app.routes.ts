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
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component')
          .then(m => m.AdminDashboardComponent)
      },
      {
        path: 'cities',
        loadComponent: () => import('./features/admin/cities/admin-cities-page.component')
          .then(m => m.AdminCitiesPageComponent)
      },
      {
        path: 'routes',
        loadComponent: () => import('./features/admin/routes/admin-routes-page.component')
          .then(m => m.AdminRoutesPageComponent)
      },
      {
        path: 'platform-fee',
        loadComponent: () => import('./features/admin/platform-fee/admin-platform-fee-page.component')
          .then(m => m.AdminPlatformFeePageComponent)
      },
      {
        path: 'operator-requests',
        loadComponent: () => import('./features/admin/operator-requests/admin-operator-requests-page.component')
          .then(m => m.AdminOperatorRequestsPageComponent)
      },
      {
        path: 'bus-approvals',
        loadComponent: () => import('./features/admin/bus-approvals/admin-bus-approvals-page.component')
          .then(m => m.AdminBusApprovalsPageComponent)
      }
    ]
  },
  {
    path: 'operator',
    canMatch: [roleGuard(['operator'])],
    loadComponent: () => import('./features/operator/operator-shell/operator-shell.component')
      .then(m => m.OperatorShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/operator/operator-dashboard/operator-dashboard.component')
          .then(m => m.OperatorDashboardComponent)
      },
      {
        path: 'offices',
        loadComponent: () => import('./features/operator/offices/operator-offices-page.component')
          .then(m => m.OperatorOfficesPageComponent)
      },
      {
        path: 'buses',
        loadComponent: () => import('./features/operator/buses/operator-buses-list.component')
          .then(m => m.OperatorBusesListComponent)
      }
    ]
  },
  {
    path: 'become-operator',
    canMatch: [roleGuard(['customer'])],
    loadComponent: () => import('./features/customer/become-operator/become-operator-page.component')
      .then(m => m.BecomeOperatorPageComponent)
  },
  { path: '**', redirectTo: '' }
];
