import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { OperatorOfficesApiService } from '../../../core/api/operator-offices.api';
import { OperatorBusesApiService } from '../../../core/api/operator-buses.api';

@Component({
  selector: 'app-operator-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule],
  templateUrl: './operator-dashboard.component.html'
})
export class OperatorDashboardComponent implements OnInit {
  private readonly officesApi = inject(OperatorOfficesApiService);
  private readonly busesApi = inject(OperatorBusesApiService);

  readonly officeCount = signal<number | null>(null);
  readonly busCount = signal<number | null>(null);

  ngOnInit(): void {
    forkJoin({
      offices: this.officesApi.list(),
      buses: this.busesApi.list()
    }).subscribe({
      next: ({ offices, buses }) => {
        this.officeCount.set(offices.length);
        this.busCount.set(buses.length);
      }
    });
  }
}
