import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { SearchApiService, TripDetailDto } from '../../../core/api/search.api';
import { BookingsApiService } from '../../../core/api/bookings.api';
import { SeatMapComponent } from '../../../shared/components/seat-map/seat-map.component';

@Component({
  selector: 'app-trip-detail',
  standalone: true,
  imports: [CommonModule, SeatMapComponent],
  templateUrl: './trip-detail.component.html',
  styleUrl: './trip-detail.component.scss'
})
export class TripDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SearchApiService);
  private readonly router = inject(Router);
  private readonly bookings = inject(BookingsApiService);
  private readonly location = inject(Location);
  private readonly snack = inject(MatSnackBar);

  readonly trip = signal<TripDetailDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);
  readonly selectedSeats = signal<string[]>([]);
  readonly locking = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.api.getTripDetail(id).subscribe({
      next: (data) => {
        this.trip.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Trip not found or no longer available.');
        this.loading.set(false);
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  onSelectionChange(seats: string[]): void {
    this.selectedSeats.set(seats);
  }

  bookNow(): void {
    const t = this.trip();
    const seats = this.selectedSeats();
    if (!t || seats.length === 0 || this.locking()) return;

    this.locking.set(true);
    const sessionId = crypto.randomUUID();
    this.bookings.lockSeats(t.tripId, { sessionId, seats }).subscribe({
      next: (lock) => {
        this.locking.set(false);
        this.router.navigate(['/checkout', t.tripId], {
          queryParams: {
            lockId: lock.lockId,
            sessionId: lock.sessionId,
            seats: lock.seats.join(','),
            expiresAt: lock.expiresAt,
            fare: t.farePerSeat
          }
        });
      },
      error: (err) => {
        this.locking.set(false);
        const code = err?.error?.error?.code;
        const msg =
          code === 'SEAT_UNAVAILABLE'
            ? 'One or more of those seats were just taken — please pick again.'
            : 'Could not hold those seats. Please try again.';
        this.snack.open(msg, 'Dismiss', { duration: 5000 });
        this.api.getTripDetail(t.tripId).subscribe((updated) => this.trip.set(updated));
      }
    });
  }
}
