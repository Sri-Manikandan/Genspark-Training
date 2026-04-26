import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatButtonModule, MatCardModule, MatIconModule, MatFormFieldModule, MatInputModule,
    DatePipe, FormsModule, CityAutocompleteComponent, MatDatepickerModule
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private readonly router = inject(Router);

  readonly source = signal<CityDto | null>(null);
  readonly destination = signal<CityDto | null>(null);
  readonly travelDate = signal<Date | null>(null);
  readonly today = new Date();
  readonly maxDate = new Date(this.today.getTime() + 60 * 24 * 60 * 60 * 1000);

  canSearch(): boolean {
    return !!this.source() && !!this.destination() && !!this.travelDate() &&
           this.source()!.id !== this.destination()!.id;
  }

  search(): void {
    if (!this.canSearch()) return;
    const d = this.travelDate()!;
    const dateStr = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    this.router.navigate(['/search-results'], {
      queryParams: { src: this.source()!.id, dst: this.destination()!.id, date: dateStr }
    });
  }
}
