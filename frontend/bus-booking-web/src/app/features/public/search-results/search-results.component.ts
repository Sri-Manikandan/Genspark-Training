import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { SearchApiService, SearchResultDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule],
  templateUrl: './search-results.component.html'
})
export class SearchResultsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(SearchApiService);

  readonly results = signal<SearchResultDto[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const src = params['src'];
      const dst = params['dst'];
      const date = params['date'];
      if (src && dst && date) {
        this.loading.set(true);
        this.api.search(src, dst, date).subscribe({
          next: (data) => {
            this.results.set(data);
            this.loading.set(false);
          },
          error: () => this.loading.set(false)
        });
      } else {
        this.loading.set(false);
      }
    });
  }

  formatTimeRange(start: string, end: string): string {
    return `${start.substring(0,5)} → ${end.substring(0,5)}`;
  }
}
