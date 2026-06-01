# Portfolio Design — Sri Manikandan R

**Date:** 2026-06-01  
**Project:** profile-app (Angular 21 + Tailwind CSS v4)

---

## Overview

A single-page portfolio for Sri Manikandan R, Full Stack Developer. All sections are stacked vertically on one page with a non-sticky top navbar whose links smooth-scroll to section anchors.

---

## Architecture

- **Framework:** Angular 21 (standalone components)
- **Styling:** Tailwind CSS v4 (already configured via `@import 'tailwindcss'` in `styles.css`)
- **Routing:** None
- **State/Services:** None — purely presentational components

The `ProfileComponent` (`src/app/profile/`) acts as the page container and renders all section components in order.

---

## Components

| Component | Selector | Purpose |
|---|---|---|
| `NavbarComponent` | `app-navbar` | Top nav with anchor links |
| `HeroComponent` | `app-hero` | Name, title, CTA button |
| `AboutComponent` | `app-about` | Bio + placeholder avatar |
| `SkillsComponent` | `app-skills` | Skill badge/pill grid |
| `ProjectsComponent` | `app-projects` | 3 placeholder project cards |
| `ContactComponent` | `app-contact` | Email + social links |

Each component is a standalone Angular component created under `src/app/` in its own subdirectory.

---

## Section Details

### Navbar
- Horizontal layout, non-sticky (scrolls away with page)
- Left: name brand ("Sri Manikandan R")
- Right: anchor links — Hero, About, Skills, Projects, Contact
- Dark background, white text

### Hero
- Full-width, vertically centered
- Large heading: "Sri Manikandan R"
- Subtitle: "Full Stack Developer"
- Cyan accent underline beneath the name
- CTA button: "View Projects" — anchor scrolls to `#projects`

### About
- Two-column layout (responsive: single column on mobile)
- Left: circular placeholder avatar (gray gradient circle)
- Right: 3–4 sentences of placeholder bio text

### Skills
- Section heading: "Skills"
- Wrapping flex row of pill badges
- Skills: Angular, .NET, SQL Server, Node.js, TypeScript
- Style: cyan border, cyan-tinted background, white text

### Projects
- Section heading: "Projects"
- 3-column grid (responsive: 1 col mobile, 2 col tablet, 3 col desktop)
- Each card:
  - Project title (placeholder)
  - 2-line description (placeholder)
  - Tech tag chips (small badges)
  - "View" button (placeholder, non-functional)

### Contact
- Centered layout
- Section heading: "Contact"
- Email text (placeholder)
- GitHub and LinkedIn icon links (placeholder hrefs)

---

## Visual Design

- **Background:** Dark navy `#0f172a` (slate-950)
- **Text:** White / slate-300 for secondary text
- **Accent:** Cyan `#06b6d4` (cyan-500) for highlights, badges, buttons, underlines
- **Cards:** Slightly lighter background (`slate-800`/`slate-900`), rounded corners, subtle border
- **Font:** System default (no external font imports)

---

## File Structure

```
src/app/
├── app.ts
├── app.html
├── profile/
│   ├── profile.ts        ← page container, imports all section components
│   ├── profile.html      ← renders navbar + all sections
│   └── profile.css
├── navbar/
│   ├── navbar.ts
│   ├── navbar.html
│   └── navbar.css
├── hero/
│   ├── hero.ts
│   ├── hero.html
│   └── hero.css
├── about/
│   ├── about.ts
│   ├── about.html
│   └── about.css
├── skills/
│   ├── skills.ts
│   ├── skills.html
│   └── skills.css
├── projects/
│   ├── projects.ts
│   ├── projects.html
│   └── projects.css
└── contact/
    ├── contact.ts
    ├── contact.html
    └── contact.css
```

---

## Out of Scope

- No Angular Router
- No backend / form submission
- No animations beyond Tailwind transitions
- No external font or icon libraries (use Unicode/emoji for icons or plain text)
