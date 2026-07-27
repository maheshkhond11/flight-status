import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightSearchComponent } from './flight-search.component';
import { FlightLookup } from '../../models/flight-status-result.model';

describe('FlightSearchComponent', () => {
  let component: FlightSearchComponent;
  let fixture: ComponentFixture<FlightSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FlightSearchComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FlightSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not emit search when the form is invalid', () => {
    const emitted: FlightLookup[] = [];
    component.search.subscribe((lookup) => emitted.push(lookup));

    component.submit();

    expect(emitted.length).toBe(0);
    expect(component.form.controls.flightNumber.touched).toBe(true);
    expect(component.form.controls.date.touched).toBe(true);
  });

  it('emits a normalised lookup (trimmed, uppercased flight number) when the form is valid', () => {
    const emitted: FlightLookup[] = [];
    component.search.subscribe((lookup) => emitted.push(lookup));

    component.form.controls.flightNumber.setValue('  sr100  ');
    component.form.controls.date.setValue('2026-07-21');
    component.submit();

    expect(emitted).toEqual([{ flightNumber: 'SR100', date: '2026-07-21' }]);
  });

  it('shows a validation message once the flight number field is touched and invalid', () => {
    component.form.controls.flightNumber.markAsTouched();
    component.form.controls.flightNumber.setValue('');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Enter a flight number.');
  });

  describe('loading state', () => {
    it('disables the submit button while loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
      expect(button.disabled).toBe(true);
    });

    it('shows a visible spinner while loading', () => {
      fixture.componentRef.setInput('loading', true);
      fixture.detectChanges();

      const spinner = fixture.nativeElement.querySelector('button .spinner');
      expect(spinner).toBeTruthy();
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('Searching');
    });

    it('does not show a spinner and re-enables the button once loading finishes', () => {
      fixture.componentRef.setInput('loading', false);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
      const spinner = fixture.nativeElement.querySelector('button .spinner');

      expect(button.disabled).toBe(false);
      expect(spinner).toBeFalsy();
      expect(button.textContent).toContain('Check status');
    });
  });
});
