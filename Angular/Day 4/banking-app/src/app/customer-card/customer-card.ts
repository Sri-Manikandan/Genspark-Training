import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CustomerModel } from '../models/customer.model';

@Component({
  selector: 'app-customer-card',
  imports: [],
  templateUrl: './customer-card.html',
  styleUrl: './customer-card.css',
})
export class CustomerCard {
  @Input() customer!: CustomerModel;
  @Output() customerSelected = new EventEmitter<CustomerModel>();

  onSelectClick(){
    this.customerSelected.emit(this.customer);
  }
}
