import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SeatLayoutDto } from '../../../core/api/search.api';
import { SeatMapComponent } from './seat-map.component';

function layout(): SeatLayoutDto {
  return {
    rows: 2,
    columns: 2,
    seats: [
      { seatNumber: 'A1', rowIndex: 0, columnIndex: 0, status: 'available' },
      { seatNumber: 'A2', rowIndex: 0, columnIndex: 1, status: 'booked' },
      { seatNumber: 'B1', rowIndex: 1, columnIndex: 0, status: 'locked' },
      { seatNumber: 'B2', rowIndex: 1, columnIndex: 1, status: 'available' }
    ]
  };
}

describe('SeatMapComponent', () => {
  let fixture: ComponentFixture<SeatMapComponent>;
  let component: SeatMapComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SeatMapComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SeatMapComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('layout', layout());
  });

  it('builds a 2x2 grid keyed by rowIndex/columnIndex', () => {
    fixture.detectChanges();
    const grid = component.seatGrid();
    expect(grid.length).toBe(2);
    expect(grid[0].length).toBe(2);
    expect(grid[0][0]?.seatNumber).toBe('A1');
    expect(grid[1][1]?.seatNumber).toBe('B2');
  });

  it('ignores clicks when `selectable` is false (default)', () => {
    fixture.detectChanges();
    const a1 = layout().seats[0];
    component.toggle(a1);
    expect(component.selected()).toEqual([]);
  });

  it('toggles available seats into the selection when selectable=true', () => {
    fixture.componentRef.setInput('selectable', true);
    fixture.detectChanges();

    const emitted: string[][] = [];
    component.selectionChange.subscribe(s => emitted.push(s));

    component.toggle(layout().seats[0]);
    expect(component.selected()).toEqual(['A1']);
    expect(emitted.at(-1)).toEqual(['A1']);

    component.toggle(layout().seats[3]);
    expect(component.selected()).toEqual(['A1', 'B2']);

    component.toggle(layout().seats[0]);
    expect(component.selected()).toEqual(['B2']);
  });

  it('never toggles booked or locked seats', () => {
    fixture.componentRef.setInput('selectable', true);
    fixture.detectChanges();

    component.toggle(layout().seats[1]);
    component.toggle(layout().seats[2]);
    expect(component.selected()).toEqual([]);
  });

  it('caps selection at `maxSelected`', () => {
    fixture.componentRef.setInput('selectable', true);
    fixture.componentRef.setInput('maxSelected', 1);
    fixture.detectChanges();

    component.toggle(layout().seats[0]);
    component.toggle(layout().seats[3]);
    expect(component.selected()).toEqual(['A1']);
  });

  it('renders aria-label and aria-pressed on seat buttons', () => {
    fixture.componentRef.setInput('selectable', true);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const a1 = Array.from(buttons).find(b => b.textContent?.trim() === 'A1')!;
    expect(a1.getAttribute('aria-label')).toBe('Seat A1 available');
    expect(a1.getAttribute('aria-pressed')).toBe('false');

    component.toggle(layout().seats[0]);
    fixture.detectChanges();
    expect(a1.getAttribute('aria-pressed')).toBe('true');
  });
});
