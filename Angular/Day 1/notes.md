### SPA Trade-offs and Real-world Examples

- Single Page Application definition: Page stays put, only parts refresh without full page reload
- Performance trade-offs for SPAs:
  - Advantage: No full page reloads after initial load
  - Disadvantage: Entire application must load upfront into browser
  - Not viable for all retail applications due to system resource constraints
- Live examples tested during session:
  - Netflix: Partial SPA with excellent buffering/caching, first load takes time even on smart TV
  - Gmail: Refreshes when clicking services
  - YouTube: Web application refreshes
  - Google Classroom: Mixed behavior
  - Notion: Appears to be SPA but requires login to verify
  - Rolls-Royce website: Mentioned for testing
  - MakeMyTrip: Heavy image engagement, search is SPA but hotel search refreshes due to image loading
- Retail brands (Hermes, Burberry): Typically not 100% SPA despite affordability for upgrades

### Image Optimization Workflow and Performance Impact

- Participant image size testing results: 10MB, 21KB, 7MB, 182KB, 600KB samples
- WebP conversion benefits and caveats:
  - Generally reduces size to approximately 1/3 of original
  - Some cases showed size increase (4.5MB → 4.6MB, 2.2MB → 2.9MB)
  - Size increase may relate to aspect ratio adjustments for full screen
  - Files below 100-200KB may have different conversion behavior
- Image metadata contains:
  - Date/time of capture
  - Camera specifications and resolution
  - GPS location data (latitude/longitude)
- Real-world application example:
  - Construction industry attendance tracking
  - App plots current location + image metadata location
  - Correlates both coordinates within ~1km radius for attendance verification
- Performance considerations:
  - 10-15 images typical on homepage slider
  - HD images significantly impact initial page load time
  - Solutions: Lazy loading attribute in HTML img tag, API-based conversion vs user restrictions

### Angular vs React Technology Choice

- Microsoft ecosystem alignment: Angular integrates better with .NET stack
- Company tech stack preference: Angular + .NET + SQL Server combination
- Web API compatibility: Angular works more comfortably with .NET Web API than React
- .NET has built-in web development capabilities that complement Angular

### Setup, CLI Commands, and Development Ports

- Prerequisites: Node.js version 20+ mandatory for Angular development
- Installation sequence:
  1. npm install -g @angular/cli (global Angular CLI installation)
  2. ng version (verify installation, current version 21.2 shown)
  3. ng new [project-name] (create new project)
- Project creation prompts:
  - Style choice: CSS, Tailwind, SCSS options (session uses CSS)
  - Server-side rendering: No (client-side only)
  - AI tool integration: Available but not used in training
- Development commands:
  - ng serve - starts development server on port 4200 (Angular default)
  - ng test - runs unit tests with Karma/Jasmine
  - ng build - compiles for production
- Auto-reload: File changes trigger automatic recompilation and browser refresh

### Project Architecture and Bootstrap Flow

- Entry point sequence: index.html → main.ts → app.ts → app.html
- Bootstrap process:
  1. index.html contains <app-root> custom element
  2. main.ts bootstraps the application
  3. app.ts defines selector: ‘app-root’
  4. Angular matches selector to inject app.html content
- Component prefix configuration:
  - Default prefix: ‘app’ (line 15 in angular.json)
  - All components use app-[component-name] selector pattern
- File structure per component:
  - .ts file: TypeScript source code
  - .html file: Template/view
  - .css file: Component-specific styling
  - .spec.ts file: Unit testing
- Package.json structure:
  - Dependencies: Available in production (Angular core, forms, router, RxJS, TypeScript)
  - DevDependencies: Development-only packages (removed in ng build --prod)

### Testing Stack and Unit Testing

- Built-in testing framework: Karma + Jasmine (ships with Angular)
- Command: ng test launches unit testing suite
- Default tests created:
  - “should create the app”
  - “should render the title”
- Each component automatically gets .spec.ts file for unit testing
- Testing runs in browser with live results display
- Front-end unit testing emphasized as important practice

### Data Binding Types and FormsModule Integration

- Three core binding types demonstrated:
  1. **Interpolation**: {{customerName}} - displays TS variable in HTML
  2. **Property binding**: [value]="customerName" - binds TS variable to HTML property
  3. **Event binding**: (click)="handleChange()" - connects HTML events to TS methods
- Two-way data binding with ngModel:
  - Syntax: [(ngModel)]="customerName"
  - Requires FormsModule import in component
  - Changes in input field automatically update TS variable and display
  - Combines property binding + event binding functionality
- TypeScript variable declaration: customerName: string = "John";
- Method example: handleChange() { alert(this.customerName); }

### Modeling, DTOs, and TypeScript Patterns

- Model organization options:
  - Decision depends on project size and module structure
- TypeScript class creation patterns:
  - **Traditional approach**: Declare properties, initialize via constructor
  - **Simplified approach**: public parameters in constructor auto-create properties
- Constructor parameter syntax:
- TypeScript constraints:
  - No method overloading available
  - Must initialize all declared variables or use optional parameters
  - Optional parameters: username?: string or default values
- DTO mapping best practice: Use identical property names and casing as backend API
- Variable naming: Camel case in frontend matches API expectations

### Styling with CSS, Bootstrap, and CDN Integration

- Component-specific CSS: Each component gets dedicated .css file
- CSS class binding: [class]="'table-class'" for dynamic styling
- Bootstrap integration via CDN:
  - Added to index.html for global availability
  - Bootstrap CSS CDN link enables predefined classes
  - Example classes used: btn btn-primary, text-center
- Bootstrap Icons integration:
  - Separate CDN for icon library
  - Production recommendation: Use minified version
  - Icons available for UI enhancement (heart icons demonstrated)
- CSS property binding examples:
  - [style.color]="colorName" for dynamic styling
  - Angular enforces proper DOM structure (rejected bgColor, required style.color)

### Today’s Exercise and Assignments

- Completed exercise: Customer component with model binding
  - Created Customer model with username, name, email, phone, dateOfBirth
  - Implemented interpolation, property binding, event binding
  - Added Bootstrap styling and CSS classes
- Current assignment: Product component creation
  - New component: ng g c product
  - New model: Product with name, price, description, image properties
  - Display in Bootstrap card layout format
  - Image property: Use string URL (not special data type)
- Post-lunch assignment: Learn Bootstrap or Tailwind CSS (for those unfamiliar)

### Next Steps and Signal Migration

- Tomorrow’s transition: Moving from conventional data binding to Angular Signals
- Rationale for current approach: 75% of existing projects use conventional binding
- Signal advantages: Replaces Zone.js dependency, improves performance
- Zone.js limitation: Re-renders entire component tree on changes
- Signal benefits: Angular’s own solution, better performance than third-party Zone.js
- Training strategy: Learn conventional first for legacy project compatibility, then migrate to modern Signal approach