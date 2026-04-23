import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import {
  AdminBookingsApiService, AdminBookingListItemDto
} from '../../../core/api/admin-bookings.api';
import {
  AdminOperatorsApiService, AdminOperatorListItemDto
} from '../../../core/api/admin-operators.api';

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: 'confirmed', label: 'Confirmed' },
  { value: 'completed', label: 'Completed' },
  { value: 'cancelled', label: 'Cancelled (user)' },
  { value: 'cancelled_by_operator', label: 'Cancelled (operator)' }
];

@Component({
  selector: 'app-admin-bookings-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatSelectModule,
    MatInputModule, MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './admin-bookings-page.component.html'
})
export class AdminBookingsPageComponent implements OnInit {
  private readonly bookingsApi = inject(AdminBookingsApiService);
  private readonly operatorsApi = inject(AdminOperatorsApiService);

  readonly bookings = signal<AdminBookingListItemDto[]>([]);
  readonly operators = signal<AdminOperatorListItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly statusOptions = STATUS_OPTIONS;
  readonly operatorFilter = new FormControl<string | null>(null);
  readonly statusFilter = new FormControl<string | null>(null);
  readonly dateFilter = new FormControl<Date | null>(null);

  readonly columns = [
    'bookingCode', 'date', 'route', 'operator', 'bus',
    'customer', 'seats', 'amount', 'status'
  ];

  ngOnInit(): void {
    this.operatorsApi.list().subscribe(list => this.operators.set(list));
    this.load();
  }

  load(): void {
    const opId = this.operatorFilter.value ?? undefined;
    const status = this.statusFilter.value ?? undefined;
    const date = this.dateFilter.value
      ? this.dateFilter.value.toISOString().slice(0, 10)
      : undefined;
    this.bookingsApi.list(opId, status, date, this.page(), this.pageSize).subscribe(res => {
      this.bookings.set(res.items);
      this.totalCount.set(res.totalCount);
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.operatorFilter.setValue(null);
    this.statusFilter.setValue(null);
    this.dateFilter.setValue(null);
    this.page.set(1);
    this.load();
  }

  statusClass(status: string): string {
    switch (status) {
      case 'confirmed': return 'bg-green-100 text-green-800';
      case 'completed': return 'bg-blue-100 text-blue-800';
      case 'cancelled':
      case 'cancelled_by_operator': return 'bg-red-100 text-red-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  }

  formatStatus(status: string): string {
    return status.replace(/_/g, ' ');
  }
}
