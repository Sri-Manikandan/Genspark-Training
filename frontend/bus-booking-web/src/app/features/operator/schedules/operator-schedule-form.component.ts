import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { OperatorSchedulesApiService, RouteOptionDto } from '../../../core/api/schedules.api';
import { OperatorBusesApiService, BusDto } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-schedule-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule
  ],
  templateUrl: './operator-schedule-form.component.html'
})
export class OperatorScheduleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly schedulesApi = inject(OperatorSchedulesApiService);
  private readonly busesApi = inject(OperatorBusesApiService);
  private readonly dialogRef = inject(MatDialogRef);

  readonly buses = signal<BusDto[]>([]);
  readonly routes = signal<RouteOptionDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    busId: ['', Validators.required],
    routeId: ['', Validators.required],
    departureTime: ['09:00', Validators.required],
    arrivalTime: ['18:00', Validators.required],
    farePerSeat: [500, [Validators.required, Validators.min(1)]],
    validFrom: ['', Validators.required],
    validTo: ['', Validators.required],
    daysOfWeek: [127, Validators.required]
  });

  readonly dayOptions = [
    { label: 'Mon', value: 1 }, { label: 'Tue', value: 2 }, { label: 'Wed', value: 4 },
    { label: 'Thu', value: 8 }, { label: 'Fri', value: 16 }, { label: 'Sat', value: 32 },
    { label: 'Sun', value: 64 }
  ];

  ngOnInit(): void {
    // Set default dates (today to +30 days)
    const today = new Date();
    const nextMonth = new Date();
    nextMonth.setDate(today.getDate() + 30);
    
    this.form.patchValue({
      validFrom: today.toISOString().split('T')[0],
      validTo: nextMonth.toISOString().split('T')[0]
    });

    this.busesApi.list().subscribe(list => 
      this.buses.set(list.filter(b => b.approvalStatus === 'approved')));
    this.schedulesApi.listRoutes().subscribe(list => this.routes.set(list));
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    const payload = {
      ...v,
      departureTime: this.toHms(v.departureTime),
      arrivalTime: this.toHms(v.arrivalTime)
    };

    this.schedulesApi.create(payload).subscribe({
      next: () => this.dialogRef.close(true),
      error: (err) => {
        this.error.set(
          err.error?.error?.message
          ?? err.error?.message
          ?? err.error?.title
          ?? (typeof err.error === 'string' ? err.error : null)
          ?? 'Failed to create schedule'
        );
        this.saving.set(false);
      }
    });
  }

  private toHms(t: string): string {
    return t && t.length === 5 ? `${t}:00` : t;
  }
}
