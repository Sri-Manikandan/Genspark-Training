import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CityDto {
  id: string;
  name: string;
  state: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CitiesApiService {
  private readonly http = inject(HttpClient);

  search(query: string, limit = 10): Observable<CityDto[]> {
    const params = new HttpParams().set('q', query).set('limit', limit.toString());
    return this.http.get<CityDto[]>(`${environment.apiBaseUrl}/cities`, { params });
  }
}
