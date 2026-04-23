import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';

interface StatusStyle {
  label: string;
  classes: string;
}

@Component({
  selector: 'app-booking-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatChipsModule],
  template: `
    <mat-chip [class]="style().classes" disableRipple>
      {{ style().label }}
    </mat-chip>
  `
})
export class BookingStatusBadgeComponent {
  readonly status = input.required<string>();

  readonly style = computed<StatusStyle>(() => {
    switch (this.status()) {
      case 'confirmed':
        return { label: 'Confirmed', classes: 'bg-emerald-100 text-emerald-800' };
      case 'pending_payment':
        return { label: 'Pending payment', classes: 'bg-amber-100 text-amber-800' };
      case 'cancelled':
        return { label: 'Cancelled', classes: 'bg-rose-100 text-rose-800' };
      case 'cancelled_by_operator':
        return { label: 'Cancelled (operator)', classes: 'bg-rose-100 text-rose-800' };
      case 'completed':
        return { label: 'Completed', classes: 'bg-slate-200 text-slate-800' };
      default:
        return { label: this.status(), classes: 'bg-slate-200 text-slate-800' };
    }
  });
}
