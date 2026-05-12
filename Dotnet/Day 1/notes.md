### .NET Framework vs .NET Core Historical Context

- .NET emergence and market positioning
  - .NET was the last major new technology invention to enter the market
  - Java dominated web development when .NET launched, with strong enterprise solutions and chat applications
  - Microsoft’s earlier technology stack (VB, C++, ASP) could not compete effectively with Java’s capabilities
  - Initial .NET Framework launch was considered a failure due to Java’s market dominance
- Evolution to .NET Framework success
  - Microsoft revamped approach with interoperability as key differentiator
  - Interoperability allowed applications in different .NET languages to coexist and interact
  - All .NET languages compile to Common Intermediate Language (CIL) for unified execution
  - Supported 16+ programming languages including C#, VB, F#, JavaScript, C++
- Compilation pipeline architecture
  - Source code (C#, VB, etc.) processed by respective compilers (CSC for C#, javac for Java, gcc for C)
  - Compilers generate intermediate code: object code (.obj) for C/C++, bytecode (.class) for Java, CIL (.dll/.exe) for .NET
  - Runtime environments process intermediate code: system assembler for C/C++, JRE for Java, CLR for .NET
  - Final output is machine language executable code across all platforms
- .NET Core as open source evolution
  - Created to compete with open source projects when Microsoft couldn’t maintain market position
  - Available on Microsoft GitHub as fully open source project
  - Current market version: .NET 8 with Long Term Support (LTS)
  - Always choose LTS versions for better resource availability and community support

### **Entity Framework Core Overview and Approaches**

- EF Core positioning as ORM solution
  - Object Relational Mapping tool for Microsoft .NET Core specifically
  - Distinct from original Entity Framework (tied to .NET Framework)
  - Automatically generates database queries vs manual ADO.NET approach
  - When learning online, specifically search for “Entity Framework Core” materials
- Development approach methodologies
  - Code First Approach: Create application classes first, generate database tables from models
  - Database First Approach: Start with existing database, generate model classes from schema
- Approach selection criteria
  - Code First appropriate for: new projects from scratch, better programmer control over database design, database script versioning capabilities
  - Database First appropriate for: existing projects with established databases, migration projects, production databases already in place
  - Strong database teams may prefer Database First; fresh projects benefit from Code First control

### **PostgreSQL Setup and Package Management**

- Required NuGet packages for PostgreSQL integration
  - Npgsql.EntityFrameworkCore.PostgreSQL: PostgreSQL-specific EF Core implementation
  - Microsoft.EntityFrameworkCore.Tools: CLI command execution capabilities
  - Package version alignment: use major version matching .NET version (8.x packages for .NET 8)
  - Always select latest stable patch version within major version (e.g., 8.0.11)
- Development environment considerations
  - Visual Studio: Package Manager Console available for direct package installation
  - Visual Studio Code: Requires CLI commands and local EF Core tools installation
  - Installation commands provided for both environments with version specifications
  - Tools package enables add-migration and update-database commands

### **DbContext Design and Configuration**

- BankingContext class implementation
  - Inherits from DbContext base class from Entity Framework Core
  - Override OnConfiguring method to customize database connectivity
  - UseNpgsql method specifies PostgreSQL as database provider
  - Connection string targets “BankingDB” database with hardcoded credentials
  - Hardcoding acceptable for learning phase; production apps must use appsettings.json
- DbSet property configuration
  - DbSet Customers property indicates table creation requirement
  - Property name becomes table name in database (pluralized convention)
  - EF Core automatically identifies “Id” properties as primary keys
  - DbContext acts as bridge between application and database operations

### **Migration Workflow and Version Control**

- Migration generation process
  - add-migration command scans project for DbContext classes
  - Validates connection string availability and DbSet property definitions
  - Generates migration class inheriting from Migration with timestamped filename
  - Uses Fluent API to create table creation scripts with primary key and identity configuration
- Model evolution and incremental changes
  - Adding properties (e.g., Status to Customer) generates ALTER TABLE migrations
  - Migration compares previous migration state with current model definitions
  - update-database applies SQL scripts to database and updates Migration History table
- Migration management and best practices
  - Migration History table maintains applied migration records with timestamps
  - ContextModelSnapshot tracks current model state for comparison
  - Unique migration class names required (cannot duplicate within namespace)
  - Reversion capabilities: Update-Database to previous migration, Remove-Migration before applying
  - Planning emphasis: design database schema early across sprints to avoid nullability and foreign key complications

### **Model Design and CRUD Operations Demo**

- Customer model implementation
  - Properties: Id (auto primary key), Name, PhoneNumber, Email, DateOfBirth
  - Public class accessibility required for Entity Framework processing
  - Standard C# property syntax with appropriate data types
- Account model with relationships
  - AccountNumber with [Key] attribute for non-standard primary key
  - CustomerId as foreign key integer property
  - Balance (decimal), LastAccess (DateTime), AccountStatus properties
  - Navigation property: Customer object for relationship traversal
  - [ForeignKey(“CustomerId”)] attribute specifies foreign key column
  - Alternative: naming convention (CustomerId) automatically inferred when Customer table exists
- CRUD operation implementations
  - Insert: Create Customer object, add to context.Customers collection, call SaveChanges()
  - Update: Fetch with FirstOrDefault by Id, modify properties, use Update() method or Entry modification
  - Read: Direct enumeration of context.Customers (no SaveChanges required)
  - Entity state transitions: Added → Unchanged (insert), Modified → Unchanged (update)

### **Data Type Mapping and PostgreSQL Compatibility**

- DateTime mapping challenges encountered
  - PostgreSQL timestamp with time zone incompatible with C# DateTime
  - Error: “The CLR type ‘DateTime’ with the kind ‘Unspecified’ cannot be mapped to PostgreSQL type ‘timestamp with time zone’”
  - Resolution approaches: Fluent API HasColumnType(“timestamp without time zone”) or DateTimeOffset usage
- Migration handling for data type changes
  - Generated migration shows data type conversion from timestamp with time zone to CLR DateTime
  - update-database applies type conversion with potential data loss warnings
  - Demonstrates importance of proper type mapping planning in initial design

### **Database Planning and Production Considerations**

- Strategic database design approach
  - Plan master tables vs transactional tables across multiple sprints
  - Identify foreign key relationships and nullability requirements early
  - Incremental migrations preferred over frequent reversions
  - Module-wise planning acceptable for large ERP projects
- Production environment constraints
  - Never delete production data for migration compatibility
  - Handle nullability changes through data backfills or staged migrations
  - Plan NOT NULL constraints carefully when existing data contains null values
  - Example scenario: changing nullable column to NOT NULL requires data cleanup first
- Performance and architectural considerations
  - EF Core provides productivity but carries resource overhead
  - Some teams choose micro-ORMs (Dapper) or ADO.NET for lighter footprint
  - EF Core improvements: better stored procedure support, enhanced transaction handling
  - Microservice architectures may prioritize EF Core convenience over database performance optimization

### **Next Session Planning and Advanced Topics**

- Fluent API deep dive scheduled
  - Relationship configuration beyond basic foreign keys
  - Custom constraints and validation rules
  - Table and column naming customization
  - Advanced schema tuning techniques
- Remaining implementation topics
  - Complete remaining model classes for banking application
  - Explore pagination techniques (database vs application level)
  - LINQ integration and query optimization strategies
  - Delete operation implementation and best practices