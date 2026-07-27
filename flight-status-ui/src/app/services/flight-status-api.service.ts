import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { FlightLookup, FlightStatusResult } from '../models/flight-status-result.model';

@Injectable({ providedIn: 'root' })
export class FlightStatusApiService {
  private readonly http = inject(HttpClient);

  getStatus(lookup: FlightLookup): Observable<FlightStatusResult> {
    const params = new HttpParams()
      .set('flightNumber', lookup.flightNumber)
      .set('date', lookup.date);

    // Relative URL: works with the dev proxy (ng serve -> proxy.conf.json) and in
    // production, where this API serves the compiled Angular build from the same origin.
    return this.http.get<FlightStatusResult>('/flights/status', { params });
  }
}
