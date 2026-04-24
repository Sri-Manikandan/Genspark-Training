import { ComponentFixture, TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { CountdownTimerComponent } from './countdown-timer.component';

describe('CountdownTimerComponent', () => {
  let fixture: ComponentFixture<CountdownTimerComponent>;
  let component: CountdownTimerComponent;

  beforeEach(async () => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2026-04-24T10:00:00.000Z'));

    await TestBed.configureTestingModule({
      imports: [CountdownTimerComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(CountdownTimerComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => jasmine.clock().uninstall());

  function setExpiry(offsetSeconds: number): void {
    const expiry = new Date(Date.now() + offsetSeconds * 1000).toISOString();
    fixture.componentRef.setInput('expiresAt', expiry);
    fixture.detectChanges();
  }

  it('displays remaining time in mm:ss', () => {
    setExpiry(125);
    expect(component.display()).toBe('02:05');
    expect(component.isWarning()).toBeFalse();
    expect(component.isExpired()).toBeFalse();
  });

  it('turns isWarning=true when 60 seconds or fewer remain', () => {
    setExpiry(45);
    expect(component.display()).toBe('00:45');
    expect(component.isWarning()).toBeTrue();
    expect(component.isExpired()).toBeFalse();
  });

  it('counts down once per second as time advances', () => {
    setExpiry(5);
    expect(component.display()).toBe('00:05');

    jasmine.clock().tick(1000);
    fixture.detectChanges();
    expect(component.display()).toBe('00:04');

    jasmine.clock().tick(2000);
    fixture.detectChanges();
    expect(component.display()).toBe('00:02');
  });

  it('emits `expired` exactly once when the countdown hits zero', () => {
    const emissions: undefined[] = [];
    setExpiry(2);
    component.expired.subscribe(() => emissions.push(undefined));

    jasmine.clock().tick(2000);
    fixture.detectChanges();

    expect(component.isExpired()).toBeTrue();
    expect(component.display()).toBe('00:00');
    expect(emissions.length).toBe(1);

    jasmine.clock().tick(5000);
    fixture.detectChanges();
    expect(emissions.length).toBe(1);
  });

  it('clamps to 00:00 when expiry is already in the past', () => {
    setExpiry(-30);
    expect(component.display()).toBe('00:00');
    expect(component.isExpired()).toBeTrue();
  });
});
