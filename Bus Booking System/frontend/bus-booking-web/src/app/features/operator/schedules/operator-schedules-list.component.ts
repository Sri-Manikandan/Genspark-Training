import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { OperatorSchedulesApiService, BusScheduleDto } from '../../../core/api/schedules.api';
import { OperatorScheduleFormComponent } from './operator-schedule-form.component';

@Component({
  selector: 'app-operator-schedules-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule],
  templateUrl: './operator-schedules-list.component.html'
})
export class OperatorSchedulesListComponent implements OnInit {
  private readonly api = inject(OperatorSchedulesApiService);
  private readonly dialog = inject(MatDialog);

  readonly schedules = signal<BusScheduleDto[]>([]);
  readonly displayedColumns = [
    'busName', 'route', 'time', 'fare', 'validity', 'status', 'actions'
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(data => this.schedules.set(data));
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(OperatorScheduleFormComponent, { width: '500px' });
    ref.afterClosed().subscribe(res => {
      if (res) this.load();
    });
  }

  toggleStatus(schedule: BusScheduleDto): void {
    this.api.update(schedule.id, { isActive: !schedule.isActive }).subscribe(() => this.load());
  }

  delete(id: string): void {
    if (confirm('Delete this schedule?')) {
      this.api.delete(id).subscribe(() => this.load());
    }
  }

  formatDays(mask: number): string {
    if (mask === 127) return 'Daily';
    const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return days.filter((_, i) => (mask & (1 << i)) !== 0).join(', ');
  }
}
