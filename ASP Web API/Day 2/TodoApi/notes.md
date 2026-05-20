### Web API Hands-On Session

- Morning session focused on controller-based Web API with database connectivity
- Self-paced learning approach followed by group demonstration
- Participants worked through provided link with database connectivity examples
- Most completed initial exercise before moving to group session
- Patch method demonstration specifically requested and confirmed for later
- Project creation and asset generation for build/debug discussed

### EF Core Integration Implementation

- Package installation within same Web API application
- DbContext configuration changes from hardcoded connection strings
  - Moved connection string to appsettings.json under “ConnectionStrings”
  - Constructor injection pattern: takes DbContextOptions parameter
  - Constructor chaining to pass options to base class
- Environment-specific configuration support
  - Development.json and Production.json for different environments
  - ASP.NET Core environment variable controls which config loads
  - Future migration to Azure Key Vault for production secrets
- Migration workflow
  - Add-Migration command after building project
  - Update-Database creates tables and relationships
  - Verbose output shows actual SQL table creation
  - Team workflow: pull code, update connection string, run update-database only

### Dependency Injection Architecture

- Dependency Injection vs Dependency Inversion distinction
  - DI: Constructor takes object as parameter (injection mechanism)
  - Dependency Inversion: Depend on interfaces, not concrete classes
- Provider role and runtime management
  - Program.cs builder.Services acts as provider
  - ASP.NET Core runtime automatically manages object creation/injection
  - Services registered via AddDbContext, AddScoped methods
- Service lifetimes and object creation patterns
  - Scoped: One object per request, shared across all injections in that request
  - Singleton: Single object for entire application lifetime
  - Transient: New object for each injection point
- Constructor chaining implementation
  - Repository takes DbContext injection
  - Service takes Repository injection
  - Controller takes Service injection
  - Automatic injection flow: Context→Repository→Service→Controller

### SOLID Principles in Banking Application

- Single Responsibility Principle
  - Account model: only handles account data structure
  - CustomerService: only customer business logic
  - BankingContext: only database operations
  - AccountRepository: only CRUD operations
- Open/Closed Principle
  - Classes open for inheritance through virtual methods
  - Closed for direct modification
  - Abstract repository allows inheritance for customization
  - Virtual methods enable overriding in derived classes
- Liskov Substitution Principle
  - Account base class replaceable with CurrentAccount or SavingsAccount
  - Code should work seamlessly without casting
  - Child classes override existing methods, don’t introduce entirely new functionality
  - Avoids need for type casting to access child-specific methods
- Interface Segregation example
  - Log repository needs only Insert and Get operations
  - Don’t implement full CRUD interface with empty Update/Delete methods
  - Create lightweight interface or base repository with minimal methods
  - Avoid NotImplementedException in unused methods

### Architecture Decisions and Patterns

- 3-tier vs single project guidance
  - Mid-size projects: use folders within single project for maintainability
  - Large projects with multiple modules: separate DLLs for each layer
  - Domain-driven design (DDD) offers alternative architecture approach
- Web API project structure
  - Controllers contain only endpoints and dependency injection
  - Models, Repositories, Services, Interfaces, Context folders
  - Miscellaneous folder for exceptions and utilities
  - DTOs can be separate folder or within Models folder

### Data Transfer Objects (DTOs)

- Purpose and cyclic JSON prevention
  - Account has Customer property, Customer has Account collection
  - Creates infinite serialization loop during JSON conversion
  - DTOs break the cycle by controlling which properties are included
- Request vs Response model separation
  - CreateAccountRequest: MinimumBalance, AccountType, CustomerId (3 fields)
  - CreateAccountResponse: AccountNumber, Balance, AccountType, CustomerId, Status
  - GetAccountResponse: similar to create response structure
- Benefits beyond cycle prevention
  - Transfer only required data, reduce payload size
  - Control exactly what client sends/receives
  - Separate concerns between database models and API contracts

### Service and Controller Implementation

- Program.cs service registrations
  - AddDbContext with PostgreSQL connection string
  - AddScoped<IRepository<string,Account>, AccountRepository>
  - Dependency inversion: interface mapped to concrete implementation
- Account number auto-generation logic
  - 12-digit format: country (3) + state (3) + branch (3) + sequence (3)
  - Query existing accounts, order by account number descending
  - Take top record, increment last 4 digits for new account
- Controller endpoints implemented
  - POST: CreateAccount accepts CreateAccountRequest, returns CreateAccountResponse
  - GET: GetAccount by account number, returns account details
  - HTTP status codes: CreatedAt for POST, BadRequest for exceptions

### Demo Results and Issues

- POST endpoint successfully creates accounts with auto-generated numbers
- GET endpoint retrieves account details by account number
- Issues identified during testing
  - CustomerId mapping showing 0 instead of provided value
  - Account number generation working but formatting needs verification
  - Null reference exceptions when migration not properly executed
- Database creation successful with proper table relationships

### Development Environment Q&A

- HTTPS configuration optional in development
  - Can be enabled later via launchSettings.json with dev certificate
  - Production deployment will require proper SSL setup
- In-memory database behavior
  - Exists only during application runtime
  - Data lost when application stops
  - Suitable for development/testing scenarios
- Null injector exceptions
  - Occur when requesting injection without registering service
  - Solution: register all dependencies in Program.cs before use
  - Best practice: register services immediately after creating them

### Next Steps

- Code will be pushed with migrations included
- Team members: update connection string in appsettings.json, run update-database
- Afternoon session will include similar hands-on exercise
- Capstone project topics to be distributed today for research and design phase
- Continue fine-tuning Web API to address remaining bugs and exceptions