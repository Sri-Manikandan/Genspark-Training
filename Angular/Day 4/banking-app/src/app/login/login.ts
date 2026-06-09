import { Component, signal } from '@angular/core';
import { LoginModel } from '../models/login.model';
import { FormsModule } from '@angular/forms';
import { BankingApiService } from '../services/bankingapi.service';
import { changeUsername } from '../rxjs/auth.operator';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  loginModel = signal(new LoginModel());
  progress = signal(false);
  constructor(private bankingApiService: BankingApiService, private router: Router) {

  }

  handleLoginClick(){
    this.progress.set(true);
    this.bankingApiService.loginApiCall(this.loginModel()).subscribe({
      next: (response:any) => {
        console.log("Login successful", response);
        sessionStorage.setItem('token', response.token);
        alert("Login successful!")
        this.progress.set(false);
        changeUsername(response.username);
        this.router.navigate(['/customers']);
      },
      error: (error) => {
        console.error("Login failed", error);
        alert("Login failed. Please try again.");
        this.progress.set(false);
      }
    });
    
  }

}
