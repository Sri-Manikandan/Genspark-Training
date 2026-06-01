# Portfolio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-page portfolio for Sri Manikandan R (Full Stack Developer) using Angular 21 standalone components and Tailwind CSS v4.

**Architecture:** Six standalone section components (Navbar, Hero, About, Skills, Projects, Contact) rendered in order by the existing `ProfileComponent` container. No routing, no services — purely presentational. All Tailwind v4 utility classes applied directly in templates.

**Tech Stack:** Angular 21, Tailwind CSS v4 (already configured), Jasmine/Karma for tests.

---

## File Map

| Action | Path | Responsibility |
|---|---|---|
| Modify | `src/styles.css` | Add smooth-scroll and body background |
| Modify | `src/app/app.html` | Remove stray `<h1>` tag |
| Create | `src/app/navbar/navbar.ts` | Navbar component class |
| Create | `src/app/navbar/navbar.html` | Navbar template |
| Create | `src/app/navbar/navbar.css` | Empty |
| Create | `src/app/navbar/navbar.spec.ts` | Navbar tests |
| Create | `src/app/hero/hero.ts` | Hero component class |
| Create | `src/app/hero/hero.html` | Hero template |
| Create | `src/app/hero/hero.css` | Empty |
| Create | `src/app/hero/hero.spec.ts` | Hero tests |
| Create | `src/app/about/about.ts` | About component class |
| Create | `src/app/about/about.html` | About template |
| Create | `src/app/about/about.css` | Empty |
| Create | `src/app/about/about.spec.ts` | About tests |
| Create | `src/app/skills/skills.ts` | Skills component class with data |
| Create | `src/app/skills/skills.html` | Skills template |
| Create | `src/app/skills/skills.css` | Empty |
| Create | `src/app/skills/skills.spec.ts` | Skills tests |
| Create | `src/app/projects/projects.ts` | Projects component class with data |
| Create | `src/app/projects/projects.html` | Projects template |
| Create | `src/app/projects/projects.css` | Empty |
| Create | `src/app/projects/projects.spec.ts` | Projects tests |
| Create | `src/app/contact/contact.ts` | Contact component class |
| Create | `src/app/contact/contact.html` | Contact template |
| Create | `src/app/contact/contact.css` | Empty |
| Create | `src/app/contact/contact.spec.ts` | Contact tests |
| Modify | `src/app/profile/profile.ts` | Import all section components |
| Modify | `src/app/profile/profile.html` | Render all section components |
| Modify | `src/app/profile/profile.spec.ts` | Test all sections are rendered |

---

## Task 1: Base styles and app cleanup

**Files:**
- Modify: `src/styles.css`
- Modify: `src/app/app.html`

- [ ] **Step 1: Update `src/styles.css`**

Replace the entire file content with:

```css
@import 'tailwindcss';

html {
  scroll-behavior: smooth;
}

body {
  background-color: #0f172a;
  margin: 0;
}
```

- [ ] **Step 2: Update `src/app/app.html`**

Replace the entire file content with:

```html
<app-profile />
```

- [ ] **Step 3: Commit**

```bash
git add src/styles.css src/app/app.html
git commit -m "feat: add base dark theme styles and clean up app shell"
```

---

## Task 2: NavbarComponent

**Files:**
- Create: `src/app/navbar/navbar.ts`
- Create: `src/app/navbar/navbar.html`
- Create: `src/app/navbar/navbar.css`
- Create: `src/app/navbar/navbar.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/navbar/navbar.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-navbar',
  imports: [],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class NavbarComponent {}
```

Create `src/app/navbar/navbar.html`:
```html
<p>navbar</p>
```

Create `src/app/navbar/navbar.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/navbar/navbar.spec.ts`:
```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NavbarComponent } from './navbar';

describe('NavbarComponent', () => {
  let fixture: ComponentFixture<NavbarComponent>;
  let el: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavbarComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(NavbarComponent);
    el = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display the developer name', () => {
    expect(el.textContent).toContain('Sri Manikandan R');
  });

  it('should render at least 5 nav links', () => {
    expect(el.querySelectorAll('a').length).toBeGreaterThanOrEqual(5);
  });
});
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/navbar/navbar.spec.ts
```

Expected: "should create" PASSES, the other two FAIL.

