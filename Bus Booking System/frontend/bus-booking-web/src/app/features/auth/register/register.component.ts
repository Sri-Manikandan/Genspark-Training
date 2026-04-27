import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthStore } from '../../../core/auth/auth.store';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(72)]],
    phone: ['']
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);
    const raw = this.form.getRawValue();
    this.auth.register({
      name: raw.name,
      email: raw.email,
      password: raw.password,
      phone: raw.phone?.trim() || null
    }).subscribe({
      next: () => {
        this.snack.open('Account created. Please log in.', 'OK', { duration: 3000 });
        this.router.navigate(['/login']);
      },
      error: (err) => {
        const code = err?.error?.error?.code;
        this.errorMessage.set(
          code === 'EMAIL_IN_USE'
            ? 'That email is already registered. Try logging in instead.'
            : 'Registration failed. Check your input and try again.');
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false)
    });
  }
}
