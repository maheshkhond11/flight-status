import { ChangeDetectorRef, Component, NgZone, inject } from '@angular/core';
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

  result: FlightStatusResult | null = null;
  errorMessage: string | null = null;
  hasSearched = false;
  isLoading = false;

  lookup(lookup: FlightLookup): void {
    this.isLoading = true;
    this.hasSearched = true;
    this.errorMessage = null;
    this.result = null;

    this.flightStatusApi.getStatus(lookup)
      .pipe(finalize(() => {
        this.ngZone.run(() => {
          this.isLoading = false;
          this.changeDetector.detectChanges();
        });
      }))
      .subscribe({
        next: (result) => {
          this.result = result;
        },
        error: (error) => {
          console.error('Flight lookup error', error);
          this.errorMessage = 'We could not retrieve the flight status. Please try again.';
        },
      });
  }
}
