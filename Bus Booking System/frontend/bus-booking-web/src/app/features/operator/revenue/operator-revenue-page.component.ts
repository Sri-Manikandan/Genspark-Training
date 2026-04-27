import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { OperatorRevenueApiService, OperatorRevenueResponseDto } from '../../../core/api/operator-revenue.api';
import { toLocalDateString } from '../../../core/utils/date-utils';

@Component({
  selector: 'app-operator-revenue-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatDatepickerModule
  ],
  templateUrl: './operator-revenue-page.component.html'
})
export class OperatorRevenuePageComponent implements OnInit {
  private readonly api = inject(OperatorRevenueApiService);

  readonly revenue = signal<OperatorRevenueResponseDto | null>(null);
  readonly displayedColumns = [
    'busName', 'registrationNumber', 'confirmedBookings', 'totalSeats', 'totalFare'
  ];

  readonly fromDate = new FormControl<Date | null>(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1)
  );
  readonly toDate = new FormControl<Date | null>(new Date());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const from = this.fromDate.value ? toLocalDateString(this.fromDate.value) : undefined;
    const to   = this.toDate.value   ? toLocalDateString(this.toDate.value)   : undefined;
    this.api.get(from, to).subscribe(res => this.revenue.set(res));
  }
}
