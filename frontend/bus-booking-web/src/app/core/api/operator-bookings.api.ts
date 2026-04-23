import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorBookingListItemDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busId: string;
  busName: string;
  customerName: string;
  seatCount: number;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  status: string;
  createdAt: string;
}

export interface OperatorBookingListResponseDto {
  items: OperatorBookingListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class OperatorBookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/bookings`;

  list(busId?: string, date?: string, page = 1, pageSize = 20): Observable<OperatorBookingListResponseDto> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (busId) params = params.set('busId', busId);
    if (date) params = params.set('date', date);
    return this.http.get<OperatorBookingListResponseDto>(this.base, { params });
  }
}
