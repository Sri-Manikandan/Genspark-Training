# Notes
dotnet new webapi -n <WebAPIAppName> --no-https false --use-controllers true --no-openapi false

- Web API introduction and fundamentals
- Database connectivity implementation
- Exception handling and filters
- Authentication and authorization (authN/Z)
- Validations and dynamic endpoints
- JSON object handling and communication
- Performance tuning and optimization
- Overflow topics continue next week
- Capstone project specifications delivery this week
- Post-lunch project work time (5 participants requested)

### Application Architecture & Design

- Frontend: Angular communicates via JSON with backend
- Backend: ASP.NET Core Web API processing JSON requests/responses
- Layered architecture maintained:
  - Models in separate DLL or folders
  - Services layer for business logic
  - Repositories for data access
  - Controllers for endpoint management
- Console application learning purpose only - not production target
- Resource-oriented design focus (entities over methods)
- Controller-based endpoint organization per resource type
- Routing table creation in memory for request mapping
- Stateless protocol - server doesn’t remember client identity
- Token-based client identification system
- Caching provision for frequently requested data

### HTTP Protocol & REST Fundamentals

- CRUD operations mapped to HTTP verbs:
  - GET: retrieve/read data
  - POST: send data (typically create operations)
  - PUT: edit/update existing data
  - DELETE: remove data
- HTTP status code ranges:
  - 100-199: informational responses
  - 200-299: success (Created, No Content included)
  - 300-399: redirection responses
  - 400-499: client-side errors (404 Not Found example)
  - 500-599: server-side errors (SQL unavailable, crashes, timeouts)
- RESTful service characteristics:
  - Resource-focused architecture
  - Server-client model
  - Stateless communication
  - Caching capability

### Development Environment & Tooling

- Visual Studio ASP.NET Core Web API template selection
- Framework version configuration during setup
- Authentication options: None, Identity, Windows (None selected)
- HTTPS configuration enabled by default
- SSL certificate behavior:
  - Development: localhost dummy certificate
  - Production: valid purchased certificate required
  - Lock symbol indicates secure connection
- Environment settings in launchSettings.json:
  - Development environment enables Swagger
  - Production environment disables Swagger
- Kestrel server as default web server
- Swagger/OpenAPI integration for:
  - API documentation
  - Endpoint testing interface
  - Request/response visualization
- Postman software installation recommended for API testing

### Program.cs Structure & Builder Pattern

- Top-level statements eliminate main method boilerplate
- Builder pattern implementation for web application creation
- Pre-Build phase (order doesn’t matter):
  - Add controllers
  - Add Swagger services
  - Configure authentication
  - Add other required services
- Build method creates web application object
- Post-Build phase (sequence critical):
  - Environment check for development
  - Enable Swagger in development only
  - HTTPS redirection setup
  - Controller mapping
- app.Run() starts application and enables request reception
- Builder organizes inputs properly regardless of addition order
- Routing table populated from controller discovery

### Design Patterns Overview

- Creational patterns (object creation):
  - Factory: hierarchical inheritance object selection
  - Singleton: single instance management
  - Builder: step-by-step construction with proper sequencing
- Structural patterns (object composition):
  - Adapter: interface conversion between incompatible objects
  - Composite: complex object breakdown into smaller components
  - Proxy: encapsulation of underlying API calls
  - Flyweight: lightweight objects for memory-constrained applications
- Behavioral patterns (object interaction):
  - Observer: publisher-subscriber notification system
  - Iterator: object traversal mechanisms
  - Chain of Responsibility: sequential processing chains
- Practical applications:
  - Builder prevents construction sequence errors
  - Controllers organize resource-based routing
  - Factory handles polymorphic object creation

### Business Logic & Exception Handling

- Custom exception communication from service layer
- Example: BookLimitReachedException for library management
- Business logic validates constraints before data operations
- Exception throwing communicates rule violations to calling code
- Service layer isolation from data access concerns
- Proper error communication between application layers

### Multi-Phase Project Development Plan

- Level 1: Individual capstone project completion
  - Backend development first
  - Frontend integration after API completion
  - UI/UX design using AI tools (design tools, Stitch recommended)
- Level 2: Project exchange and collaboration
  - Mutual Knowledge Transfer (KT) sessions
  - Feature addition to exchanged projects
  - Pull request creation and review process
  - Code quality assessment and standards enforcement
  - Caution with AI-generated code integration
- Level 3: Migration projects
  - Technology stack transitions
  - Parallel execution during Azure/Docker modules
  - Cross-platform development experience

### Learning Philosophy & Practice Approach

- Hands-on repetition develops logical thinking “balance”
- Analogy to learning bicycle balance or swimming float
- Trainer provides guidance while students develop independent logic
- Practice builds mental framework for problem-solving
- Progression from guided learning to autonomous development
- Multiple technology layers build comprehensive understanding

### Hands-On Implementation Steps

- Create new ASP.NET Core Web API project
- Configure HTTPS and Swagger support
- Inherit controllers from ControllerBase class
- Use [Route] attributes for endpoint definition
- Implement action methods for HTTP verbs
- Test endpoints using browser (GET only) or Postman
- Monitor network requests via F12 developer tools
- Verify JSON response format and status codes

### Immediate Action Items

- Install Postman software (request IT team assistance if needed)
- Create new Web API project following demonstrated configuration
- Review design patterns documentation (link to be provided)
- Prepare for controller implementation exercises
- Begin hands-on practice with routing and action methods
