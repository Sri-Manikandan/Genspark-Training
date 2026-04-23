import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SeatLayoutDto, SeatStatusDto } from '../../../core/api/search.api';

@Component({
  selector: 'app-seat-map',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seat-map.component.html',
  styleUrl: './seat-map.component.scss'
})
export class SeatMapComponent {
  readonly layout = input.required<SeatLayoutDto>();

  // Helper to create a 2D array of seats for rendering
  get seatGrid(): (SeatStatusDto | null)[][] {
    const l = this.layout();
    const grid: (SeatStatusDto | null)[][] = Array.from({ length: l.rows }, () =>
      Array(l.columns).fill(null)
    );

    for (const seat of l.seats) {
      grid[seat.rowIndex][seat.columnIndex] = seat;
    }
    return grid;
  }
}
