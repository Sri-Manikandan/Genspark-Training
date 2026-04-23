import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { BusType, OperatorBusesApiService } from '../../../core/api/operator-buses.api';

const BUS_TYPES: BusType[] = ['seater', 'sleeper', 'semi_sleeper'];

@Component({
  selector: 'app-operator-bus-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule],
  templateUrl: './operator-bus-form.component.html'
})
export class OperatorBusFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OperatorBusesApiService);
  private readonly dialogRef = inject(MatDialogRef<OperatorBusFormComponent>);

  readonly busTypes = BUS_TYPES;
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    registrationNumber: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
    busName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(60)]],
    busType: ['seater' as BusType, Validators.required],
    rows: [10, [Validators.required, Validators.min(1), Validators.max(26)]],
    columns: [4, [Validators.required, Validators.min(1), Validators.max(12)]]
  });

  get capacity(): number {
    const { rows, columns } = this.form.getRawValue();
    return rows * columns;
  }

  save(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    this.api.create(this.form.getRawValue()).subscribe({
      next: (bus) => this.dialogRef.close(bus),
      error: (err) => {
        this.saving.set(false);
        const code = err?.error?.error?.code;
        this.error.set(code === 'REGISTRATION_NUMBER_TAKEN'
          ? 'Registration number already in use.'
          : 'Failed to add bus. Please try again.');
      }
    });
  }
}
