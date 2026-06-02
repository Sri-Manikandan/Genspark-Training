import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Customers } from './customers/customers';
import { Products } from "./products/products";
import { Login } from "./login/login";
import { Register } from './register/register';

@Component({
  selector: 'app-root',
  imports: [Customers, Products, Login, Register],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('banking-app');
}
