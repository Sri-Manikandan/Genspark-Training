import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HeroComponent } from './hero';

describe('HeroComponent', () => {
  let fixture: ComponentFixture<HeroComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeroComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(HeroComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the name in an h1', () => {
    expect(el.querySelector('h1')?.textContent?.trim()).toContain('Sri Manikandan R');
  });

  it('should display the Full Stack Developer title', () => {
    expect(el.textContent).toContain('Full Stack Developer');
  });

  it('should have a View Projects anchor link', () => {
    const link = el.querySelector('a[href="#projects"]');
    expect(link).toBeTruthy();
    expect(link?.textContent).toContain('View Projects');
  });
});
