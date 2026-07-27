import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { FlightStatusApiService } from './flight-status-api.service';
import { FlightStatusResult } from '../models/flight-status-result.model';

describe('FlightStatusApiService', () => {
  let service: FlightStatusApiService;
  let httpMock: HttpTestingController;

  const result: FlightStatusResult = {
    flightNumber: 'SR100',
    date: '2026-07-21',
    status: 'OnTime',
    scheduledDepartureUtc: '2026-07-21T10:00:00+00:00',
    actualDepartureUtc: '2026-07-21T10:10:00+00:00',
    scheduledArrivalUtc: null,
    actualArrivalUtc: null,
    terminal: '1',
    gate: 'A12',
    delayReason: null,
    lastUpdatedUtc: '2026-07-21T09:30:00+00:00',
    message: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FlightStatusApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('calls the relative /flights/status endpoint with the lookup as query params', () => {
    service.getStatus({ flightNumber: 'SR100', date: '2026-07-21' }).subscribe();

    const request = httpMock.expectOne(
      (req) => req.url === '/flights/status' && req.params.get('flightNumber') === 'SR100' && req.params.get('date') === '2026-07-21',
    );

    expect(request.request.method).toBe('GET');
    request.flush(result);
  });

  it('propagates a usable result to the caller', () => {
    let received: FlightStatusResult | undefined;
    service.getStatus({ flightNumber: 'SR100', date: '2026-07-21' }).subscribe((value) => (received = value));

    const request = httpMock.expectOne((req) => req.url === '/flights/status');
    request.flush(result);

    expect(received).toEqual(result);
  });

  it('propagates an API error to the caller for the error state to handle', () => {
    let receivedError: unknown;
    service.getStatus({ flightNumber: 'SR100', date: '2026-07-21' }).subscribe({
      error: (error) => (receivedError = error),
    });

    const request = httpMock.expectOne((req) => req.url === '/flights/status');
    request.flush({ error: 'boom' }, { status: 500, statusText: 'Internal Server Error' });

    expect(receivedError).toBeTruthy();
  });
});
