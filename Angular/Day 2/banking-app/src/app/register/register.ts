import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RegisterModel } from '../models/register.model';
import { BankingApiService } from '../services/bankingapi.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  user = signal(new RegisterModel());

  constructor(private bankingApiService: BankingApiService) {

  }

  handleRegisterClick() {
    console.log("Register button clicked");
    this.bankingApiService.registerApiCall(this.user()).subscribe({
      next: (response)=>{
        console.log("Registration successful", response);
        alert("Registration successful! Please log in.");
      },
      error: (error)=>{
        console.error("Registration failed", error);
        console.log(this.user());
        alert("Registration failed. Please try again.");
      },
      complete: ()=>{
        console.log("Registration API call completed");
      } 
    })
  }
}
