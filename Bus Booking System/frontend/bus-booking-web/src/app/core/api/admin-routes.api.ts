import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CityDto } from './cities.api';

export interface RouteDto {
  id: string;
  source: CityDto;
  destination: CityDto;
  distanceKm: number | null;
  isActive: boolean;
}
export interface CreateRouteRequest {
  sourceCityId: string;
  destinationCityId: string;
  distanceKm?: number | null;
}
export interface UpdateRouteRequest { distanceKm?: number | null; isActive?: boolean; }

@Injectable({ providedIn: 'root' })
export class AdminRoutesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/routes`;

  list(): Observable<RouteDto[]> { return this.http.get<RouteDto[]>(this.base); }
  create(body: CreateRouteRequest): Observable<RouteDto> { return this.http.post<RouteDto>(this.base, body); }
  update(id: string, body: UpdateRouteRequest): Observable<RouteDto> {
    return this.http.patch<RouteDto>(`${this.base}/${id}`, body);
  }
}
