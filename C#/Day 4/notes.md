### Exception Handling Fundamentals

- Exception = unexpected situation at runtime (not errors)
- Programmers must be “pessimistic” - expect things to go wrong
- Applications should handle situations gracefully, not crash
- Real-world example: ATM transaction failure during network outage
  - Account debited but no cash dispensed
  - Should rollback transaction automatically
  - Provide reference number for dispute resolution

### Structured Exception Handling Syntax

- Try-catch-finally blocks for handling exceptions
- Try block: contains code that might throw exceptions
- Catch blocks: handle specific exception types
  - Must be relevant to exception thrown
  - Provide user-friendly messages (not technical details)
- Finally block: executes whether exception occurs or not
  - Used for resource cleanup (file closing, database connections)
  - Executes even after return statements

### Exception Handling Best Practices

- Never return null - biggest programming mistake
- Write relevant catch blocks for each exception type
- Provide proper user messages vs programmer messages
- Handle exceptions in calling code, not where they occur
- Examples of common exceptions:
  1. OverflowException - data limits exceeded
  2. FormatException - invalid input format
  3. DivideByZeroException - mathematical errors

### Custom Exception Implementation

- Create user-defined exceptions for business logic communication
- Inherit from Exception class to enable throwing
- Example: InvalidPhoneNumberException for account creation
- Benefits over returning null:
  - Clear communication of what went wrong
  - Calling code knows specific failure reason
  - Better error handling and user experience

### Multi-Layer Exception Strategy

- Different exception types for each application layer:
  - Data Access Layer: SQL/database exceptions
  - Business Logic Layer: custom business exceptions
  - UI Layer: user interface exceptions
- Separation of concerns - each layer handles relevant exceptions
- Convert technical exceptions to business-friendly ones between layers

### Separation of Concerns Architecture

- Split application into separate class libraries:
  1. Model Library - data objects and exceptions
  2. Data Access Layer (DAL) - CRUD operations only
  3. Business Logic Layer (BL) - validations and business rules
  4. User Interface - user interaction only
- Benefits:
  - Easy to change individual components
  - Reusable across different applications
  - Clear responsibility assignment

### Real-World Application Examples

- Demonetization ATM failure case study
  - Non-scalable algorithms and hardware
  - Importance of flexible, configurable systems
- Banking account creation validation:
  - Phone number verification with OTP
  - Email validation
  - Age verification for account types
  - Service availability checks

### Next Steps

- Practice exception handling in existing applications
- Implement multi-tier architecture with separate projects
- Recording will be shared via Google Drive for review
- Assignment deadline: Thursday midnight for multi-tier application
- Tomorrow’s topics: extension methods and LINQ
- Capstone project requirements to be shared next week