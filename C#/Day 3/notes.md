### Training Session Overview & AI Usage Guidelines

- Morning session on object-oriented programming fundamentals
- Strong guidance against AI usage for learning assignments
  - AI prevents understanding of complex concepts during learning phase
  - Simple/mundane problems should be solved manually to build skills
  - Screen sharing reveals AI usage to instructors
  - AI appropriate only for complex problems, not basic exercises
  - Using AI for simple tasks won’t prepare students for future challenges

### Object-Oriented Programming Fundamentals

- Encapsulation definition and implementation
  - Binding properties and methods together within objects
  - User model example: private attributes with public getter/setter methods
  - Access specifiers control exposure (private fields, public methods)
  - C# uses properties instead of traditional getter/setter methods
- Object vs Class distinction
  - Objects are instances of classes
  - Memory allocation occurs only during object instantiation, not for class definitions
  - Classes serve as blueprints; objects are real-world implementations
- Interface concepts and purpose
  - Define method signatures as contracts for implementing classes
  - Service contract analogy: specifies functionalities you’re bound to receive
  - Provides abstraction of functionality without implementation details
  - Forces implementing classes to provide required methods

### Constructor Implementation & Overloading

- Constructor purposes and types
  - Initialize class attributes during object instantiation
  - Default constructor: zero parameters, automatically provided by compiler if none written
  - Parameterized constructor: takes parameters for custom initialization
- Constructor overloading mechanics
  - Multiple constructors in same class with different signatures
  - Variations allowed: parameter count, data types, parameter order
  - No inheritance required - overloading occurs within same class
  - Enables flexible object creation patterns

### CRUD Operations & Repository Design Pattern

- Design patterns solve common programming problems
  - Repository pattern specifically addresses CRUD operation standardization
  - Prevents creating separate repositories for each entity type
  - Provides common solutions instead of starting from scratch
- Standard CRUD method structure
  1. Create - Add new records, return created object for confirmation
  2. Read - GetAll() for collections, Get(key) for single records
  3. Update - Modify existing records using primary key
  4. Delete - Remove records, typically soft delete (status change only)
- Delete operation specifics
  - Return deleted object for UI confirmation messages
  - Soft delete preferred over hard delete in production systems
  - Object detached from collection but returned to caller

### Generic Repository Implementation

- Generic interface design: IRepository<T, K> where T : class
  - T represents entity type (Account, Customer, Employee)
  - K represents key data type (string for Account, int for Transaction)
  - Single interface instantiated for multiple entity types
- AccountRepository example
  - Implements IRepository<Account, string>
  - String key type for account numbers
  - Account entity type for stored objects
  - Methods automatically generated for specified types
- Key type variations by entity
  - Account: string keys for account numbers
  - Transaction: integer keys for transaction IDs
  - Customer: could use GUID or integer depending on design

### Collections & Data Management Strategy

- Dictionary<K,V> chosen for internal storage
  - Key-value pair structure enables fast lookups by primary key
  - ContainsKey() method for existence checking
  - Direct access via dictionary[key] syntax
- Memory management and object lifecycle
  - Remove() detaches reference from collection, doesn’t delete object from memory
  - Object remains in memory until all references removed and garbage collected
  - Multiple references can exist to same object
- Nullable return handling
  - Methods return nullable types for not-found scenarios
  - Caller responsible for null checking before usage
- Sorting requirements and performance
  - Data must be returned in sorted order, not random
  - Implement IComparable interface for sorting capability
  - CompareTo() method uses AccountNumber.CompareTo() for comparison
  - Large datasets: sort and paginate in database (ORDER BY with OFFSET/LIMIT)
  - Avoids O(n log n) overhead in application layer

### Advanced OOP Concepts

- Indexer implementation
  - Exposes internal collection through array-like syntax
  - Repository object treated like dictionary: repo[key] = value
  - Maintains encapsulation while providing convenient access
  - Single indexer per class, exposes one chosen collection
- Inheritance and method resolution
  - Abstract methods: no implementation, forces child class override
  - Virtual methods: has implementation, allows optional child override
  - Interface methods: no override keyword needed (implied contract)
- Polymorphism mechanics
  - Override vs shadowing (new keyword) behavior
  - RHS object determines method called with override
  - LHS reference determines method called with shadowing
  - Override enables true polymorphism, shadowing breaks it

### Implementation Assignments & Next Steps

- Student deliverables
  - Build console UI with CRUD menu loop (options 1-6 for add/view/update/delete/exit)
  - Study provided links on C# collections and generics
  - Pull code from Git repository for reference implementation
- Upcoming topics preview
  - Tomorrow: exceptions, extension methods, lambda expressions
  - Friday: comprehensive CRUD project incorporating all learned concepts
- Learning resources
  - HTML reference page created with examples from session
  - MSDN documentation links for object orientation concepts
  - Ready reckoner format for easy memory retention