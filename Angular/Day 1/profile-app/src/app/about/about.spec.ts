import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AboutComponent } from './about';

describe('AboutComponent', () => {
  let fixture: ComponentFixture<AboutComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AboutComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(AboutComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the About Me heading', () => {
    expect(el.textContent).toContain('About Me');
  });

  it('should render an avatar placeholder element', () => {
    const avatar = el.querySelector('.rounded-full');
    expect(avatar).toBeTruthy();
  });
});