- [ ] **Step 4: Implement the navbar template**

Replace `src/app/navbar/navbar.html` with:
```html
<nav class="bg-slate-950 text-white px-8 py-4 flex justify-between items-center">
  <span class="text-xl font-bold text-cyan-400">Sri Manikandan R</span>
  <div class="flex gap-6 text-sm">
    <a href="#hero" class="hover:text-cyan-400 transition-colors">Home</a>
    <a href="#about" class="hover:text-cyan-400 transition-colors">About</a>
    <a href="#skills" class="hover:text-cyan-400 transition-colors">Skills</a>
    <a href="#projects" class="hover:text-cyan-400 transition-colors">Projects</a>
    <a href="#contact" class="hover:text-cyan-400 transition-colors">Contact</a>
  </div>
</nav>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/navbar/navbar.spec.ts
```

Expected: all 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/navbar/
git commit -m "feat: add NavbarComponent with anchor links"
```

---

## Task 3: HeroComponent

**Files:**
- Create: `src/app/hero/hero.ts`
- Create: `src/app/hero/hero.html`
- Create: `src/app/hero/hero.css`
- Create: `src/app/hero/hero.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/hero/hero.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-hero',
  imports: [],
  templateUrl: './hero.html',
  styleUrl: './hero.css',
})
export class HeroComponent {}
```

Create `src/app/hero/hero.html`:
```html
<p>hero</p>
```

Create `src/app/hero/hero.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/hero/hero.spec.ts`:
```typescript
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
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/hero/hero.spec.ts
```

Expected: "should create" PASSES, the other three FAIL.

- [ ] **Step 4: Implement the hero template**

Replace `src/app/hero/hero.html` with:
```html
<section id="hero" class="bg-slate-950 text-white min-h-screen flex items-center justify-center text-center px-4">
  <div>
    <h1 class="text-5xl font-bold mb-3">Sri Manikandan R</h1>
    <div class="w-24 h-1 bg-cyan-400 mx-auto mb-5"></div>
    <p class="text-2xl text-slate-300 mb-10">Full Stack Developer</p>
    <a href="#projects"
       class="bg-cyan-500 hover:bg-cyan-600 text-white px-8 py-3 rounded-lg font-semibold transition-colors">
      View Projects
    </a>
  </div>
</section>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/hero/hero.spec.ts
```

Expected: all 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/hero/
git commit -m "feat: add HeroComponent with name, title, and CTA"
```

---

## Task 4: AboutComponent

**Files:**
- Create: `src/app/about/about.ts`
- Create: `src/app/about/about.html`
- Create: `src/app/about/about.css`
- Create: `src/app/about/about.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/about/about.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-about',
  imports: [],
  templateUrl: './about.html',
  styleUrl: './about.css',
})
export class AboutComponent {}
```

Create `src/app/about/about.html`:
```html
<p>about</p>
```

Create `src/app/about/about.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/about/about.spec.ts`:
```typescript
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
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/about/about.spec.ts
```

Expected: "should create" PASSES, the other two FAIL.

- [ ] **Step 4: Implement the about template**

Replace `src/app/about/about.html` with:
```html
<section id="about" class="bg-slate-900 text-white py-20 px-8">
  <div class="max-w-4xl mx-auto">
    <h2 class="text-3xl font-bold text-center mb-12">About Me</h2>
    <div class="flex flex-col md:flex-row items-center gap-12">
      <div class="w-40 h-40 rounded-full bg-gradient-to-br from-slate-600 to-slate-800 flex-shrink-0 flex items-center justify-center text-6xl">
        👤
      </div>
      <div class="text-slate-300 text-lg leading-relaxed">
        <p class="mb-4">Hi, I'm Sri Manikandan R, a passionate Full Stack Developer with experience building modern web applications from the ground up.</p>
        <p class="mb-4">I specialize in crafting scalable backend services with .NET and Node.js, and building responsive frontends with Angular and TypeScript.</p>
        <p>I enjoy solving complex problems and delivering clean, maintainable code that makes a real impact.</p>
      </div>
    </div>
  </div>
</section>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/about/about.spec.ts
```

