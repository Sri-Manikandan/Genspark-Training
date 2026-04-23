import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { OperatorRequestsApiService, OperatorRequestDto } from '../../../core/api/operator-requests.api';
import { RejectDialogComponent } from './reject-dialog.component';

@Component({
  selector: 'app-admin-operator-requests-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, MatTableModule, MatButtonModule, MatIconModule, MatTabsModule],
  templateUrl: './admin-operator-requests-page.component.html'
})
export class AdminOperatorRequestsPageComponent implements OnInit {
  private readonly api = inject(OperatorRequestsApiService);
  private readonly dialog = inject(MatDialog);

  readonly requests = signal<OperatorRequestDto[]>([]);
  readonly displayedColumns = ['company', 'user', 'requestedAt', 'status', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.requests.set(list));
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
