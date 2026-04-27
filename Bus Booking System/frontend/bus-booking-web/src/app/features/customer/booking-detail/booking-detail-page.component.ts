// frontend/bus-booking-web/src/app/features/customer/booking-detail/booking-detail-page.component.ts
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BookingDetailDto, BookingsApiService } from '../../../core/api/bookings.api';
import { BookingStatusBadgeComponent } from '../../../shared/components/booking-status-badge/booking-status-badge.component';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';
import { CancelBookingDialogComponent, CancelDialogResult } from './cancel-booking-dialog.component';

@Component({
  selector: 'app-booking-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DatePipe, RouterLink, BookingStatusBadgeComponent],
  templateUrl: './booking-detail-page.component.html',
  styleUrl: './booking-detail-page.component.scss'
})
export class BookingDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BookingsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly tokens = inject(AuthTokenStore);
  private readonly snack = inject(MatSnackBar);

  readonly booking = signal<BookingDetailDto | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly canCancel = computed(() => this.booking()?.status === 'confirmed');
  readonly canDownloadTicket = computed(() => {
    const s = this.booking()?.status;
    return s === 'confirmed' || s === 'completed';
  });
  readonly isCancelled = computed(() => {
    const s = this.booking()?.status;
    return s === 'cancelled' || s === 'cancelled_by_operator';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.loading.set(false); return; }
    this.fetch(id);
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getBooking(id).subscribe({
      next: (b) => { this.booking.set(b); this.loading.set(false); },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.message ?? 'Booking not found');
      }
    });
  }

  openCancelDialog(): void {
    const b = this.booking();
    if (!b) return;
    const ref = this.dialog.open<CancelBookingDialogComponent, { bookingId: string; bookingCode: string; totalAmount: number }, CancelDialogResult>(
      CancelBookingDialogComponent,
      {
        width: '480px',
        data: { bookingId: b.bookingId, bookingCode: b.bookingCode, totalAmount: b.totalAmount }
      }
    );
    ref.afterClosed().subscribe((result) => {
      if (result?.cancelled && result.detail) {
        this.booking.set(result.detail);
        this.snack.open('Booking cancelled. Refund email is on its way.', 'Dismiss', { duration: 4000 });
      }
    });
  }

  downloadTicket(): void {
    const b = this.booking();
    if (!b) return;
    const url = this.api.getTicketUrl(b.bookingId);
    fetch(url, { headers: { Authorization: `Bearer ${this.tokens.token() ?? ''}` } })
      .then(r => r.blob())
      .then(blob => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `ticket-${b.bookingCode}.pdf`;
        document.body.appendChild(link);
        link.click();
        link.remove();
      });
  }
}
