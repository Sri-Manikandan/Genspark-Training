import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface LockSeatsRequest {
  sessionId: string;
  seats: string[];
}

export interface SeatLockResponseDto {
  lockId: string;
  sessionId: string;
  seats: string[];
  expiresAt: string;
}

export interface PassengerDto {
  seatNumber: string;
  passengerName: string;
  passengerAge: number;
  passengerGender: 'male' | 'female' | 'other';
}

export interface CreateBookingRequest {
  tripId: string;
  lockId: string;
  sessionId: string;
  passengers: PassengerDto[];
}

export interface CreateBookingResponseDto {
  bookingId: string;
  bookingCode: string;
  razorpayOrderId: string;
  keyId: string;
  amount: number;
  currency: string;
}

export interface VerifyPaymentRequest {
  razorpayPaymentId: string;
  razorpaySignature: string;
}

export interface BookingSeatDto {
  seatNumber: string;
  passengerName: string;
  passengerAge: number;
  passengerGender: string;
}

export interface BookingDetailDto {
  bookingId: string;
  bookingCode: string;
  tripId: string;
  tripDate: string;
  sourceCity: string;
  destinationCity: string;
  busName: string;
  operatorName: string;
  departureTime: string;
  arrivalTime: string;
  totalFare: number;
  platformFee: number;
  totalAmount: number;
  seatCount: number;
  status: string;
  confirmedAt: string | null;
  createdAt: string;
  seats: BookingSeatDto[];
}

@Injectable({ providedIn: 'root' })
export class BookingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  lockSeats(tripId: string, body: LockSeatsRequest): Observable<SeatLockResponseDto> {
    return this.http.post<SeatLockResponseDto>(`${this.base}/trips/${tripId}/seat-locks`, body);
  }

  releaseLock(lockId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/seat-locks/${lockId}`, {
      params: { sessionId }
    });
  }

  createBooking(body: CreateBookingRequest): Observable<CreateBookingResponseDto> {
    return this.http.post<CreateBookingResponseDto>(`${this.base}/bookings`, body);
  }

  verifyPayment(bookingId: string, body: VerifyPaymentRequest): Observable<BookingDetailDto> {
    return this.http.post<BookingDetailDto>(`${this.base}/bookings/${bookingId}/verify-payment`, body);
  }

  getBooking(bookingId: string): Observable<BookingDetailDto> {
    return this.http.get<BookingDetailDto>(`${this.base}/bookings/${bookingId}`);
  }

  getTicketUrl(bookingId: string): string {
    return `${this.base}/bookings/${bookingId}/ticket`;
  }
}

