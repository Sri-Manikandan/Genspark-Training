import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { AdminBusesApiService } from '../../../core/api/admin-buses.api';
import { BusDto } from '../../../core/api/operator-buses.api';
import { RejectDialogComponent } from '../operator-requests/reject-dialog.component';

@Component({
  selector: 'app-admin-bus-approvals-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, MatButtonModule, MatIconModule, MatChipsModule],
  templateUrl: './admin-bus-approvals-page.component.html'
})
export class AdminBusApprovalsPageComponent implements OnInit {
  private readonly api = inject(AdminBusesApiService);
  private readonly dialog = inject(MatDialog);

  readonly buses = signal<BusDto[]>([]);
  readonly displayedColumns = ['registration', 'operator', 'type', 'seats', 'approval', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.buses.set(list));
  }

  approve(id: string): void {
    this.api.approve(id).subscribe(() => this.load());
  }

  reject(id: string): void {
    this.dialog.open(RejectDialogComponent, { width: '420px' })
      .afterClosed().subscribe((reason: string | undefined) => {
        if (reason) {
          this.api.reject(id, { reason }).subscribe(() => this.load());
        }
      });
  }
}
