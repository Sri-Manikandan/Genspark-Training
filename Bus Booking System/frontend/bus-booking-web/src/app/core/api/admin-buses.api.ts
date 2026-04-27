import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BusApprovalStatus, BusDto } from './operator-buses.api';

export interface RejectBusRequest { reason: string; }

@Injectable({ providedIn: 'root' })
export class AdminBusesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/buses`;

  list(status?: BusApprovalStatus): Observable<BusDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<BusDto[]>(this.base, { params });
  }

  approve(id: string): Observable<BusDto> {
    return this.http.post<BusDto>(`${this.base}/${id}/approve`, {});
  }

  reject(id: string, body: RejectBusRequest): Observable<BusDto> {
    return this.http.post<BusDto>(`${this.base}/${id}/reject`, body);
  }
}
