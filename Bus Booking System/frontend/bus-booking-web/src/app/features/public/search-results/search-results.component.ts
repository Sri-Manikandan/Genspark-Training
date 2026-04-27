import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SearchApiService, SearchResultDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './search-results.component.html',
  styleUrl: './search-results.component.scss'
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

  pad(n: number): string { return n.toString().padStart(2, '0'); }
}
