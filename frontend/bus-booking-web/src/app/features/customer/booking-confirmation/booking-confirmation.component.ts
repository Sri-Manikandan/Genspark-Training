import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BookingDetailDto, BookingsApiService } from '../../../core/api/bookings.api';
import { AuthTokenStore } from '../../../core/auth/auth-token.store';

@Component({
  selector: 'app-booking-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatIconModule],
  templateUrl: './booking-confirmation.component.html'
})
export class BookingConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BookingsApiService);
  private readonly tokens = inject(AuthTokenStore);

  readonly booking = signal<BookingDetailDto | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }
    this.api.getBooking(id).subscribe({
      next: (b) => {
        this.booking.set(b);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  downloadTicket(): void {
    const b = this.booking();
    if (!b) return;
    const url = this.api.getTicketUrl(b.bookingId);
    const token = this.tokens.token();
    fetch(url, { headers: { Authorization: `Bearer ${token ?? ''}` } })
      .then((r) => r.blob())
      .then((blob) => {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `ticket-${b.bookingCode}.pdf`;
        document.body.appendChild(link);
        link.click();
        link.remove();
      });
  }
}

