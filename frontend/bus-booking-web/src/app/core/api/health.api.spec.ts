import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { HealthApiService, HealthResponse } from './health.api';
import { environment } from '../../../environments/environment';

describe('HealthApiService', () => {
  let service: HealthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        HealthApiService
      ]
    });
    service = TestBed.inject(HealthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the health endpoint and returns the payload', () => {
    const expected: HealthResponse = {
      status: 'ok',
      service: 'bus-booking-api',
      version: '0.1.0',
      timestampUtc: '2026-04-22T10:00:00.000Z'
    };

    let received: HealthResponse | undefined;
    service.ping().subscribe((r) => (received = r));

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/health`);
    expect(req.request.method).toBe('GET');
    req.flush(expected);

    expect(received).toEqual(expected);
  });
});
