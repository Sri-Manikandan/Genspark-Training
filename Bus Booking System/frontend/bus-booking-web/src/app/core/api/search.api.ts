import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SearchResultDto {
  tripId: string;
  busName: string;
  busType: string;
  operatorName: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  seatsLeft: number;
  pickupAddress: string;
  dropAddress: string;
}

export interface SeatStatusDto {
  seatNumber: string;
  rowIndex: number;
  columnIndex: number;
  status: 'available' | 'locked' | 'booked';
}

export interface SeatLayoutDto {
  rows: number;
  columns: number;
  seats: SeatStatusDto[];
}

export interface TripDetailDto {
  tripId: string;
  busId: string;
  busName: string;
  busType: string;
  operatorName: string;
  tripDate: string;
  departureTime: string;
  arrivalTime: string;
  farePerSeat: number;
  seatsLeft: number;
  sourceCityName: string;
  destinationCityName: string;
  pickupAddress: string | null;
  dropAddress: string | null;
  seatLayout: SeatLayoutDto;
}

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  search(src: string, dst: string, date: string): Observable<SearchResultDto[]> {
    const params = new HttpParams().set('src', src).set('dst', dst).set('date', date);
    return this.http.get<SearchResultDto[]>(`${this.base}/search`, { params });
  }

  getTripDetail(tripId: string): Observable<TripDetailDto> {
    return this.http.get<TripDetailDto>(`${this.base}/trips/${tripId}`);
  }

  getSeatLayout(tripId: string): Observable<SeatLayoutDto> {
    return this.http.get<SeatLayoutDto>(`${this.base}/trips/${tripId}/seats`);
  }
}
