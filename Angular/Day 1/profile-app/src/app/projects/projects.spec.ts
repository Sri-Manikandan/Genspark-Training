import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProjectsComponent } from './projects';

describe('ProjectsComponent', () => {
  let fixture: ComponentFixture<ProjectsComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectsComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(ProjectsComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have 3 projects in the data array', () => {
    expect(fixture.componentInstance.projects.length).toBe(3);
  });

  it('should render E-Commerce Platform title', () => {
    expect(el.textContent).toContain('E-Commerce Platform');
  });

  it('should render Task Management API title', () => {
    expect(el.textContent).toContain('Task Management API');
  });

  it('should render Real-Time Dashboard title', () => {
    expect(el.textContent).toContain('Real-Time Dashboard');
  });
});
