import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { FlightStatusResult } from '../../models/flight-status-result.model';

@Component({
  selector: 'app-flight-result-card',
  imports: [DatePipe],
  templateUrl: './flight-result-card.component.html',
  styleUrl: './flight-result-card.component.scss',
})
export class FlightResultCardComponent {
  readonly result = input<FlightStatusResult | null>(null);

  get statusClass(): string {
    const status = this.result()?.status ?? 'unknown';
    return `status--${status.toLowerCase()}`;
  }
}
