import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CustomerModel } from '../models/customer.model';
import { CustomerCard } from '../customer-card/customer-card';
import { getUsernameFromToken } from '../rxjs/auth.operator';

@Component({
  selector: 'app-customers',
  imports: [FormsModule, CustomerCard],
  templateUrl: './customers.html',
  styleUrl: './customers.css',
})
export class Customers {
  username = signal(getUsernameFromToken());
  customer:CustomerModel = new CustomerModel();
  selectedCustomer: CustomerModel | null = null;

  onCustomerSelected(customer: CustomerModel){
    this.selectedCustomer = customer;
    alert("Selected Customer: " + customer.name);
  }

  handleChangeClick(){
    alert("Customer Name: " + this.customer.name);
  }
}
