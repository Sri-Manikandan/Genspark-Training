import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type PlatformFeeType = 'fixed' | 'percent';

export interface PlatformFeeDto {
  feeType: PlatformFeeType;
  value: number;
  effectiveFrom: string;
}
export interface UpdatePlatformFeeRequest {
  feeType: PlatformFeeType;
  value: number;
}

@Injectable({ providedIn: 'root' })
export class AdminPlatformFeeApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin/platform-fee`;

  get(): Observable<PlatformFeeDto> { return this.http.get<PlatformFeeDto>(this.base); }
  update(body: UpdatePlatformFeeRequest): Observable<PlatformFeeDto> {
    return this.http.put<PlatformFeeDto>(this.base, body);
  }
}
