import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Customers } from './customers/customers';
import { Account } from './account/account';
import { Products } from './products/products';

export const routes: Routes = [
    { path: '', redirectTo: 'login', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'customers', component: Customers },
    { path: 'account', component: Account },
    { path: 'products', component: Products },
    { path: '**', redirectTo: 'login' }

];
