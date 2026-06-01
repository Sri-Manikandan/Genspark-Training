import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkillsComponent } from './skills';

describe('SkillsComponent', () => {
  let fixture: ComponentFixture<SkillsComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkillsComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(SkillsComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should have 5 skills in the data array', () => {
    expect(fixture.componentInstance.skills.length).toBe(5);
  });

  it('should render Angular skill badge', () => {
    expect(el.textContent).toContain('Angular');
  });

  it('should render TypeScript skill badge', () => {
    expect(el.textContent).toContain('TypeScript');
  });

  it('should render SQL Server skill badge', () => {
    expect(el.textContent).toContain('SQL Server');
  });
});
