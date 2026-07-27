import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightResultCardComponent } from './flight-result-card.component';
import { FlightStatusResult } from '../../models/flight-status-result.model';

describe('FlightResultCardComponent', () => {
  let component: FlightResultCardComponent;
  let fixture: ComponentFixture<FlightResultCardComponent>;

  const baseResult: FlightStatusResult = {
    flightNumber: 'SR100',
    date: '2026-07-21',
    status: 'OnTime',
    scheduledDepartureUtc: '2026-07-21T10:00:00+00:00',
    actualDepartureUtc: '2026-07-21T10:10:00+00:00',
    scheduledArrivalUtc: null,
    actualArrivalUtc: null,
    terminal: null,
    gate: null,
    delayReason: null,
    lastUpdatedUtc: '2026-07-21T09:30:00+00:00',
    message: null,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FlightResultCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FlightResultCardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('status colour mapping', () => {
    const cases: Array<[FlightStatusResult['status'], string]> = [
      ['OnTime', 'status--ontime'],
      ['Delayed', 'status--delayed'],
      ['Cancelled', 'status--cancelled'],
      ['Diverted', 'status--diverted'],
      ['Unknown', 'status--unknown'],
    ];

    for (const [status, expectedClass] of cases) {
      it(`maps ${status} to ${expectedClass}`, () => {
        fixture.componentRef.setInput('result', { ...baseResult, status });
        fixture.detectChanges();

        const article = fixture.nativeElement.querySelector('article.result-card') as HTMLElement;
        expect(article.classList.contains(expectedClass)).toBe(true);
      });
    }
  });

  describe('conditional AeroTrack-only fields', () => {
    it('does not render terminal, gate, or delay reason when absent', () => {
      fixture.componentRef.setInput('result', baseResult);
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).not.toContain('Terminal:');
      expect(text).not.toContain('Gate:');
      expect(text).not.toContain('Delay reason:');
    });

    it('renders terminal, gate, and delay reason when supplied', () => {
      fixture.componentRef.setInput('result', {
        ...baseResult,
        status: 'Delayed',
        terminal: '1',
        gate: 'A12',
        delayReason: 'Weather disruption',
      });
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Terminal:');
      expect(text).toContain('1');
      expect(text).toContain('Gate:');
      expect(text).toContain('A12');
      expect(text).toContain('Delay reason:');
      expect(text).toContain('Weather disruption');
    });

    it('renders only the fields that are present when partially supplied', () => {
      fixture.componentRef.setInput('result', { ...baseResult, gate: 'B04' });
      fixture.detectChanges();

      const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
      expect(text).toContain('Gate:');
      expect(text).not.toContain('Terminal:');
      expect(text).not.toContain('Delay reason:');
    });
  });

  it('renders the Unknown message when no provider returned a usable status', () => {
    fixture.componentRef.setInput('result', {
      ...baseResult,
      status: 'Unknown',
      scheduledDepartureUtc: null,
      actualDepartureUtc: null,
      lastUpdatedUtc: null,
      message: 'No usable status was returned by either provider.',
    });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No usable status was returned by either provider.');
  });
});
