### Object-Oriented Programming Fundamentals

- OOP designed for complex problems, not simple tasks like adding two numbers or finding greatest of two numbers
  - Using OOP for trivial problems is “painful process” - like using complex solution architecture for basic calculations
  - Object orientation becomes “very irritating and useless” when applied to simple problems
- Evolution from modular to wholesome programming solutions
  - Earlier computing: dot matrix printers in accounts departments for salary calculations only
  - Modular programming worked well for mathematical problems - break into small modules, solve separately, combine solutions
  - Modern need: comprehensive applications like shopping apps requiring wholesome picture
  - Cannot solve shopping cart → order conversion with separate modular approach - too complex to map IDs between modules
- Real-life mapping principle: programming structure must match real-life structures
  - Product should be wholesome with all necessary properties
  - Customer should be wholesome with every required property available
- Object definition and classification
  - Object: anything with properties and behaviors in given context
  - Can be tangible (customer, bus, phone) or intangible (route, class being delivered)
  - Class: blueprint/collection of properties and behaviors; defines the object structure
  - Object: instance of a class
  - Properties called attributes; behaviors called methods/functionalities

### Four Core OOP Principles

- **Inheritance**: Using existing properties/behaviors, adding what’s needed
  - Real-life analogy: generations inherit knowledge, don’t start from scratch; motorcycle inherits from cycle knowledge
  - Types allowed in C#: Single (one base class, one derived class), Multi-level (grandparent→parent→child), Hierarchical (one base class, multiple derived classes)
  - Multiple inheritance not allowed due to diamond problem/ambiguity - when inheriting from multiple classes with same method names, unclear which to invoke
  - Interface workaround available but creates complexity (James Gosling “turning in his grave”)
  - Common examples: Vehicle→Bus/Car/Minivan/Minibus; Account→Savings/Current/Demat; Customer→Platinum/Gold/Silver
  - Hierarchical and multi-level are majorly used inheritance types
  - Common properties go in base class, specific properties only in derived classes
- **Encapsulation**: Hiding unnecessary internal workings, wrapping properties and behaviors together
  - Phone example: user knows how to make calls (dial, press green button) but internal call routing is hidden
  - Don’t need to know: how phone charges battery, how messages are sent, how apps install internally
  - Object’s internal working hidden from user; only necessary methods/behaviors exposed
  - Access specifiers in .NET: public, private, protected, internal, protected internal, private protected (six total)
  - Public: usable everywhere; Protected: only for inheritance; Internal: within same assembly
  - Private methods used for internal working only, not exposed to object users
- **Abstraction**: Showing only what’s necessary based on context
  - Same object exhibits different behaviors in different contexts
  - Customer object example:
    - To bus operator: shows book bus, make payment, cancel bus, add passenger behaviors
    - To admin: shows provide feedback on operator, register/unregister behaviors
  - All behaviors exist in same customer object, but interface exposes only relevant methods per context
  - Achieved through interfaces - pass IOperatorCustomer to operator, IAdminCustomer to admin
  - All method implementations remain inside customer object
- **Polymorphism**: Same method behaving differently in different environments
  - Static/Compile-time polymorphism (early binding):
    - Implemented through overloading - same method, different parameter lists
    - Login example: login(username, password) for desktop vs login(biometrics) for mobile
    - Compiler knows which method to invoke before runtime based on parameters
    - Changing number/order/data type of parameters
  - Dynamic/Runtime polymorphism (late binding):
    - Implemented through overriding - requires inheritance mandatorily
    - Booking example: same booking method gives 10% discount for gold customer, no discount for general customer
    - Only at runtime system knows customer type and which method to invoke
    - Parent vs child class method decision made at runtime

### C# Properties and Implementation Details

- Properties replace traditional getter/setter complexity
  - Old way: private variable + separate GetAccountNumber() and SetAccountNumber() methods
  - New way: single property with automatic getter/setter generation
  - Compiler converts properties to get/set methods during compilation
  - Works for all variable types regardless of return type, including custom structs
- Property implementation options:
  - Simple auto-implemented: public string AccountNumber { get; set; }
  - Custom logic example: masking account number to show only last four digits using substring
  - Lambda expressions for simple cases with no complex logic
  - Can provide default values: = string.Empty or use required keyword
- Nullable reference types and validation:
  - String is nullable type, generates warnings if not handled
  - Solutions: make nullable with ?, use required keyword, or set default string.Empty
  - Required fields enforce mandatory input; empty string is just opening/closing quotes with no space
  - Zero warnings policy strongly recommended - shows refined programming
- Default initialization rules:
  - Primitive types: initialized to base values automatically
  - Reference types: initialized to null unless explicitly set
  - Alternative: assign default values in default constructor if not set in property declaration

### Constructors and Early Binding

- Constructor overloading demonstrates static polymorphism
  - Default constructor: no parameters, calls base initialization
  - Parameterized constructor: accepts specific properties, can be generated via Control+. in Visual Studio
  - Compiler knows which constructor to invoke before compilation (early binding)
  - Zero reference vs multiple references visible in IDE before execution
