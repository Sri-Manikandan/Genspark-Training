### Asynchronous Programming Concepts

- Processor time optimization challenges
  - Applications contain multiple tasks, some dependent on others’ outputs
  - Single process systems execute only one task at any given time
  - Processor time wasted when tasks wait for input or external resources
  - Need mechanism to utilize processor efficiently during wait periods
- Process vs task distinction and hyperthreading
  - Single process system can only execute one process at any point
  - Hyperthreading (HT) allows virtual processor division for powerful processors
  - Processor quickly shifts between processes in nanoseconds
  - Creates illusion of simultaneous execution across multiple processes
  - Legacy round-robin algorithm mentioned as obsolete scheduling method
- Threading complexity and pain points
  - Raw threading approach required manual thread creation and management
  - Programmers struggled with memory allocation for threads
  - Semaphore concepts difficult for many developers to comprehend
  - Manual toggling between threads created significant complexity
  - High error rate and messy implementations led industry toward abstractions
- Evolution toward simplified approach
  - Industry decided to reduce programmer burden
  - Solution: methods return Task instead of direct data
  - Runtime handles thread management automatically
  - Developers use await keyword when output required before proceeding

### Async/Await Implementation

- Modern C#/.NET async pattern
  - Methods marked with async keyword return Task or Task
  - Runtime automatically manages thread pool operations
  - No manual thread allocation or management required
  - Significant simplification over raw threading approaches
- Await semantics and behavior
  - Use await only when method output required before next step
  - Omitting await causes method to return Task immediately and continue execution
  - Runtime understands await as signal to retrieve completed output
  - Missing await can cause early method exit without executing remaining code
- Thread pool mechanics
  - Runtime maintains pool of available threads
  - Async methods automatically assigned to free threads in pool
  - Completed work queued and ready for retrieval
  - Thread pool handles allocation, execution, and cleanup automatically
- Common implementation pitfalls
  - Forgetting await keyword leads to incomplete execution
  - Marking methods async unnecessarily when no async operations present
  - Must be conscious and careful when writing async methods
  - Improper async usage can cause application to exit prematurely

### Code Implementation Changes

- Repository layer modifications
  - Changed return types from T to Task for async operations
  - Added async keyword to method signatures
  - Implemented await before SaveChangesAsync() calls
  - Applied await to repository method calls requiring results before proceeding
  - Maintained same functionality while enabling asynchronous execution
- Service layer updates
  - Customer service converted to async pattern where appropriate
  - Token service kept synchronous since no async operations required
  - Added proper await usage for operations dependent on other method outputs
  - Avoided unnecessary async marking for methods without async operations
- Controller layer changes
  - Updated action method signatures to return Task
  - Applied await keyword to service method calls
  - Maintained existing functionality with improved concurrency potential
  - Expected performance improvements from async implementation
- Testing and validation
  - Login functionality verified working after changes
  - Transaction processing confirmed successful
  - Account status updates and amount calculations working correctly
  - All existing functionality preserved with async benefits

### Unit Testing Overview and Setup

- AAA testing methodology
  - Arrange: setup test conditions and data
  - Act: execute the method being tested
  - Assert: verify expected outcomes and behaviors
  - Standard pattern for all unit test implementations
- Testing framework options and selection
  - NUnit chosen for this project
  - Other options mentioned: xUnit, MSTest (Microsoft Unit Testing)
  - Framework syntax differs but concepts remain consistent
  - Ensured test project targets same .NET version as main project
- Test project structure and lifecycle
  - Created separate test project within solution
  - [SetUp] attribute: method executes before every test
  - [TearDown] attribute: method executes after every test
  - Test Explorer available for running and monitoring tests
- Dependency injection advantages for testing
  - Easy to inject custom DbContext for testing scenarios
  - Can provide test data through constructor injection
  - Eliminates need to instantiate dependencies within classes being tested
  - Makes unit testing significantly easier and more flexible
- Database testing strategies
  - EF Core InMemory database chosen for this project
  - Alternative: separate test database for large-scale projects
  - InMemory option creates temporary database in memory during testing
  - Database automatically dropped after test completion

### Unit Tests Implemented During Session

- Repository test examples
  - AddCustomer pass test: verified object returned not null and ID mapping correct
  - GetCustomer pass test: confirmed retrieval by ID returns expected customer data
  - DeleteCustomer exception test: validated custom exception thrown for non-existent customer ID
  - Used async/await pattern in test methods to handle async repository methods
- Service layer testing setup
  - Created InMemory database context for service testing
  - Configured multiple repository dependencies (Account, User, Customer repositories)
  - Set up TokenService with mocked configuration including JWT settings
  - Implemented comprehensive dependency injection for service testing
- OpenAccount service test implementation
  - Created test customer in repository before testing account creation
  - Used CreateAccountRequest DTO for service method input
  - Verified account number generation and customer ID association
  - Discovered logic error during test execution

### Defect Found and Fix

- Issue identification
  - GenerateAccountNumber method threw IndexOutOfRange exception
  - Problem occurred when no existing accounts in database
  - Method attempted to access last account number from empty collection
  - Unit test revealed this edge case that manual testing missed
- Root cause analysis
  - Code assumed at least one existing account for number generation
  - New database or fresh test environment exposed the flaw
  - Logic error rather than syntax or configuration issue
  - Demonstrated value of unit testing for edge case discovery
- Solution implemented
  - Added guard clause checking if account count equals zero
  - When no accounts exist, automatically assign first account number
  - Maintained existing logic for incrementing from last account number
  - Re-ran tests to confirm fix resolved the issue

### Test Execution and Results

- Running tests through IDE
  - Right-click method and select “Run Test” option
  - Test Explorer window opens showing test results
  - Available through View menu as well
  - Can run individual tests or entire test suite
- Test result interpretation
  - Green tick mark indicates test passed successfully
  - Failed tests show assertion details and exception information
  - Test Explorer provides comprehensive result overview
  - Easy identification of passing vs failing test cases

### Expectations and Next Steps

- Immediate actions
  - Code pushed to Git repository for team access
  - Team members to pull latest changes and review implementation
  - Thumbs-up confirmation requested after code review completion
  - Break scheduled before continuing with additional testing
- Testing coverage goals
  - Minimum three tests required per method
  - Target 80% code coverage for service layer
  - Service layer priority since it contains business logic
  - Controller testing planned for post-break session
- Future testing expansion
  - Additional unit tests for remaining service methods
  - Controller testing implementation and best practices
  - Integration testing discussion planned
  - Emphasis on testing both positive and negative scenarios