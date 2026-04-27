import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser } from '../auth/current-user';

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  phone: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  register(body: RegisterRequest): Observable<CurrentUser> {
    return this.http.post<CurrentUser>(`${environment.apiBaseUrl}/auth/register`, body);
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, body);
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${environment.apiBaseUrl}/me`);
  }
}
