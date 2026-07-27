import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { App } from './app.component';
import { FlightStatusApiService } from './services/flight-status-api.service';
import { FlightStatusResult } from './models/flight-status-result.model';

// Matches App's MinimumSpinnerDurationMs with headroom, so these tests don't
// race the delay that keeps the spinner visible for instant stub responses.
const SPINNER_DELAY_WAIT_MS = 1700;

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

const sampleResult: FlightStatusResult = {
  flightNumber: 'SR100',
  date: '2026-07-21',
  status: 'OnTime',
  scheduledDepartureUtc: null,
  actualDepartureUtc: null,
  scheduledArrivalUtc: null,
  actualArrivalUtc: null,
  terminal: null,
  gate: null,
  delayReason: null,
  lastUpdatedUtc: null,
  message: null,
};

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the initial empty state', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('h1')?.textContent).toContain('Flight status tracker');
    expect(compiled.textContent).toContain('Enter a flight number and date to begin.');
  });

  it('keeps the loading state visible for a minimum duration even when the API responds instantly', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    })
      .overrideProvider(FlightStatusApiService, {
        // `of(...)` emits synchronously, like the real stub API does in practice.
        useValue: { getStatus: () => of(sampleResult) },
      })
      .compileComponents();

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    fixture.componentInstance.lookup({ flightNumber: 'SR100', date: '2026-07-21' });

    // Immediately after an instant response, the spinner should still be showing -
    // this is the exact regression the minimum-spinner-duration delay guards against.
    expect(fixture.componentInstance.isLoading()).toBe(true);
    expect(fixture.componentInstance.result()).toBeNull();

    await wait(SPINNER_DELAY_WAIT_MS);
    fixture.detectChanges();

    expect(fixture.componentInstance.isLoading()).toBe(false);
    expect(fixture.componentInstance.result()).toEqual(sampleResult);
  });

  it('shows an error state and clears the empty state when the API call fails', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    })
      .overrideProvider(FlightStatusApiService, {
        useValue: { getStatus: () => throwError(() => new Error('network down')) },
      })
      .compileComponents();

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    fixture.componentInstance.lookup({ flightNumber: 'SR100', date: '2026-07-21' });

    await wait(SPINNER_DELAY_WAIT_MS);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="alert"]')?.textContent).toContain(
      'We could not retrieve the flight status. Please try again.',
    );
    expect(compiled.textContent).not.toContain('Enter a flight number and date to begin.');
    expect(fixture.componentInstance.isLoading()).toBe(false);
  });
});
