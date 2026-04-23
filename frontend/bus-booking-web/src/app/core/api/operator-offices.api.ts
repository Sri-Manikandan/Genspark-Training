import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface OperatorOfficeDto {
  id: string;
  cityId: string;
  cityName: string;
  addressLine: string;
  phone: string;
  isActive: boolean;
}

export interface CreateOperatorOfficeRequest {
  cityId: string;
  addressLine: string;
  phone: string;
}

@Injectable({ providedIn: 'root' })
export class OperatorOfficesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/operator/offices`;

  list(): Observable<OperatorOfficeDto[]> {
    return this.http.get<OperatorOfficeDto[]>(this.base);
  }

  create(body: CreateOperatorOfficeRequest): Observable<OperatorOfficeDto> {
    return this.http.post<OperatorOfficeDto>(this.base, body);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
