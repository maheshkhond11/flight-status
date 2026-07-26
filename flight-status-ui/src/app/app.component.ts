import { ChangeDetectorRef, Component, NgZone, inject, signal } from '@angular/core';
import { finalize } from 'rxjs';
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
  private readonly flightStatusApi = inject(FlightStatusApiService);
  private readonly ngZone = inject(NgZone);
  private readonly changeDetector = inject(ChangeDetectorRef);

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
      .pipe(finalize(() => {
        this.ngZone.run(() => {
          this.isLoading.set(false);
          this.changeDetector.detectChanges();
        });
      }))
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
