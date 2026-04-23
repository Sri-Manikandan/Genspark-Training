import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  input,
  output,
  signal
} from '@angular/core';

@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './countdown-timer.component.html'
})
export class CountdownTimerComponent implements OnDestroy {
  readonly expiresAt = input.required<string>();
  readonly expired = output<void>();

  private readonly now = signal(Date.now());
  private readonly intervalId = setInterval(() => this.tick(), 1000);

  readonly secondsLeft = computed(() => {
    const target = new Date(this.expiresAt()).getTime();
    return Math.max(0, Math.floor((target - this.now()) / 1000));
  });

  readonly display = computed(() => {
    const s = this.secondsLeft();
    const mm = Math.floor(s / 60)
      .toString()
      .padStart(2, '0');
    const ss = (s % 60).toString().padStart(2, '0');
    return `${mm}:${ss}`;
  });

  readonly isWarning = computed(() => this.secondsLeft() <= 60 && this.secondsLeft() > 0);
  readonly isExpired = computed(() => this.secondsLeft() === 0);

  private emittedExpired = false;
  private tick(): void {
    this.now.set(Date.now());
    if (!this.emittedExpired && this.secondsLeft() === 0) {
      this.emittedExpired = true;
      this.expired.emit();
    }
  }

  ngOnDestroy(): void {
    clearInterval(this.intervalId);
  }
}

