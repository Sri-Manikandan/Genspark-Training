import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTableModule } from '@angular/material/table';
import { BusDto, OperatorBusesApiService } from '../../../core/api/operator-buses.api';
import { OperatorBusFormComponent } from './operator-bus-form.component';

@Component({
  selector: 'app-operator-buses-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, MatButtonModule, MatIconModule, MatMenuModule, MatChipsModule],
  templateUrl: './operator-buses-list.component.html'
})
export class OperatorBusesListComponent implements OnInit {
  private readonly api = inject(OperatorBusesApiService);
  private readonly dialog = inject(MatDialog);

  readonly buses = signal<BusDto[]>([]);
  readonly displayedColumns = ['registration', 'type', 'capacity', 'approval', 'operational', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.buses.set(list));
  }

  openAddForm(): void {
    this.dialog.open(OperatorBusFormComponent, { width: '480px' })
      .afterClosed().subscribe(result => { if (result) this.load(); });
  }

  retire(id: string): void {
    this.api.retire(id).subscribe(() => this.load());
  }

  updateStatus(id: string, status: 'active' | 'under_maintenance'): void {
    this.api.updateStatus(id, { operationalStatus: status }).subscribe(() => this.load());
  }
}
