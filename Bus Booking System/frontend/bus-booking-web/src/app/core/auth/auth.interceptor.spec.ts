import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthTokenStore } from './auth-token.store';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let tokenStore: AuthTokenStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStore = TestBed.inject(AuthTokenStore);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('does not add Authorization header when no token is stored', () => {
    http.get('/api/v1/anything').subscribe();
    const req = httpMock.expectOne('/api/v1/anything');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('adds `Authorization: Bearer <token>` when a token is stored', () => {
    tokenStore.set('jwt-abc.def.ghi');

    http.get('/api/v1/me').subscribe();
    const req = httpMock.expectOne('/api/v1/me');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-abc.def.ghi');
    req.flush({});
  });

  it('stops sending the header after the token is cleared', () => {
    tokenStore.set('jwt-abc.def.ghi');
    tokenStore.clear();

    http.get('/api/v1/me').subscribe();
    const req = httpMock.expectOne('/api/v1/me');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });
});
