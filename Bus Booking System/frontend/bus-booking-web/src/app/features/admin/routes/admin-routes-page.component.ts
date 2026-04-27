import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { AdminRoutesApiService, RouteDto } from '../../../core/api/admin-routes.api';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-admin-routes-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatInputModule,
    MatSlideToggleModule, MatTableModule,
    CityAutocompleteComponent
  ],
  templateUrl: './admin-routes-page.component.html',
  styleUrl: './admin-routes-page.component.scss'
})
export class AdminRoutesPageComponent {
  private readonly api = inject(AdminRoutesApiService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);

  readonly routes = signal<RouteDto[]>([]);
  readonly source = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
  readonly columns = ['source', 'destination', 'distance', 'active'];

  readonly form = this.fb.nonNullable.group({
    distanceKm: [null as number | null,
      [Validators.min(1), Validators.max(5000)]]
  });

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.api.list().subscribe({
      next: list => this.routes.set(list),
      error: () => this.snack.open('Failed to load routes', 'Dismiss', { duration: 4000 })
    });
  }

  canSubmit(): boolean {
    const s = this.source(); const d = this.destination();
    return !!s && !!d && s.id !== d.id && this.form.valid;
  }

  submit(): void {
    const s = this.source(); const d = this.destination();
    if (!s || !d) return;
    this.api.create({
      sourceCityId: s.id,
      destinationCityId: d.id,
      distanceKm: this.form.getRawValue().distanceKm
    }).subscribe({
      next: created => {
        this.routes.set([created, ...this.routes()]);
        this.source.set(null);
        this.destination.set(null);
        this.form.reset();
      },
      error: err => this.snack.open(
        err?.error?.error?.message ?? 'Create failed', 'Dismiss', { duration: 4000 })
    });
  }

  toggleActive(r: RouteDto): void {
    this.api.update(r.id, { isActive: !r.isActive }).subscribe({
      next: updated => this.routes.set(this.routes().map(x => x.id === updated.id ? updated : x)),
      error: () => this.snack.open('Update failed', 'Dismiss', { duration: 4000 })
    });
  }
}
