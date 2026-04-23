import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import {
  AdminOperatorsApiService, AdminOperatorListItemDto
} from '../../../core/api/admin-operators.api';
import {
  DisableOperatorDialogComponent, DisableOperatorDialogData
} from './disable-operator-dialog.component';

@Component({
  selector: 'app-admin-operators-page',
  standalone: true,
  imports: [
    CommonModule, MatTableModule, MatButtonModule, MatIconModule,
    MatDialogModule, MatSnackBarModule
  ],
  templateUrl: './admin-operators-page.component.html'
})
export class AdminOperatorsPageComponent implements OnInit {
  private readonly api = inject(AdminOperatorsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly operators = signal<AdminOperatorListItemDto[]>([]);
  readonly busy = signal<string | null>(null);

  readonly columns = ['name', 'email', 'buses', 'state', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.operators.set(list));
  }

  disable(op: AdminOperatorListItemDto): void {
    const ref = this.dialog.open(DisableOperatorDialogComponent, {
      width: '480px',
      data: { operatorName: op.name } satisfies DisableOperatorDialogData
    });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.busy.set(op.userId);
      this.api.disable(op.userId, { reason: result.reason }).subscribe({
        next: updated => {
          this.operators.update(list =>
            list.map(o => o.userId === updated.userId ? updated : o));
          this.snack.open(`Disabled ${op.name}`, 'OK', { duration: 3500 });
          this.busy.set(null);
        },
        error: () => {
          this.snack.open(`Failed to disable ${op.name}`, 'Dismiss', { duration: 5000 });
          this.busy.set(null);
        }
      });
    });
  }

  enable(op: AdminOperatorListItemDto): void {
    this.busy.set(op.userId);
    this.api.enable(op.userId).subscribe({
      next: updated => {
        this.operators.update(list =>
          list.map(o => o.userId === updated.userId ? updated : o));
        this.snack.open(`Enabled ${op.name}`, 'OK', { duration: 3500 });
        this.busy.set(null);
      },
      error: () => {
        this.snack.open(`Failed to enable ${op.name}`, 'Dismiss', { duration: 5000 });
        this.busy.set(null);
      }
    });
  }
}
