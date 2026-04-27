import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
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
  readonly selectable = input(false);
  readonly maxSelected = input(6);
  readonly fare = input<number | null>(null);
  readonly selectionChange = output<string[]>();

  private readonly selectedSet = signal<Set<string>>(new Set());
  readonly selected = computed(() => Array.from(this.selectedSet()).sort());

  readonly seatGrid = computed(() => {
    const l = this.layout();
    const grid: (SeatStatusDto | null)[][] = Array.from({ length: l.rows }, () =>
      Array(l.columns).fill(null)
    );
    for (const s of l.seats) grid[s.rowIndex][s.columnIndex] = s;
    return grid;
  });

  toggle(seat: SeatStatusDto): void {
    if (!this.selectable()) return;
    if (seat.status !== 'available') return;

    const next = new Set(this.selectedSet());
    if (next.has(seat.seatNumber)) {
      next.delete(seat.seatNumber);
    } else {
      if (next.size >= this.maxSelected()) return;
      next.add(seat.seatNumber);
    }

    this.selectedSet.set(next);
    this.selectionChange.emit(this.selected());
  }

  isSelected(seat: string): boolean {
    return this.selectedSet().has(seat);
  }
}
