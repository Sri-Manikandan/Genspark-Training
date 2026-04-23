import {
  ChangeDetectionStrategy, Component, EventEmitter, Output,
  input, signal
} from '@angular/core';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {
  MatAutocompleteModule, MatAutocompleteSelectedEvent
} from '@angular/material/autocomplete';
import { debounceTime, distinctUntilChanged, filter, switchMap } from 'rxjs';
import { CitiesApiService, CityDto } from '../../../core/api/cities.api';

@Component({
  selector: 'app-city-autocomplete',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatAutocompleteModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './city-autocomplete.component.html',
  styleUrl: './city-autocomplete.component.scss'
})
export class CityAutocompleteComponent {
  readonly label = input.required<string>();
  readonly placeholder = input<string>('Start typing a city…');

  @Output() readonly citySelected = new EventEmitter<CityDto>();

  readonly control = new FormControl<string>('', { nonNullable: true });
  readonly options = signal<CityDto[]>([]);
  readonly loading = signal(false);

  constructor(private readonly api: CitiesApiService) {
    this.control.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      filter(v => (v ?? '').trim().length >= 2),
      switchMap(v => {
        this.loading.set(true);
        return this.api.search(v!.trim());
      })
    ).subscribe({
      next: list => { this.options.set(list); this.loading.set(false); },
      error: () => { this.options.set([]); this.loading.set(false); }
    });

    this.control.valueChanges.pipe(
      filter(v => (v ?? '').trim().length < 2)
    ).subscribe(() => this.options.set([]));
  }

  displayFn = (c: CityDto | string | null): string =>
    typeof c === 'string' || c === null ? (c ?? '') : c.name;

  onSelect(city: CityDto): void {
    this.citySelected.emit(city);
  }

  onOptionPicked(event: MatAutocompleteSelectedEvent): void {
    this.onSelect(event.option.value as CityDto);
  }
}
