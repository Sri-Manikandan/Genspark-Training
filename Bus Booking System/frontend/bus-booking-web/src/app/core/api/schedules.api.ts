import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BusScheduleDto {
  id: string;
  busId: string;
  busName: string;
  routeId: string;
  sourceCityName: string;
  destinationCityName: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  validFrom: string;
  validTo: string;
  daysOfWeek: number;
  isActive: boolean;
}

export interface RouteOptionDto {
  id: string;
  sourceCityName: string;
  destinationCityName: string;
  distanceKm: number | null;
}

export interface CreateBusScheduleRequest {
  busId: string;
  routeId: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  validFrom: string;
  validTo: string;
  daysOfWeek: number;
}

export interface UpdateBusScheduleRequest {
  departureTime?: string;
  arrivalTime?: string;
  farePerSeat?: number;
  validFrom?: string;
  validTo?: string;
  daysOfWeek?: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class OperatorSchedulesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator`;

  list(busId?: string): Observable<BusScheduleDto[]> {
    let params = new HttpParams();
    if (busId) params = params.set('busId', busId);
    return this.http.get<BusScheduleDto[]>(`${this.base}/schedules`, { params });
  }

  create(body: CreateBusScheduleRequest): Observable<BusScheduleDto> {
    return this.http.post<BusScheduleDto>(`${this.base}/schedules`, body);
  }

  update(id: string, body: UpdateBusScheduleRequest): Observable<BusScheduleDto> {
    return this.http.patch<BusScheduleDto>(`${this.base}/schedules/${id}`, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/schedules/${id}`);
  }

  listRoutes(): Observable<RouteOptionDto[]> {
    return this.http.get<RouteOptionDto[]>(`${this.base}/routes`);
  }
}
