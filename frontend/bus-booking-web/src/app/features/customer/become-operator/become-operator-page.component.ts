import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { OperatorRequestsApiService } from '../../../core/api/operator-requests.api';

@Component({
  selector: 'app-become-operator-page',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatCardModule, MatProgressSpinnerModule, MatIconModule, RouterLink],
  templateUrl: './become-operator-page.component.html',
  styleUrl: './become-operator-page.component.scss'
})
export class BecomeOperatorPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OperatorRequestsApiService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(160)]]
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) return;
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.api.submit(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.submitting.set(false);
        const code = err?.error?.error?.code;
        if (code === 'REQUEST_ALREADY_PENDING') {
          this.errorMessage.set('You already have a pending operator application.');
        } else if (code === 'ALREADY_OPERATOR') {
          this.errorMessage.set('Your account is already an operator.');
        } else {
          this.errorMessage.set('Something went wrong. Please try again.');
        }
      }
    });
  }
}
