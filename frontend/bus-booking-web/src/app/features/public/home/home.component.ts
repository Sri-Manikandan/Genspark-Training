import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { DatePipe } from '@angular/common';
import { HealthApiService, HealthResponse } from '../../../core/api/health.api';

type Status = 'loading' | 'ok' | 'failed';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, DatePipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private readonly api = inject(HealthApiService);

  readonly status = signal<Status>('loading');
  readonly payload = signal<HealthResponse | null>(null);
  readonly statusLabel = computed(() => {
    const s = this.status();
    if (s === 'loading') return 'checking…';
    if (s === 'ok') return 'backend online';
    return 'backend unreachable';
  });

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
