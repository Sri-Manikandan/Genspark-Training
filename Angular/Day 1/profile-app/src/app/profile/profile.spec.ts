import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Profile } from './profile';

describe('Profile', () => {
  let fixture: ComponentFixture<Profile>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Profile],
    }).compileComponents();
    fixture = TestBed.createComponent(Profile);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the navbar', () => {
    expect(el.querySelector('app-navbar')).toBeTruthy();
  });

  it('should render the hero section', () => {
    expect(el.querySelector('app-hero')).toBeTruthy();
  });

  it('should render the about section', () => {
    expect(el.querySelector('app-about')).toBeTruthy();
  });

  it('should render the skills section', () => {
    expect(el.querySelector('app-skills')).toBeTruthy();
  });

  it('should render the projects section', () => {
    expect(el.querySelector('app-projects')).toBeTruthy();
  });

  it('should render the contact section', () => {
    expect(el.querySelector('app-contact')).toBeTruthy();
  });
});
