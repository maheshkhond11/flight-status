import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FlightResultCard } from './flight-result-card.component';

describe('FlightResultCard', () => {
  let component: FlightResultCard;
  let fixture: ComponentFixture<FlightResultCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FlightResultCard],
    }).compileComponents();

    fixture = TestBed.createComponent(FlightResultCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
