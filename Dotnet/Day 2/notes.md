# Database scafolding
```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=bankingdb;Username=postgres;Password=Poornima290178@" Npgsql.EntityFrameworkCore.PostgreSQL -o models
```

# Notes
### 

- Android 17 announced as “biggest updates to Android ever” - high bar to set
  - Not visual design changes but significant added features
  - Gemini interface integration with “sparklier” UI elements
  - Concept UI subject to change - shouldn’t rely too heavily on current design
  - Performance improvements noted as “a lot faster at the active effort”
- Services integration examples: Gmail, Photos, etc.
  - Part of broader Google ecosystem announcements
  - Timeline referenced as “this week’s” major release cycle

### Database Design Best Practices

- Database design must be completed and frozen upfront before development
  - 60% of project success: correct database design + Figma frontend design
  - 40% remaining: business logic implementation
  - “If you get these two right, the project will be 60% over”
- Sprint-based incremental development acceptable
  - Module-by-module creation allowed after overall design freeze
  - Don’t need “whole database design at one go” but designing must be upfront
- Client requirement changes inevitable due to communication gaps
  - “Most of them do not know how to communicate the requirement”
  - Misinterpretation major cause of rework and project delays
  - Cost analysis required for change requests
  - Projects sometimes transferred to new vendors when changes too extensive
- Data type evolution example: enum ordering disaster
  - Initially used integer enum (0,1,2) for customer status
  - Database clustering changed order: available products showed as unavailable
  - “We were showing all products which were not available as available”
  - Solution: created master table instead of relying on enum values
  - Lesson: minimal data type changes acceptable, avoid drastic redesigns
- Client IT teams and business analysts handle requirement documentation
  - Larger companies have clear IT wings with domain expertise
  - Business analysts create requirement documents covering all needs
  - Architect’s role: select appropriate technology within client constraints

### Entity Framework Database-First Approach

- Scaffolding process from existing DummyDB database
  - Command: Scaffold-DbContext with connection string and provider
  - Provider specification required (differs by database type)
  - Optional: -OutputDir Models to organize generated files
- Auto-generation creates comprehensive project structure
  - Models folder with all table classes
  - Context class (DummyDBContext) with connection handling
  - Fluent API configuration for relationships and constraints
  - Foreign key constraints: “OnDelete.ClientSetNull”
  - All table relationships and navigation properties
  - Views included (e.g., ProductSales1997 view scaffolded automatically)
- Best use cases for database-first approach
  - Existing database migrations (Oracle/Java to .NET)
  - When database already validated and stable
  - “No meaning in starting code-first when you already have valid database”
- Context optimization recommendations
  - Make context read-only for faster memory access
  - “We are not going to change the context, not assign to something else”
  - Constructor injection pattern for dependency management
- Scaffolding appears “like magic” but requires database expertise
  - All tables, relationships, constraints automatically mapped
  - “Either you write code here or write code in database - it’s all the same”

### Stored Procedures & Transactions

- PostgreSQL stored procedure syntax demonstrated
- Entity Framework Core execution pattern
  - context.Database.ExecuteSqlInterpolated($"CALL add_account({account.AccountNumber}, {account.Balance})")
  - No SaveChanges() required for direct SQL queries
  - “Direct SQL query - you don’t have to execute save changes”
- Data type alignment issues surfaced
  - Float vs. double precision mismatch in database vs. C#
  - Recommendation: align types between database and application models
- Upcoming transaction workflow design
  - Transaction table: transaction_id, from_account, to_account, amount
  - Separate stored procedure needed for balance updates
  - Try/catch implementation recommended for error handling
- Read-only context pattern for stored procedure execution
  - Performance benefit: faster memory access
  - Constructor injection for context management

### Integration & Architecture Considerations

- Micro-frontend state management challenges
  - Multiple web applications requiring shared state
  - “Two different memories - how do you transfer values between them?”
  - Solutions: common container for state transfer or async messaging
  - Technology options: Kafka topics, event grid for cross-application communication
- Client integration requirements often unforeseen
  - Existing modules need incorporation into new systems
  - Technology boundaries create integration constraints
  - Architecture decisions must account for client’s existing infrastructure

### Real-World Case Study: Insurance Claim Automation

- Original manual process: email-based claim handling
  - Common email ID for all customer claims
  - Excel sheet tracking with manual number assignment
  - Tiered officer structure: junior ($0-5K), intermediate ($5-10K), senior ($10K+)
- Automated pipeline implementation
  - GraphQL for mail processing and auto-reply system
  - AI-triggered acknowledgment with claim number generation
  - Async communication chain: mail → database entry → representative allocation
- Queue management algorithm developed
  - Maximum 3 claims in queue per representative
  - Idle-time-based allocation: longest idle rep gets next claim
  - Overflow policy: lower-value claims escalated to higher-tier reps when queues full
  - “Higher value reps can handle lower value, but not vice versa”
- Technology decision rationale
  - Kafka chosen over cron jobs for multi-directional communication
  - Event grid considered as alternative
  - “One Kafka could shoot mail, shoot requirement to rep, and do background processes”
- Business logic gaps identified proactively
  - Client couldn’t foresee queue overflow scenarios
  - Solution prevented customer lawsuit situations through algorithm tweaks

### Next Topics & Logistics

- Planned session coverage: pagination, filtering, stored procedures, transactions
- Case study document preparation
  - Will be uploaded to Google Classroom and code shell
  - Time allocated for reading and Q&A before session end
- Session break taken and resumed at 11:30
- Exam schedule accommodations noted for June first week