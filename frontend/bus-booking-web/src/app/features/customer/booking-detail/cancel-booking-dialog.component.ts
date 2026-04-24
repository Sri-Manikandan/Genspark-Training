// frontend/bus-booking-web/src/app/features/customer/booking-detail/cancel-booking-dialog.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BookingDetailDto, BookingsApiService, RefundPreviewDto } from '../../../core/api/bookings.api';
import { RefundPolicyLabelPipe } from '../../../shared/pipes/refund-policy-label.pipe';

export interface CancelDialogData {
  bookingId: string;
  bookingCode: string;
  totalAmount: number;
}

export interface CancelDialogResult {
  cancelled: boolean;
  detail?: BookingDetailDto;
}

@Component({
  selector: 'app-cancel-booking-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    CurrencyPipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    RefundPolicyLabelPipe
  ],
  templateUrl: './cancel-booking-dialog.component.html'
})
export class CancelBookingDialogComponent implements OnInit {
  private readonly api = inject(BookingsApiService);
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<CancelBookingDialogComponent, CancelDialogResult>);
  readonly data = inject<CancelDialogData>(MAT_DIALOG_DATA);

  readonly loadingPreview = signal(true);
  readonly preview = signal<RefundPreviewDto | null>(null);
  readonly previewError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.maxLength(500)]]
  });

  ngOnInit(): void {
    this.api.getRefundPreview(this.data.bookingId).subscribe({
      next: (p) => { this.preview.set(p); this.loadingPreview.set(false); },
      error: (err) => {
        this.loadingPreview.set(false);
        this.previewError.set(err?.error?.error?.message ?? 'Could not load refund preview');
      }
    });
  }

  confirm(): void {
    if (this.submitting() || !this.preview()?.cancellable) return;
    this.submitting.set(true);
    this.submitError.set(null);

    const reason = this.form.controls.reason.value.trim();

    this.api.cancelBooking(this.data.bookingId, { reason: reason.length > 0 ? reason : null }).subscribe({
      next: (detail) => {
        this.submitting.set(false);
        this.ref.close({ cancelled: true, detail });
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(err?.error?.error?.message ?? 'Cancellation failed');
      }
    });
  }

  dismiss(): void {
    this.ref.close({ cancelled: false });
  }
}