- Code generation shortcuts:
  - prop + tab: generates property template
  - ctor: generates default constructor
  - Control+.: generates parameterized constructor with selected properties
  - Works in both Visual Studio and Visual Studio Code

### Practical Banking Demo Walkthrough

- Account base class structure:
  - Properties: AccountNumber (string), NameOnAccount, DateOfBirth (DateTime), Email, Phone, Balance (decimal/double)
  - AccountType enum with SavingsAccount/CurrentAccount values
  - Child classes: SavingsAccount, CurrentAccount (hierarchical inheritance)
  - AccountNumber as string: no arithmetic operations needed, handles 10-12 digit numbers without size restrictions
- ToString() method override demonstration:
  - Base Object.ToString() returns fully qualified class name only
  - Override provides meaningful account details using interpolation
  - Child classes can further override to append AccountType
  - Runtime polymorphism: same method, different behavior based on object type
  - Zero references during compilation shows late binding uncertainty
- Account opening workflow:
  - Validate user choice with loop: force entry of 1 (Savings) or 2 (Current) only
  - Type casting validation: choice > 0 && choice < 3
  - Instantiate correct object type based on choice
  - Collect inputs: name, date of birth, email, phone with basic validations suggested
  - Validation opportunities: DOB < today, age >= 18 check, OTP for phone/email verification
  - Initialize balance (default 0 for primitives, or set custom amount)
- Data management:
  - Static List collection to store all accounts
  - Parent reference holds both SavingsAccount and CurrentAccount objects
  - Account number generation: static string seed incremented for each new account
  - Search functionality: iterate through collection, match account number, return object
- UX and coding standards observed:
  - Always prefix currency with type (INR, not just numbers)
  - Include country code (+91) for phone numbers
  - Use proper salutations (Mr./Ms.) when gender is captured
  - Maintain politeness in customer interactions

### Interfaces and Service Layer Abstraction

- ICustomerInteract interface design:
  - Methods: OpenAccount() returns Account; PrintAccountDetails(string accountNumber)
  - Interface names typically use verbs for functionality description
  - Defines contract for service layer implementation
- CustomerService class implementation:
  - Implements ICustomerInteract interface
  - Contains business logic and account management
  - Private methods for internal operations (encapsulation)
  - Constructor initializes interface reference with concrete service class
- Method design principles:
  - Parameters: how you communicate to the method (input)
  - Return types: how method communicates back to you (output)
  - Avoid void methods when possible - enable method communication
  - Keep methods under 15 executable lines of code
  - Use private helper methods to break down complex operations
  - Method parameters are short-lived, exist only within method scope
- Abstraction through interfaces:
  - Show only necessary functionality to users
  - Service class may contain many behaviors, interface exposes only required ones
  - Enables clean separation between what’s available vs what’s needed
  - Supports multiple programmers working with defined contracts

### Project Structure and Organization

- Folder organization for clean architecture:
  - Models folder: Account, SavingsAccount, CurrentAccount classes
  - Services folder: CustomerService (business logic)
  - Interfaces folder: ICustomerInteract
  - Automatic namespace arrangement when moving files
- Development environment notes:
  - Visual Studio: auto-saves on execution
  - Visual Studio Code: enable auto-save manually recommended
  - Dynamic compilation shows references before execution
  - Control+. for code generation works in both environments
- Code sharing and version control:
  - Code shared via Code Share platform
  - Also pushed to GitHub for version control
  - Students can pull and work with provided examples

### Coding Standards and Best Practices

- Code quality standards:
  - Zero warnings policy (not just zero errors) - indicates refined programming
  - Readable, maintainable code over complex single methods
  - Avoid 100-line methods with multiple responsibilities
  - Use private methods for internal operations, public for external interface
- Control flow preferences:
  - Switch statements preferred over long if-else chains for better readability
  - Proper validation loops to force correct user input
  - Break statements to exit loops when conditions met
- Data type considerations:
  - Decimal vs float/double for currency: decimal reduces rounding errors but requires backend mapping
  - Float/double acceptable for applications with proper validation blocks
  - String for identifiers like account numbers (no arithmetic needed)
- Method communication:
  - Always use parameters and return types for method interaction
  - Avoid keeping variables in memory longer than necessary
  - Reference types can be modified through method parameters
  - Memory management handled effectively through proper scoping

### Assignments and Next Steps

- Two assignments for completion:
  - Menu-driven console application: Add Account, Print Account Details, Exit options with loop iteration
  - Transport application review: add interfaces/model structure, document changes and rationale based on OOP learning
- Learning progression plan:
  - Initial focus: CRUD operations (Create, Read, Update, Delete)
  - Later progression: business logic implementation
  - Parallel learning: fundamentals + AI-generated code understanding
- Schedule and logistics:
  - Break scheduled at 11:35 AM
  - Five working days this week for training
  - May 1st was last holiday; next holidays expected in September (Indian festivals)
  - No holidays planned during training period
  - Post-lunch: continue with CRUD operations, then advance to business logic