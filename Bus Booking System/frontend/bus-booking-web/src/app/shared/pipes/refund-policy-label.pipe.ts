import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'refundPolicyLabel', standalone: true, pure: true })
export class RefundPolicyLabelPipe implements PipeTransform {
  transform(hoursUntilDeparture: number): string {
    if (hoursUntilDeparture >= 24) {
      return '80% refund (24h or more before departure)';
    }
    if (hoursUntilDeparture >= 12) {
      return '50% refund (12h–24h before departure)';
    }
    return 'Cancellation not allowed (under 12h to departure)';
  }
}
