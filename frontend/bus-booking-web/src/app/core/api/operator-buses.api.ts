import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type BusType = 'seater' | 'sleeper' | 'semi_sleeper';
export type BusApprovalStatus = 'pending' | 'approved' | 'rejected';
export type BusOperationalStatus = 'active' | 'under_maintenance' | 'retired';

export interface BusDto {
  id: string;
  operatorUserId: string;
  registrationNumber: string;
  busName: string;
  busType: BusType;
  capacity: number;
  approvalStatus: BusApprovalStatus;
  operationalStatus: BusOperationalStatus;
  createdAt: string;
  approvedAt: string | null;
  rejectReason: string | null;
}

export interface CreateBusRequest {
  registrationNumber: string;
  busName: string;
  busType: BusType;
  rows: number;
  columns: number;
}

export interface UpdateBusStatusRequest {
  operationalStatus: 'active' | 'under_maintenance';
}

@Injectable({ providedIn: 'root' })
export class OperatorBusesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/buses`;

  list(): Observable<BusDto[]> { return this.http.get<BusDto[]>(this.base); }
  create(body: CreateBusRequest): Observable<BusDto> { return this.http.post<BusDto>(this.base, body); }
  updateStatus(id: string, body: UpdateBusStatusRequest): Observable<BusDto> {
    return this.http.patch<BusDto>(`${this.base}/${id}/status`, body);
  }
  retire(id: string): Observable<BusDto> { return this.http.delete<BusDto>(`${this.base}/${id}`); }
}
