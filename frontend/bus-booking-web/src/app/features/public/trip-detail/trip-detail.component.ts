import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { SearchApiService, TripDetailDto } from '../../../core/api/search.api';
import { SeatMapComponent } from '../../../shared/components/seat-map/seat-map.component';

@Component({
  selector: 'app-trip-detail',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, SeatMapComponent],
  templateUrl: './trip-detail.component.html'
})
export class TripDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SearchApiService);
  private readonly location = inject(Location);

  readonly trip = signal<TripDetailDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(true);

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
}
