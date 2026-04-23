import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorRevenueItemDto {
  busId: string;
  busName: string;
  registrationNumber: string;
  confirmedBookings: number;
  totalSeats: number;
  totalFare: number;
}

export interface OperatorRevenueResponseDto {
  dateFrom: string;
  dateTo: string;
  grandTotalFare: number;
  byBus: OperatorRevenueItemDto[];
}

@Injectable({ providedIn: 'root' })
export class OperatorRevenueApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/revenue`;

  get(from?: string, to?: string): Observable<OperatorRevenueResponseDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<OperatorRevenueResponseDto>(this.base, { params });
  }
}
