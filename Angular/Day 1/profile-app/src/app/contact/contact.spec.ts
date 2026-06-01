import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ContactComponent } from './contact';

describe('ContactComponent', () => {
  let fixture: ComponentFixture<ContactComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(ContactComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display a Contact heading', () => {
    expect(el.querySelector('h2')?.textContent?.trim()).toBe('Contact');
  });

  it('should have a GitHub link', () => {
    expect(el.textContent).toContain('GitHub');
  });

  it('should have a LinkedIn link', () => {
    expect(el.textContent).toContain('LinkedIn');
  });
});
