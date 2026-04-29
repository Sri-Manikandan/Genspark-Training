### Administrative Requirements and Attendance

- GenSpark email ID login mandatory for Teams meetings
  - Must use proper full names in GenSpark email format
  - Attendance tracking directly tied to salary calculation
  - Yesterday was last working day for salary calculation period
- Previous month’s attendance reconciliation challenges
  - Took until 3 AM to complete due to identification issues
  - Participants using personal emails with “strange names”
  - Manual mapping required for each individual
  - Will not repeat this process next month
- Instructor emphasis: “You will not be paid salary” without proper GenSpark ID login
  - This is paid training, not charity service
  - Proper attendance documentation essential

### Database Join Fundamentals

- Real-world necessity for joins
  - Single table cannot contain all required data due to normalization
  - Amazon product display example: discount table, description table, images table
  - Bus booking system: amenities (AC, WiFi, food, blankets) stored separately as multi-value attributes
- Parent-child relationships and participation types
  - Partially participating: some parent records have no child records
  - Fully participating: all parent records have corresponding child records
  - Join type selection based on participation requirements

### Join Types and Usage Patterns

- Cross Join (Cartesian Product)
  - Least used join type for probability calculations
  - Gambling example: two dice combinations for rolling seven (6 possibilities)
  - Digital forensics: fingerprint matching with probability scores
  - No relationship required between tables
  - Result: column addition, row multiplication
  - Customer table (2 columns, 6 rows) × Products table (3 columns, 4 rows) = 5 columns, 24 rows
- Inner Join (Most Common)
  - Fetches only participating records from both tables
  - Default join type when using JOIN keyword
  - Area-Employee example: only areas with employees
  - Performance advantage: uses primary key indexes
- Outer Join Variations
  - Left Outer Join: all records from left table plus matching from right
  - Right Outer Join: all records from right table plus matching from left
  - Full Outer Join: unmatched records from both tables
  - Default OUTER JOIN is LEFT OUTER JOIN
- Natural Join Behavior
  - Automatic joining when column names and data types match
  - Becomes inner join with relationships present
  - Becomes cross join without relationships
  - Not a separate join type but natural joining method
- Self Join Implementation
  - Table joins with itself using aliases
  - Employee-manager hierarchy example with reports_to column
  - Can be inner join (participating employees only) or outer join (include managers)
  - Not a separate join type but joining technique

### Practical Join Examples and Results

- Customer-Order relationship demonstration
  - Inner join produced 830 rows (participating customers only)
  - Left outer join revealed 2 customers who never placed orders
  - Specific query: customer name and order date across tables
- Three-table join sequencing
  - Products → Order Details → Orders progression
  - Sequential approach: join two tables first, then join result with third table
  - Cannot directly join three tables simultaneously
- Join direction consistency importance
  - Left outer join on first pair requires consideration for second join
  - Alternative: reverse order and use right outer join on final step
  - Maintain record preservation throughout join chain

### Ad-hoc Queries vs Stored Procedures

- Ad-hoc query execution process
  1. Compilation (syntax checking)
  2. Execution plan generation
  3. Query execution
- Performance impact of query variations
  - Minor formatting changes (line breaks, spacing) trigger new execution plans
  - Same logical query written differently creates separate plans
  - Repeated compilation overhead for similar queries
- Stored procedure advantages over ad-hoc queries
  - Compile once during creation, not per execution
  - Cached execution plan reused for all calls
  - Bypasses compilation and plan generation steps

### Stored Procedures and Functions in PostgreSQL

- Performance benefits
  - Pre-compilation eliminates repeated syntax checking
  - Cached execution plans improve response time
  - Faster execution through plan reuse
- Security enhancements
  - Parameter-based input prevents SQL injection
  - Example: expected “101” but receives “101; DELETE * FROM users”
  - Encapsulation hides underlying table structure
  - Additional security layer beyond direct table access
- Development efficiency
  - Database specialists write complex queries
  - Application developers execute simple procedure calls
  - Reduces SQL expertise requirements across teams
  - Avoids query complexity for non-database programmers
- PostgreSQL syntax and implementation
  - Language specification: PL/pgSQL
  - Dollar-quoted strings ($$ … $$) for procedure body
  - CREATE PROCEDURE for non-returning operations
  - CREATE FUNCTION preferred for returning result sets
  - RETURNS TABLE syntax for structured data return
- Function execution patterns
  - Procedures: CALL procedure_name(parameters)
  - Functions: SELECT * FROM function_name(parameters)
- Example function structure

### Transaction Management and ACID Compliance

- Transaction necessity for multi-step operations
  - Bank transfer scenario: debit account A, credit account B, log transaction
  - Order processing: reduce inventory, process payment, create shipping entry, empty cart
  - Bus booking: block seat, process payment, reduce available seats, create booking entry
- ACID principle implementation
  - All operations succeed together or none succeed
  - Graceful failure handling prevents partial completion
  - Either COMMIT (execute all) or ROLLBACK (execute none)
- Bank transfer demonstration logic
  - Procedure parameters: from_account (integer), to_account (integer), amount (integer)
  - Current balance validation before processing
  - Minimum balance enforcement (500 rupee threshold)
  - Conditional processing with IF-THEN-ELSE structure
- Transaction flow implementation
  1. BEGIN transaction
  2. INSERT into transaction table
  3. UPDATE account balances
  4. Validate business rules (minimum balance)
  5. COMMIT if valid, ROLLBACK if invalid
- Error handling and logging
  - Transaction table entries required regardless of success/failure
  - Status field indicating “success” or “failure due to insufficient balance”
  - Complete audit trail for all transaction attempts

### Performance Best Practices and Advanced Topics

- Query optimization recommendations
  - Prefer joins over subqueries for better performance
  - Primary keys provide automatic indexing benefits
  - Indexes enable faster data retrieval (like book index vs page-by-page search)
- Intermediate result management
  - Common Table Expressions (CTEs) for temporary query results
  - Temporary tables for cross-session data persistence
  - CTE usage: couple of queries on complex join results
  - Temp table usage: extensive analytics requiring multiple query sessions
- Memory vs storage considerations
  - CTEs exist in memory during query execution
  - Temp tables persist until session logout
  - Choose based on reuse frequency and session requirements