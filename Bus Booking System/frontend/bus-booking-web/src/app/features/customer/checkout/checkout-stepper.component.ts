import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthStore } from '../../../core/auth/auth.store';
import {
  BookingsApiService,
  CreateBookingResponseDto,
  PassengerDto
} from '../../../core/api/bookings.api';
import { CountdownTimerComponent } from '../../../shared/components/countdown-timer/countdown-timer.component';

@Component({
  selector: 'app-checkout-stepper',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, CountdownTimerComponent],
  templateUrl: './checkout-stepper.component.html',
  styleUrl: './checkout-stepper.component.scss'
})
export class CheckoutStepperComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BookingsApiService);
  private readonly auth = inject(AuthStore);
  private readonly snack = inject(MatSnackBar);

  readonly tripId = signal<string>('');
  readonly lockId = signal<string>('');
  readonly sessionId = signal<string>('');
  readonly seats = signal<string[]>([]);
  readonly expiresAt = signal<string>('');
  readonly fare = signal<number>(0);
  readonly submitting = signal(false);
  readonly lockExpired = signal(false);
  readonly currentStep = signal<1 | 2 | 3>(1);
  readonly platformFee = signal<number | null>(null);
  readonly grandTotal = signal<number | null>(null);
  private bookingConfirmed = false;

  readonly total = computed(() => this.fare() * this.seats().length);

  readonly passengersForm = this.fb.nonNullable.group({
    passengers: this.fb.array<ReturnType<typeof this.buildPassengerGroup>>([])
  });

  get passengersArray(): FormArray {
    return this.passengersForm.controls.passengers;
  }

  ngOnInit(): void {
    const pm = this.route.snapshot.paramMap;
    const qm = this.route.snapshot.queryParamMap;
    this.tripId.set(pm.get('tripId') ?? '');
    this.lockId.set(qm.get('lockId') ?? '');
    this.sessionId.set(qm.get('sessionId') ?? '');
    this.seats.set((qm.get('seats') ?? '').split(',').filter(Boolean));
    this.expiresAt.set(qm.get('expiresAt') ?? '');
    this.fare.set(Number(qm.get('fare') ?? 0));

    if (!this.lockId() || this.seats().length === 0) {
      this.router.navigate(['/']);
      return;
    }

    for (const s of this.seats()) {
      this.passengersArray.push(this.buildPassengerGroup(s));
    }

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url }
      });
    }
  }

  buildPassengerGroup(seatNumber: string) {
    return this.fb.nonNullable.group({
      seatNumber: [seatNumber],
      passengerName: ['', [Validators.required, Validators.maxLength(120)]],
      passengerAge: [30, [Validators.required, Validators.min(1), Validators.max(120)]],
      passengerGender: ['male' as 'male' | 'female' | 'other', Validators.required]
    });
  }

  onLockExpired(): void {
    this.lockExpired.set(true);
  }

  nextStep(): void {
    const s = this.currentStep();
    if (s < 3) this.currentStep.set((s + 1) as 1 | 2 | 3);
  }

  prevStep(): void {
    const s = this.currentStep();
    if (s > 1) this.currentStep.set((s - 1) as 1 | 2 | 3);
  }

  payNow(): void {
    if (this.passengersForm.invalid || this.submitting() || this.lockExpired()) return;
    this.submitting.set(true);

    const passengers: PassengerDto[] = this.passengersArray.controls.map(
      (c) => c.getRawValue() as PassengerDto
    );

    this.api
      .createBooking({
        tripId: this.tripId(),
        lockId: this.lockId(),
        sessionId: this.sessionId(),
        passengers
      })
      .subscribe({
        next: (created) => this.openRazorpay(created),
        error: (err) => {
          this.submitting.set(false);
          const code = err?.error?.error?.code;
          if (code === 'LOCK_EXPIRED') {
            this.lockExpired.set(true);
          } else {
            this.snack.open(err?.error?.error?.message ?? 'Failed to create booking', 'Dismiss', {
              duration: 5000
            });
          }
        }
      });
  }

  private openRazorpay(created: CreateBookingResponseDto): void {
    this.platformFee.set(created.platformFee);
    this.grandTotal.set(created.totalAmount);
    const user = this.auth.user();
    const options: RazorpayOptions = {
      key: created.keyId,
      amount: created.amount,
      currency: created.currency,
      name: 'BusBooking',
      description: `Booking ${created.bookingCode}`,
      order_id: created.razorpayOrderId,
      prefill: { name: user?.name, email: user?.email },
      theme: { color: '#3f51b5' },
      handler: (resp) => this.verify(created.bookingId, resp),
      modal: {
        ondismiss: () => {
          this.submitting.set(false);
          this.snack.open(
            'Payment dismissed. You can retry or wait for the lock to expire.',
            'Dismiss',
            { duration: 4000 }
          );
        }
      }
    };

    const rzp = new window.Razorpay(options);
    rzp.open();
  }

  private verify(bookingId: string, resp: RazorpayHandlerResponse): void {
    this.api
      .verifyPayment(bookingId, {
        razorpayPaymentId: resp.razorpay_payment_id,
        razorpaySignature: resp.razorpay_signature
      })
      .subscribe({
        next: () => {
          this.bookingConfirmed = true;
          this.router.navigate(['/booking-confirmation', bookingId]);
        },
        error: (err) => {
          this.submitting.set(false);
          this.snack.open(err?.error?.error?.message ?? 'Payment verification failed', 'Dismiss', {
            duration: 6000
          });
        }
      });
  }

  ngOnDestroy(): void {
    // After successful payment the server deletes the locks; don't fire a best-effort release.
    if (this.bookingConfirmed || this.submitting() || !this.lockId()) return;
    this.api.releaseLock(this.lockId(), this.sessionId()).subscribe({ next: () => {}, error: () => {} });
  }
}

