import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';
import { OperatorOfficesApiService } from '../../../core/api/operator-offices.api';

@Component({
  selector: 'app-add-office-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    CityAutocompleteComponent
  ],
  template: `
    <h2 mat-dialog-title>Add office</h2>
    <mat-dialog-content>
      <div class="flex flex-col gap-3 pt-2">
        <app-city-autocomplete label="City" (citySelected)="city.set($event)" />

        <mat-form-field appearance="outline">
          <mat-label>Address Line</mat-label>
          <input matInput [(ngModel)]="addressLine" placeholder="e.g. 123 Main St" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Phone</mat-label>
          <input matInput [(ngModel)]="phone" placeholder="e.g. +91 9876543210" />
        </mat-form-field>
      </div>

      @if (error()) {
        <p class="text-rose-600 text-sm mt-2">{{ error() }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="!city() || !addressLine || !phone || saving()" (click)="save()">
        @if (saving()) { <mat-spinner diameter="16"></mat-spinner> }
        Add
      </button>
    </mat-dialog-actions>
  `
})
export class AddOfficeDialogComponent {
  private readonly api = inject(OperatorOfficesApiService);
  private readonly dialogRef = inject(MatDialogRef<AddOfficeDialogComponent>);

  readonly city = signal<CityDto | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  addressLine = '';
  phone = '';

  save(): void {
    const c = this.city();
    if (!c || this.saving() || !this.addressLine || !this.phone) return;
    this.saving.set(true);
    this.error.set(null);
    this.api.create({ cityId: c.id, addressLine: this.addressLine, phone: this.phone }).subscribe({
      next: (office) => this.dialogRef.close(office),
      error: (err) => {
        this.saving.set(false);
        const code = err?.error?.error?.code;
        this.error.set(code === 'OFFICE_ALREADY_EXISTS'
          ? 'An office already exists in this city.'
          : 'Failed to add office. Please try again.');
      }
    });
  }
}
