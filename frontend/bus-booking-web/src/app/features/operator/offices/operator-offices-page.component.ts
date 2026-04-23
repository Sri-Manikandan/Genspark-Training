import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { OperatorOfficesApiService } from '../../../core/api/operator-offices.api';
import { AddOfficeDialogComponent } from './add-office-dialog.component';

@Component({
  selector: 'app-operator-offices-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatTableModule, MatButtonModule, MatIconModule],
  templateUrl: './operator-offices-page.component.html'
})
export class OperatorOfficesPageComponent implements OnInit {
  private readonly api = inject(OperatorOfficesApiService);
  private readonly dialog = inject(MatDialog);

  readonly offices = signal<any[]>([]);
  readonly displayedColumns = ['city', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.list().subscribe(list => this.offices.set(list));
  }

  openAddDialog(): void {
    this.dialog.open(AddOfficeDialogComponent, { width: '400px' })
      .afterClosed().subscribe(result => {
        if (result) this.load();
      });
  }

  delete(id: string): void {
    this.api.delete(id).subscribe(() => this.load());
  }
}
