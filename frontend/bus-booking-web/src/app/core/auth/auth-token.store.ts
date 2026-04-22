import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'bb.auth.token';

@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  readonly token = signal<string | null>(this.loadInitial());

  set(token: string | null): void {
    this.token.set(token);
    if (token) {
      localStorage.setItem(STORAGE_KEY, token);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  clear(): void {
    this.set(null);
  }

  private loadInitial(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
