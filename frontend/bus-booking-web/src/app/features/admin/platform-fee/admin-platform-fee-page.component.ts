import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AdminPlatformFeeApiService, PlatformFeeDto, PlatformFeeType
} from '../../../core/api/admin-platform-fee.api';

@Component({
  selector: 'app-admin-platform-fee-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule, DatePipe,
    MatButtonModule, MatCardModule, MatFormFieldModule,
    MatInputModule, MatRadioModule
  ],
  templateUrl: './admin-platform-fee-page.component.html',
  styleUrl: './admin-platform-fee-page.component.scss'
})
export class AdminPlatformFeePageComponent {
  private readonly api = inject(AdminPlatformFeeApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly active = signal<PlatformFeeDto | null>(null);
  readonly saving = signal(false);

  readonly form = this.fb.nonNullable.group({
    feeType: ['fixed' as PlatformFeeType, [Validators.required]],
    value: [25, [Validators.required, Validators.min(0), Validators.max(10000)]]
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.api.get().subscribe({
      next: dto => {
        this.active.set(dto);
        this.form.patchValue({ feeType: dto.feeType, value: dto.value });
      },
      error: () => this.snack.open('Failed to load platform fee', 'Dismiss', { duration: 4000 })
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.update(this.form.getRawValue()).subscribe({
      next: dto => {
        this.active.set(dto);
        this.saving.set(false);
        this.snack.open('Platform fee updated', 'Dismiss', { duration: 3000 });
      },
      error: err => {
        this.saving.set(false);
        this.snack.open(err?.error?.error?.message ?? 'Update failed', 'Dismiss', { duration: 4000 });
      }
    });
  }
}
