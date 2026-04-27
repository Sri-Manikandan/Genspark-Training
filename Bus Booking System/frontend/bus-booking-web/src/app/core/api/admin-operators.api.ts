import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminOperatorListItemDto {
  userId: string;
  name: string;
  email: string;
  createdAt: string;
  isDisabled: boolean;
  disabledAt: string | null;
  totalBuses: number;
  activeBuses: number;
  retiredBuses: number;
}

export interface DisableOperatorRequest {
  reason?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminOperatorsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/operators`;

  list(): Observable<AdminOperatorListItemDto[]> {
    return this.http.get<AdminOperatorListItemDto[]>(this.base);
  }

  disable(id: string, body: DisableOperatorRequest): Observable<AdminOperatorListItemDto> {
    return this.http.post<AdminOperatorListItemDto>(`${this.base}/${id}/disable`, body);
  }

  enable(id: string): Observable<AdminOperatorListItemDto> {
    return this.http.post<AdminOperatorListItemDto>(`${this.base}/${id}/enable`, {});
  }
}
