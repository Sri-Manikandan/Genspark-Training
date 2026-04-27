import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { roleGuard } from './role.guard';
import { AuthStore } from './auth.store';
import { CurrentUser } from './current-user';

describe('roleGuard', () => {
  let auth: AuthStore;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
    auth = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
  });

  function runGuard(allowed: string[]): boolean | UrlTree {
    const guard = roleGuard(allowed);
    return TestBed.runInInjectionContext(() => {
      return guard({} as never, []) as boolean | UrlTree;
    });
  }

  it('redirects to /login when the user is not logged in', () => {
    auth.user.set(null);

    const result = runGuard(['operator']);
    expect(result instanceof UrlTree).toBeTrue();
    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });

  it('redirects to / when logged in but without an allowed role', () => {
    const user: CurrentUser = {
      id: 'u1', name: 'C1', email: 'c@x', phone: null, roles: ['customer']
    };
    auth.user.set(user);

    const result = runGuard(['operator']);
    expect(result instanceof UrlTree).toBeTrue();
    expect(router.serializeUrl(result as UrlTree)).toBe('/');
  });

  it('returns true when the user has at least one allowed role', () => {
    const user: CurrentUser = {
      id: 'u1', name: 'C1', email: 'c@x', phone: null, roles: ['customer', 'operator']
    };
    auth.user.set(user);

    const result = runGuard(['operator', 'admin']);
    expect(result).toBeTrue();
  });

  it('matches on any of several allowed roles', () => {
    const user: CurrentUser = {
      id: 'u1', name: 'A1', email: 'a@x', phone: null, roles: ['admin']
    };
    auth.user.set(user);

    const result = runGuard(['operator', 'admin']);
    expect(result).toBeTrue();
  });
});
