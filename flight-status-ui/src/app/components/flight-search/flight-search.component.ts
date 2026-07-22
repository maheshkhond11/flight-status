import { Component, EventEmitter, input, output } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { FlightLookup } from '../../models/flight-status-result.model';

@Component({
  selector: 'app-flight-search',
  imports: [ReactiveFormsModule],
  templateUrl: './flight-search.component.html',
  styleUrl: './flight-search.component.scss',
})
export class FlightSearchComponent {
  readonly loading = input(false);
  readonly search = output<FlightLookup>();

  readonly form = new FormGroup({
    flightNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(10)] }),
    date: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.search.emit({
      flightNumber: this.form.controls.flightNumber.value.trim().toUpperCase(),
      date: this.form.controls.date.value,
    });
  }
}
