import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminBookingListItemDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busId: string;
  busName: string;
  operatorUserId: string;
  operatorName: string;
  customerName: string;
  customerEmail: string;
  seatCount: number;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  status: string;
  createdAt: string;
}

export interface AdminBookingListResponseDto {
  items: AdminBookingListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AdminBookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/bookings`;

  list(
    operatorUserId?: string,
    status?: string,
    date?: string,
    page = 1,
    pageSize = 20
  ): Observable<AdminBookingListResponseDto> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (operatorUserId) params = params.set('operatorUserId', operatorUserId);
    if (status) params = params.set('status', status);
    if (date) params = params.set('date', date);
    return this.http.get<AdminBookingListResponseDto>(this.base, { params });
  }
}
