import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApiService, LoginRequest, LoginResponse, RegisterRequest } from '../api/auth.api';
import { CurrentUser } from './current-user';
import { AuthTokenStore } from './auth-token.store';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApiService);
  private readonly tokenStore = inject(AuthTokenStore);

  readonly user = signal<CurrentUser | null>(null);
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly roles = computed(() => this.user()?.roles ?? []);

  register(body: RegisterRequest): Observable<CurrentUser> {
    return this.api.register(body);
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.api.login(body).pipe(
      tap(res => {
        this.tokenStore.set(res.token);
        this.user.set(res.user);
      })
    );
  }

  loadMe(): Observable<CurrentUser> {
    return this.api.me().pipe(tap(user => this.user.set(user)));
  }

  logout(): void {
    this.tokenStore.clear();
    this.user.set(null);
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }
}
