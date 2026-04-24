// frontend/bus-booking-web/src/app/features/customer/bookings-list/bookings-list-page.component.ts
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatTabsModule, MatTabChangeEvent } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import {
  BookingFilter,
  BookingListItemDto,
  BookingsApiService
} from '../../../core/api/bookings.api';
import { BookingStatusBadgeComponent } from '../../../shared/components/booking-status-badge/booking-status-badge.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-bookings-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    BookingStatusBadgeComponent,
    EmptyStateComponent
  ],
  templateUrl: './bookings-list-page.component.html'
})
export class BookingsListPageComponent implements OnInit {
  private readonly api = inject(BookingsApiService);
  private readonly router = inject(Router);

  readonly filter = signal<BookingFilter>('upcoming');
  readonly items = signal<BookingListItemDto[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly columns = ['code', 'route', 'date', 'seats', 'amount', 'status', 'actions'];

  private readonly tabIndexToFilter: BookingFilter[] = ['upcoming', 'past', 'cancelled'];

  ngOnInit(): void {
    this.load();
  }

  onTabChange(e: MatTabChangeEvent): void {
    this.filter.set(this.tabIndexToFilter[e.index]);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.listBookings(this.filter(), 1, 50).subscribe({
      next: (resp) => {
        this.items.set(resp.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.message ?? 'Could not load your bookings');
      }
    });
  }

  open(row: BookingListItemDto): void {
    this.router.navigate(['/my-bookings', row.bookingId]);
  }
}
