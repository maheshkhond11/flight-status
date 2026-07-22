export type FlightStatus = 'OnTime' | 'Delayed' | 'Cancelled' | 'Diverted' | 'Unknown';

export interface FlightLookup {
  flightNumber: string;
  date: string;
}

export interface FlightStatusResult {
  flightNumber: string;
  date: string;
  status: FlightStatus;
  scheduledDepartureUtc: string | null;
  actualDepartureUtc: string | null;
  scheduledArrivalUtc: string | null;
  actualArrivalUtc: string | null;
  terminal: string | null;
  gate: string | null;
  delayReason: string | null;
  lastUpdatedUtc: string | null;
  message: string | null;
}
