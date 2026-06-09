import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Customers } from './customers/customers';
import { Account } from './account/account';
import { Products } from './products/products';
import { authGuard } from './rxjs/auth.guard';

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'customers', component: Customers, canActivate: [authGuard] },
    { path: 'account', component: Account, canActivate: [authGuard] },
    { path: 'products', component: Products, canActivate: [authGuard] },
    { path: '**', redirectTo: 'login' }
];
