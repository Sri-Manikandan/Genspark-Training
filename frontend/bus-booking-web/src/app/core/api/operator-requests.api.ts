import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorRequestDto {
  id: string;
  userId: string;
  userEmail: string;
  userName: string;
  companyName: string;
  status: 'pending' | 'approved' | 'rejected';
  requestedAt: string;
  reviewedAt: string | null;
  reviewedByAdminId: string | null;
  rejectReason: string | null;
}

export interface BecomeOperatorRequest {
  companyName: string;
}

export interface RejectOperatorRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class OperatorRequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  submit(body: BecomeOperatorRequest): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(`${this.base}/me/become-operator`, body);
  }

  list(status?: 'pending' | 'approved' | 'rejected'): Observable<OperatorRequestDto[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<OperatorRequestDto[]>(`${this.base}/admin/operator-requests`, { params });
  }

  approve(id: string): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(
      `${this.base}/admin/operator-requests/${id}/approve`, {});
  }

  reject(id: string, body: RejectOperatorRequest): Observable<OperatorRequestDto> {
    return this.http.post<OperatorRequestDto>(
      `${this.base}/admin/operator-requests/${id}/reject`, body);
  }
}
