- **Why signals over traditional binding**
  - Component maintains its own memory instead of refreshing entire component tree
  - Zone.js (old approach) refreshes whole component tree when detecting changes
  - Signal indicates specific memory that could change after initial load
  - Derived from React.js concept for faster, lightweight performance
- **Getter/setter pattern implementation**
  - Getter: productName() - retrieves value like method call
  - Setter: productName.set('new value') - triggers change detection
  - Memory access delegated through methods, not direct memory manipulation
  - Similar to React’s useState hook pattern
- **When to use signals**
  - Put data in signal only if it will change after component loads first time
  - Enables middleware additions and performance optimizations later
  - Change detection triggered by setter, not background monitoring
- **Object vs array signals**
  - Single object: signal<ProductModel>(new ProductModel())
  - Array: signal<ProductModel[]>([])
  - Both follow same getter/setter pattern

### Implementation Walkthrough

- **Creating products component**
  - Generated via Angular CLI: ng generate component products
  - Creates folder with component files (ts, html, css, spec)
- **Single product signal setup**
  - Strong typing emphasis: signal<string> specifies data type
  - Initial value provided: 'sample product'
- **HTML binding syntax**
  - Note method-style call with parentheses
  - Different from traditional interpolation (no parentheses)
- **Updating signal values**
- **Scaling to API data**
  - Moved from single ProductModel to ProductModel[] array
  - Updated signal: products = signal<ProductModel[]>([])
  - Populated via API response: this.products.set(response.products)

### Models & TypeScript Typing

- **ProductModel structure**
- **Constructor parameter handling**
  - Initial mistake: autocomplete suggested required parameters
  - Corrected to optional parameters for flexible object creation
  - Emphasis on not trusting autocomplete blindly
- **Strong typing discipline**
  - Always specify data types in TypeScript
  - signal<string>, signal<ProductModel[]> examples
  - Critical for maintainable Angular applications

### HTTP Client & Dependency Injection

- **Application configuration**
  - Enables HTTP client service application-wide
  - Configuration automatically bootstrapped in main.ts
- **Service creation with DI**
- **Injectable decorator requirements**
  - Plain TypeScript classes need @Injectable() for DI eligibility
  - Components automatically injectable (via @Component)
  - Makes class eligible to receive and provide injections
- **Constructor injection patterns**
  - private http: HttpClient - accessible within class only
  - public would create accessible property but not needed here
  - Private reduces boilerplate vs manual property declaration

### API Calls & Observable Pattern

- **GET request implementation**
- **API response structure**
  - External API returns: {products: [...]}
  - Access array via: response.products
- **Observable subscription handling**
- **Observable vs Promise differences**
  - Observable: multiple emissions via next(), has complete(), supports unsubscribe()
  - Promise: single emission, no completion signal, no unsubscribe
  - Observable like newsletter subscription - continuous data, can unsubscribe
- **Automatic completion**
  - GET requests complete automatically after data received
  - Demonstrated in console: data → complete sequence

### Dynamic List Rendering (Angular 17+)

- **New @for syntax with signals**
- **Tracking importance**
  - track product.id enables individual item updates
  - Without tracking, entire list re-renders on changes
  - Uses ID for internal component identification
- **Property binding vs interpolation**
  - Preferred: [src]="product.thumbnail" (property binding)
  - Works but “dirty”: src="{{product.thumbnail}}" (interpolation)
  - Property binding clearer for backend data, better error detection
- **Bootstrap + Flexbox layout**
  - Responsive grid automatically wraps items
  - 16px gap between cards
  - Adjusts to screen size dynamically

### Conditional Rendering & UX States

- **Loading state implementation**
  - Shows briefly while API loads
  - Condition fails when products array populated
- **Button state management**
  - Disable login button during API call: [disabled]="isLoading"
  - Re-enable on both success AND error responses
  - Prevents multiple submissions (idempotent behavior)
- **UX best practices**
  - Always show work-in-progress feedback
  - Avoid fluorescent colors (green/red) - bring user attention appropriately
  - Loading indicators improve perceived performance

### POST API Flow (Login Example)

- **Service method structure**
- **DTO structure**
- **Base URL configuration**
  - Create environment files in src/app or src/environments
  - Import baseUrl in service: import { baseUrl } from './environment'
  - Production deployment: replace with actual domain URLs
- **CORS policy handling**
  - Backend must allow frontend origin
  - Required when frontend/backend on different ports/domains
  - Configure in Spring Boot or respective backend framework

### Development Tooling & Debugging

- **VS Code import behavior**
  - Auto-import sometimes fails or bugs out
  - Manual import required: check component imports in .ts files
  - Restart VS Code/system if auto-import not working
- **Component style scoping**
  - Angular generates unique attribute IDs per component
  - Maps component styles to specific elements
  - Prevents style conflicts between components
  - Visible in DOM as ng-content attributes with numbers
- **Internal tracking vs DOM**
  - Angular’s internal tracking IDs not visible in DOM
  - Used for change detection and component tree management
  - Different from user-defined IDs or classes

### Tomorrow’s Topics & Assignment

- **RxJS operators session**
  - Pagination implementation
  - Filter and search operators
  - Throttling for performance
  - Reduces code repetition for common operations
- **Interceptor overview**
  - HTTP request/response middleware
  - Authentication token handling
  - Error handling centralization
- **Today’s assignment**
  1. Build Register page using POST method (similar to Login)
  2. Submit comfort level note on API calls and frontend learning
  3. Document what you learned about signals vs older zone.js binding
  4. Review day-one project - ensure you understand frontend-backend connectivity
- **Project development approach**
  - Build component by component with immediate service binding
  - Complete one module (frontend + backend + binding) before next
  - Parallel: backend ready, frontend design, then binding integration