import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { HealthApiService, HealthResponse } from '../../../core/api/health.api';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

type Status = 'loading' | 'ok' | 'failed';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatButtonModule, MatCardModule, MatIconModule, MatFormFieldModule, MatInputModule,
    DatePipe, CityAutocompleteComponent, MatDatepickerModule
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly api = inject(HealthApiService);
  private readonly router = inject(Router);

  readonly status = signal<Status>('loading');
  readonly payload = signal<HealthResponse | null>(null);
  readonly source = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
  readonly travelDate = signal<Date | null>(null);
  readonly today = new Date();
  readonly maxDate = new Date(this.today.getTime() + 60 * 24 * 60 * 60 * 1000); // +60 days

  readonly statusLabel = computed(() => {
    const s = this.status();
    if (s === 'loading') return 'checking…';
    if (s === 'ok') return 'backend online';
    return 'backend unreachable';
  });

  canSearch(): boolean {
    return !!this.source() && !!this.destination() && !!this.travelDate() &&
           this.source()!.id !== this.destination()!.id;
  }

  search(): void {
    if (!this.canSearch()) return;
    const dateStr = this.travelDate()!.toISOString().split('T')[0];
    this.router.navigate(['/search-results'], {
      queryParams: { src: this.source()!.id, dst: this.destination()!.id, date: dateStr }
    });
  }

  ngOnInit(): void {
    this.ping();
  }

  ping(): void {
    this.status.set('loading');
    this.api.ping().subscribe({
      next: (r) => {
        this.payload.set(r);
        this.status.set('ok');
      },
      error: () => {
        this.payload.set(null);
        this.status.set('failed');
      }
    });
  }
}
