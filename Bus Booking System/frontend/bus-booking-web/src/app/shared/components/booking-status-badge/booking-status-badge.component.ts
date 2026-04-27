import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-booking-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `<span class="status-badge" [class]="'status-badge--' + cssKey()">{{ label() }}</span>`,
  styleUrl: './booking-status-badge.component.scss'
})
export class BookingStatusBadgeComponent {
  readonly status = input.required<string>();

  readonly label = computed(() => {
    const map: Record<string, string> = {
      confirmed: 'Confirmed',
      pending_payment: 'Pending',
      cancelled: 'Cancelled',
      cancelled_by_operator: 'Cancelled',
      completed: 'Completed',
    };
    return map[this.status()] ?? this.status();
  });

  readonly cssKey = computed(() => {
    const map: Record<string, string> = {
      confirmed: 'confirmed',
      pending_payment: 'pending',
      cancelled: 'cancelled',
      cancelled_by_operator: 'cancelled',
      completed: 'completed',
    };
    return map[this.status()] ?? 'default';
  });
}
