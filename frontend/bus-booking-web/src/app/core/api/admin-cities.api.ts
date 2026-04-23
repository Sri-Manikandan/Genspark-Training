import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CityDto } from './cities.api';

export interface CreateCityRequest { name: string; state: string; }
export interface UpdateCityRequest { name?: string; state?: string; isActive?: boolean; }

@Injectable({ providedIn: 'root' })
export class AdminCitiesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/cities`;

  list(): Observable<CityDto[]> { return this.http.get<CityDto[]>(this.base); }
  get(id: string): Observable<CityDto> { return this.http.get<CityDto>(`${this.base}/${id}`); }
  create(body: CreateCityRequest): Observable<CityDto> { return this.http.post<CityDto>(this.base, body); }
  update(id: string, body: UpdateCityRequest): Observable<CityDto> {
    return this.http.patch<CityDto>(`${this.base}/${id}`, body);
  }
}