Expected: all 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/about/
git commit -m "feat: add AboutComponent with bio and avatar"
```

---

## Task 5: SkillsComponent

**Files:**
- Create: `src/app/skills/skills.ts`
- Create: `src/app/skills/skills.html`
- Create: `src/app/skills/skills.css`
- Create: `src/app/skills/skills.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/skills/skills.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-skills',
  imports: [],
  templateUrl: './skills.html',
  styleUrl: './skills.css',
})
export class SkillsComponent {
  skills = ['Angular', '.NET', 'SQL Server', 'Node.js', 'TypeScript'];
}
```

Create `src/app/skills/skills.html`:
```html
<p>skills</p>
```

Create `src/app/skills/skills.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/skills/skills.spec.ts`:
```typescript
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
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/skills/skills.spec.ts
```

Expected: first two PASS (data exists), last three FAIL (template not rendering them).

- [ ] **Step 4: Implement the skills template**

Replace `src/app/skills/skills.html` with:
```html
<section id="skills" class="bg-slate-950 text-white py-20 px-8">
  <div class="max-w-4xl mx-auto">
    <h2 class="text-3xl font-bold text-center mb-12">Skills</h2>
    <div class="flex flex-wrap justify-center gap-4">
      @for (skill of skills; track skill) {
        <span class="border border-cyan-400 text-cyan-400 bg-cyan-400/10 px-6 py-2 rounded-full font-medium text-sm">
          {{ skill }}
        </span>
      }
    </div>
  </div>
</section>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/skills/skills.spec.ts
```

Expected: all 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/skills/
git commit -m "feat: add SkillsComponent with badge grid"
```

---

## Task 6: ProjectsComponent

**Files:**
- Create: `src/app/projects/projects.ts`
- Create: `src/app/projects/projects.html`
- Create: `src/app/projects/projects.css`
- Create: `src/app/projects/projects.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/projects/projects.ts`:
```typescript
import { Component } from '@angular/core';

interface Project {
  title: string;
  description: string;
  tags: string[];
}

@Component({
  selector: 'app-projects',
  imports: [],
  templateUrl: './projects.html',
  styleUrl: './projects.css',
})
export class ProjectsComponent {
  projects: Project[] = [
    {
      title: 'E-Commerce Platform',
      description: 'A full-stack e-commerce application with product listings, cart, and checkout flow.',
      tags: ['Angular', '.NET', 'SQL Server'],
    },
    {
      title: 'Task Management API',
      description: 'RESTful API for managing tasks and projects with authentication and role-based access.',
      tags: ['Node.js', 'TypeScript', 'SQL Server'],
    },
    {
      title: 'Real-Time Dashboard',
      description: 'A live data dashboard with real-time updates using WebSockets and Angular.',
      tags: ['Angular', 'TypeScript', 'Node.js'],
    },
  ];
}
```

Create `src/app/projects/projects.html`:
```html
<p>projects</p>
```

Create `src/app/projects/projects.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/projects/projects.spec.ts`:
```typescript
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
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/projects/projects.spec.ts
```

Expected: first two PASS (data exists), last three FAIL.

- [ ] **Step 4: Implement the projects template**

Replace `src/app/projects/projects.html` with:
```html
<section id="projects" class="bg-slate-900 text-white py-20 px-8">
  <div class="max-w-5xl mx-auto">
    <h2 class="text-3xl font-bold text-center mb-12">Projects</h2>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      @for (project of projects; track project.title) {
        <div class="bg-slate-800 border border-slate-700 rounded-xl p-6 flex flex-col gap-4">
          <h3 class="text-xl font-semibold">{{ project.title }}</h3>
          <p class="text-slate-400 text-sm flex-1">{{ project.description }}</p>
          <div class="flex flex-wrap gap-2">
            @for (tag of project.tags; track tag) {
              <span class="text-xs bg-slate-700 text-slate-300 px-3 py-1 rounded-full">{{ tag }}</span>
            }
          </div>
          <button class="mt-2 border border-cyan-400 text-cyan-400 hover:bg-cyan-400 hover:text-slate-900 px-4 py-2 rounded-lg text-sm font-medium transition-colors">
            View
          </button>
        </div>
      }
    </div>
  </div>
</section>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/projects/projects.spec.ts
```

