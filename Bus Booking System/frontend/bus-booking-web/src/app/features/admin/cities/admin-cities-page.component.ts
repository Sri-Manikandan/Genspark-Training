import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminCitiesApiService } from '../../../core/api/admin-cities.api';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-admin-cities-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatSlideToggleModule, MatTableModule
  ],
  templateUrl: './admin-cities-page.component.html',
  styleUrl: './admin-cities-page.component.scss'
})
export class AdminCitiesPageComponent {
  private readonly api = inject(AdminCitiesApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly cities = signal<CityDto[]>([]);
  readonly saving = signal(false);
  readonly columns = ['name', 'state', 'active'];

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    state: ['', [Validators.required, Validators.maxLength(120)]]
  });

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.api.list().subscribe({
      next: list => this.cities.set(list),
      error: () => this.snack.open('Failed to load cities', 'Dismiss', { duration: 4000 })
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.api.create(this.form.getRawValue()).subscribe({
      next: created => {
        this.cities.set([...this.cities(), created].sort((a, b) => a.name.localeCompare(b.name)));
        this.form.reset();
        this.saving.set(false);
      },
      error: err => {
        this.saving.set(false);
        this.snack.open(err?.error?.error?.message ?? 'Create failed', 'Dismiss', { duration: 4000 });
      }
    });
  }

  toggleActive(c: CityDto): void {
    this.api.update(c.id, { isActive: !c.isActive }).subscribe({
      next: updated => {
        this.cities.set(this.cities().map(x => (x.id === updated.id ? updated : x)));
      },
      error: () => this.snack.open('Update failed', 'Dismiss', { duration: 4000 })
    });
  }
}
