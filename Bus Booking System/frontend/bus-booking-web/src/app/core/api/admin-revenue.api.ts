import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminRevenueOperatorItemDto {
  operatorUserId: string;
  operatorName: string;
  confirmedBookings: number;
  gmv: number;
  platformFeeIncome: number;
}

export interface AdminRevenueResponseDto {
  dateFrom: string;
  dateTo: string;
  confirmedBookings: number;
  gmv: number;
  platformFeeIncome: number;
  byOperator: AdminRevenueOperatorItemDto[];
}

@Injectable({ providedIn: 'root' })
export class AdminRevenueApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/revenue`;

  get(from?: string, to?: string): Observable<AdminRevenueResponseDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<AdminRevenueResponseDto>(this.base, { params });
  }
}
