import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CustomerModel } from '../models/customers.model';

@Component({
  selector: 'app-customers',
  imports: [FormsModule],
  templateUrl: './customers.html',
  styleUrl: './customers.css',
})
export class Customers {
  // customer: CustomerModel = new CustomerModel("manish", "Manish", "manish@gmail.com", "1234567890", "active", new Date());
  customer: CustomerModel = new CustomerModel();
  styleclass:string = "tableclass";

  handleChangeClick(){
    alert("Customer Name Changed: " + this.customer.name);
  }
}
