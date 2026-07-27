import { Component, inject, signal } from '@angular/core';
import { delay, finalize } from 'rxjs';
import { FlightResultCardComponent } from './components/flight-result-card/flight-result-card.component';
import { FlightSearchComponent } from './components/flight-search/flight-search.component';
import { FlightLookup, FlightStatusResult } from './models/flight-status-result.model';
import { FlightStatusApiService } from './services/flight-status-api.service';

@Component({
  selector: 'app-root',
  imports: [FlightSearchComponent, FlightResultCardComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class App {
  /**
   * The stub API resolves effectively instantly, so without this the loading
   * spinner would flash on and off faster than it can visibly rotate. Holding
   * every response back (success or error) by a fixed amount keeps the loading
   * state on screen long enough to read - this is a UI/demo affordance, not a
   * simulation of real network latency.
   */
  private static readonly MinimumSpinnerDurationMs = 1500;

  private readonly flightStatusApi = inject(FlightStatusApiService);

  readonly result = signal<FlightStatusResult | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly hasSearched = signal(false);
  readonly isLoading = signal(false);

  lookup(lookup: FlightLookup): void {
    this.isLoading.set(true);
    this.hasSearched.set(true);
    this.errorMessage.set(null);
    this.result.set(null);

    this.flightStatusApi.getStatus(lookup)
      .pipe(
        delay(App.MinimumSpinnerDurationMs),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (result) => {
          this.result.set(result);
        },
        error: (error) => {
          console.error('Flight lookup error', error);
          this.errorMessage.set('We could not retrieve the flight status. Please try again.');
        },
      });
  }
}
