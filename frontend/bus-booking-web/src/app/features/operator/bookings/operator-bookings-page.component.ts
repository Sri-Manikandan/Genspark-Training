import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { OperatorBookingsApiService, OperatorBookingListItemDto } from '../../../core/api/operator-bookings.api';
import { OperatorBusesApiService, BusDto } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-bookings-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatSelectModule,
    MatInputModule, MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './operator-bookings-page.component.html'
})
export class OperatorBookingsPageComponent implements OnInit {
  private readonly api = inject(OperatorBookingsApiService);
  private readonly busesApi = inject(OperatorBusesApiService);

  readonly bookings = signal<OperatorBookingListItemDto[]>([]);
  readonly buses = signal<BusDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly busFilter = new FormControl<string | null>(null);
  readonly dateFilter = new FormControl<Date | null>(null);

  readonly displayedColumns = [
    'bookingCode', 'date', 'route', 'bus', 'customer', 'seats', 'amount', 'status'
  ];

  ngOnInit(): void {
    this.busesApi.list().subscribe(buses => this.buses.set(buses));
    this.load();
  }

  load(): void {
    const busId = this.busFilter.value ?? undefined;
    const date = this.dateFilter.value
      ? this.dateFilter.value.toISOString().slice(0, 10)
      : undefined;
    this.api.list(busId, date, this.page(), this.pageSize).subscribe(res => {
      this.bookings.set(res.items);
      this.totalCount.set(res.totalCount);
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.busFilter.setValue(null);
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
