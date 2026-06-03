### Angular HTTP Interceptor Implementation

- Created HTTP interceptor for automatic bearer token authentication
  - Intercepts all outgoing requests before reaching backend services
  - Automatically retrieves token from session storage and adds to Authorization header
  - Eliminates repetitive manual token addition across all authorized API endpoints
- Technical implementation details:
  1. Created http-interceptor.ts file in app folder (alternative: miscellaneous/interceptors folder)
  2. Exported authInterceptor constant of type HttpInterceptorFn
  3. Function takes request and next parameters, checks for token availability
  4. Cannot directly modify request object - must clone using request.clone()
  5. Cloned request includes Authorization header with bearer token
  6. Returns cloned request if token exists, otherwise original request
- Configuration and registration:
  - Registered in app.config.ts using withInterceptors([authInterceptor])
  - Must import authInterceptor at top of config file
  - Attaches interceptor to HttpClient service for all HTTP operations
- Login service integration:
  - Modified login method to store token in session storage after successful authentication
  - Used sessionStorage.setItem(‘token’, response.token) pattern
  - Session storage preferred over local storage for security (expires with browser session)
- Verification and testing:
  - Demonstrated with get account details API endpoint
  - Network tab showed Authorization header automatically included in requests
  - Unauthorized (401) errors eliminated for protected endpoints

### Local Template Reference Variables vs NgModel

- Introduced template reference variables as alternative to two-way binding
  - Created #accountNumber reference on input element
  - Accessed input.value property directly in method calls
  - Syntax: (click)=“getAccountDetails(accountNumber.value)”
- Comparison with NgModel approach:
  - NgModel requires FormsModule import and creates component property
  - Template reference suitable for one-time value capture scenarios
  - NgModel better for dynamic value tracking and validation
  - Template reference reduces component complexity for simple inputs
- Implementation benefits:
  - No need to create component properties for temporary input values
  - Direct access to DOM element properties beyond just value
  - Cleaner component code when input doesn’t need persistent state tracking

### RxJS Subject Pattern and Observable Fundamentals

- Introduced Subject as custom observable publisher for dynamic data streams
  - Subject type declaration for type-safe string emissions
  - Imported from ‘rxjs’ package, extends Observable functionality
  - Can emit multiple values over time using .next() method
- Basic Subject implementation pattern:
  1. Declared searchSubject = new Subject() as class property
  2. Subscribed in constructor using this.searchSubject.subscribe()
  3. Published values on events using this.searchSubject.next(value)
  4. Subscription receives emitted values in observer pattern
- Subscription management and lifecycle:
  - Subscribe in constructor to establish listener before any emissions
  - Proper observer object with next, error, complete handlers
  - Must unsubscribe in ngOnDestroy to prevent memory leaks
  - Called this.searchSubject.complete() and unsubscribe() in cleanup
- Observable vs Promise comparison:
  - Observables emit multiple values, Promises resolve once
  - Observables support cancellation via unsubscribe()
  - Observables are lazy (don’t execute until subscribed)
  - Better for continuous data streams and event handling

### Advanced Search Optimization with RxJS Operators

- Planned pipe operator chain for intelligent search behavior:
  - debounceTime(500) - Waits 500 milliseconds after user stops typing
  - distinctUntilChanged() - Only proceeds if search value actually changed
  - switchMap() - Cancels previous HTTP requests when new search initiated
- Network optimization benefits:
  - Prevents API bombardment during rapid typing or backspacing
  - Reduces server load and improves application responsiveness
  - Handles edge cases like typing same value repeatedly
- SwitchMap behavior explanation:
  - Automatically cancels previous observable when new one emitted
  - Returns Observable from HTTP service call
  - Flattens nested observables into single stream
  - Handles null/empty account numbers with conditional logic
- Implementation considerations:
  - Import operators from ‘rxjs/operators’
  - Chain operators using pipe() method on subject
  - Error handling for failed HTTP requests within switchMap
  - Return empty observable for invalid search terms

### Inter-Component Communication Architecture

- Subject-based communication between unrelated components
  - Created changeUsername() method for cross-component data sharing
  - Login component publishes username after successful authentication
  - App component subscribes to receive username updates
- Real-world application scenarios:
  - Navbar updates showing “Hello [username]” instead of “Hello Guest”
  - Toggle between login/signup buttons and logout functionality
  - Maintain consistent authentication state across entire application
- Implementation pattern:
  - Export constant functions from shared service files
  - Components import and call these functions to publish data
  - Other components subscribe to receive published updates
  - Avoids tight coupling between components in different parts of app tree

### Modern Angular Patterns vs Traditional Approaches

- Dependency injection evolution:
  - Traditional: Constructor-based injection with @Injectable decorator
  - Modern: inject() function for cleaner syntax without constructor boilerplate
  - Both approaches achieve same result, inject() is newer Angular syntax
- AI-generated code advanced patterns observed:
  - Computed signals for derived reactive state management
  - Reactive forms instead of template-driven forms with ngModel
  - Route guards for authentication and authorization checks
  - Lazy loading components for performance optimization
  - Custom pipes for data transformation in templates
  - Standalone components reducing NgModule dependencies
- Folder structure best practices:
  - Separate components folder for larger projects
  - Organize by feature rather than file type
  - Interceptors in dedicated folder or miscellaneous directory
  - Services grouped by domain functionality
- Production-ready features comparison:
  - Foundation learned: basic components, services, HTTP client, routing
  - AI code includes: advanced RxJS operators, complex state management
  - Performance optimizations: OnPush change detection, lazy loading
  - Security patterns: route guards, token interceptors, role-based access

### Project Timeline and Evaluation Criteria

- Immediate next steps (Wednesday afternoon, June 3, 2026):
  - Design frontend screens using preferred tools (Figma, wireframes, paper)
  - Plan component structure and user flow for individual projects
  - Complete frontend mockups before backend API development begins
- Dedicated API development period:
  - Thursday-Friday, June 4-5, 2026: Focused backend development time
  - Instructor support available for questions and code reviews
  - Screen sharing sessions for real-time debugging assistance
  - Goal: Complete all backend APIs by end of Friday session
- Special Friday activity (June 5, 2026):
  - Team sharing session: Each person presents one unforgettable life experience
  - Estimated 2+ hours duration, plan API work accordingly
  - Stories can be funny, scary, memorable from school/college/personal life
- Backend evaluation criteria (Monday, June 8, 2026):
  - Complete API endpoints following established coding standards
  - Proper business logic implementation (e.g., prevent duplicate seat bookings)
  - Code optimization and performance considerations
  - Ability to explain code functionality and make live modifications
  - AI usage acceptable if student demonstrates understanding and can adapt code
  - Postman collection setup for API testing and demonstration
- Evaluation scoring approach:
  - Positive points for code optimization and best practices
  - Deductions for poor coding standards or inability to explain logic
  - Business rule implementation creativity and thoroughness assessed
  - Dynamic code modification during evaluation session required