Expected: all 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/projects/
git commit -m "feat: add ProjectsComponent with placeholder project cards"
```

---

## Task 7: ContactComponent

**Files:**
- Create: `src/app/contact/contact.ts`
- Create: `src/app/contact/contact.html`
- Create: `src/app/contact/contact.css`
- Create: `src/app/contact/contact.spec.ts`

- [ ] **Step 1: Create the minimal component scaffold**

Create `src/app/contact/contact.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-contact',
  imports: [],
  templateUrl: './contact.html',
  styleUrl: './contact.css',
})
export class ContactComponent {}
```

Create `src/app/contact/contact.html`:
```html
<p>contact</p>
```

Create `src/app/contact/contact.css` (empty file).

- [ ] **Step 2: Write the failing tests**

Create `src/app/contact/contact.spec.ts`:
```typescript
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
```

- [ ] **Step 3: Run tests to confirm content tests fail**

```bash
npx ng test --watch=false --include=src/app/contact/contact.spec.ts
```

Expected: "should create" PASSES, the other three FAIL.

- [ ] **Step 4: Implement the contact template**

Replace `src/app/contact/contact.html` with:
```html
<section id="contact" class="bg-slate-950 text-white py-20 px-8">
  <div class="max-w-xl mx-auto text-center">
    <h2 class="text-3xl font-bold mb-6">Contact</h2>
    <p class="text-slate-300 mb-6">Feel free to reach out — I'm always open to new opportunities.</p>
    <p class="text-cyan-400 text-lg mb-10">sri.manikandan&#64;example.com</p>
    <div class="flex justify-center gap-8">
      <a href="#" class="text-slate-300 hover:text-cyan-400 transition-colors font-medium">GitHub</a>
      <a href="#" class="text-slate-300 hover:text-cyan-400 transition-colors font-medium">LinkedIn</a>
    </div>
  </div>
</section>
```

- [ ] **Step 5: Run tests to confirm all pass**

```bash
npx ng test --watch=false --include=src/app/contact/contact.spec.ts
```

Expected: all 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/contact/
git commit -m "feat: add ContactComponent with email and social links"
```

---

## Task 8: Wire up ProfileComponent

**Files:**
- Modify: `src/app/profile/profile.ts`
- Modify: `src/app/profile/profile.html`
- Modify: `src/app/profile/profile.spec.ts`

- [ ] **Step 1: Write the failing test first**

Replace `src/app/profile/profile.spec.ts` with:
```typescript
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
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
npx ng test --watch=false --include=src/app/profile/profile.spec.ts
```

Expected: "should create" PASSES, all section-render tests FAIL.

- [ ] **Step 3: Update `src/app/profile/profile.ts` to import all section components**

Replace the entire file with:
```typescript
import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';
import { HeroComponent } from '../hero/hero';
import { AboutComponent } from '../about/about';
import { SkillsComponent } from '../skills/skills';
import { ProjectsComponent } from '../projects/projects';
import { ContactComponent } from '../contact/contact';

@Component({
  selector: 'app-profile',
  imports: [
    NavbarComponent,
    HeroComponent,
    AboutComponent,
    SkillsComponent,
    ProjectsComponent,
    ContactComponent,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {}
```

- [ ] **Step 4: Update `src/app/profile/profile.html` to render all sections**

Replace the entire file with:
```html
<app-navbar />
<main>
  <app-hero />
  <app-about />
  <app-skills />
  <app-projects />
  <app-contact />
</main>
```

- [ ] **Step 5: Run all tests to confirm everything passes**

```bash
npx ng test --watch=false
```

Expected: all tests across all spec files PASS.

- [ ] **Step 6: Commit**

```bash
git add src/app/profile/
git commit -m "feat: wire up ProfileComponent to render all portfolio sections"
```

---

## Final verification

- [ ] **Start the dev server and visually verify the portfolio**

```bash
npx ng serve
```

Open `http://localhost:4200` in a browser. Check:
- Dark navy background covers the full page
- Navbar shows name on left, 5 links on right
- Hero section is full-screen with name, cyan underline, title, CTA button
- About section shows avatar circle and bio text
- Skills section shows 5 cyan pill badges
- Projects section shows 3 cards in a grid with tags and View buttons
- Contact section shows email and GitHub/LinkedIn links
- Clicking navbar links scrolls smoothly to each section
