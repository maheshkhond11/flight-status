import { TestBed } from '@angular/core/testing';

import { FlightStatusApi } from './flight-status-api.service';

describe('FlightStatusApi', () => {
  let service: FlightStatusApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FlightStatusApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
