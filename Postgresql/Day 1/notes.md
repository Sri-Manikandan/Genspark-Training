#### Database Design Fundamentals

-   Database design was positioned as a foundational part of application development, with the claim that if the database is designed correctly, the project is already significantly complete.
    
-   The session focused on how to **visualize data** before writing queries, starting from an RDBMS perspective.
    
-   RDBMS was described as a method introduced by **E.F. Codd** at IBM, based on splitting data across multiple tables and creating relationships between them.
    
-   The trainer emphasized that understanding database design is still important even when AI can generate code, because architectural choices require human thinking.
    

#### Entity Identification

-   An **entity** was defined as any object in a given project context that has meaningful description.
    
-   Whether something is an entity depends on the **context** of the application. For example, a customer may be an entity in a shop system because it has ID, name, and phone number, while an employee may not matter if employee details are irrelevant to the scenario.
    
-   Identifying entities was framed as an **architectural decision**, not a trivial coding task.
    
-   The trainer noted that being able to identify entities clearly is closer to the work of an architect than routine programmer work.
    

#### Attributes and Context-Driven Design

-   After identifying entities, the next step is deciding which **descriptions are relevant** for the current context.
    
-   For a customer in a billing/shop example, relevant attributes included:
    
    -   name
        
    -   phone number
        
    -   email
        
    -   address
        
-   Attributes such as favorite color were used as examples of information that may exist but are **not relevant** to the current use case.
    
-   Gender and age were described as potentially useful later for analytics, but not necessary for a simple billing system.
    
-   Attributes were explained as the descriptions/adjectives associated with an entity that matter in context.
    

#### Key Attribute / Primary Key

-   A **key attribute** is an attribute that uniquely identifies each row in an entity.
    
-   This was also described in “professional” terms as the attribute which, when changed, changes the whole tuple/row.
    
-   “Primary key” and “key attribute” were treated as acceptable terms for the same idea.
    
-   Properties of a key attribute:
    
    -   must be unique
        
    -   cannot be null
        
    -   is used to determine the rest of the row
        
-   The session stressed that **null does not mean empty**; null means **undefined / not yet determined**.
    
-   Because a key attribute must identify data, it cannot be null and cannot contain duplicates.
    

#### Strong vs Weak Entities

-   A **strong entity** has a key attribute.
    
-   A **weak entity** does not have a key attribute.
    
-   The trainer strongly recommended creating strong entities in almost all practical cases.
    
-   One reason given was performance: when a key attribute is created, it is indexed, which improves search, grouping, counting, and lookup speed.
    

#### Attribute Types

-   The session classified attributes into four major types.

1.  **Simple attribute**
    
    -   Contains one unit of data.
        
    -   Cannot be meaningfully split further.
        
    -   Examples given: customer ID, product ID, product name.
        
2.  **Complex attribute**
    
    -   Contains more than one unit of data.
        
    -   Can be split meaningfully into a finite number of columns.
        
    -   Examples given:
        
        -   customer name → first name, middle name, last name
            
        -   address → door number, building name, street, state, country
            
3.  **Multi-value attribute**
    
    -   Contains more than one unit of data.
        
    -   Cannot be split into a fixed, defined number of columns.
        
    -   Examples given:
        
        -   phone numbers
            
        -   emails
            
        -   skills
            
4.  **Derived attribute**
    
    -   Computed from another attribute.
        
    -   Examples given:
        
        -   age from date of birth
            
        -   years of experience from joining date
            

#### Relationships Between Entities

-   Relationships were described as the **verb** connecting entities, while entities are like nouns and attributes are like adjectives.
    
-   Relationship cardinality covered:
    
    -   **one-to-one**
        
    -   **one-to-many**
        
    -   **many-to-many**
        
-   Examples:
    
    -   one customer → many bills
        
    -   one bill → many products, and one product → many bills, so bills/products is many-to-many
        
    -   customer ↔ address can be treated as one-to-one in a simplified design
        

#### Participation and Parent/Child Tables

-   Participation was explained as:
    
    -   **full participation**: every row in one table has a corresponding related row in another table
        
    -   **partial participation**: some rows may not have any related row
        
-   Example: not every product must appear in billing details, because some stocked products may never be sold. That makes the participation partial.
    
-   A **parent table** is where the entity originates and is identified.
    
-   A **child table** is where that entity is referenced/used.
    

#### Master Tables vs Transaction Tables

-   The trainer introduced a practical split between **master tables** and **transactional tables**.

**Master tables**

-   Hold values that mostly stay stable.
    
-   Are not edited frequently.
    
-   Are typically reference/configuration data.
    

Examples mentioned:

-   product
    
-   customer
    
-   supplier
    
-   roles
    
-   statuses
    
-   destinations
    
-   bus operators
    

**Transactional tables**

-   Hold day-to-day operational activity.
    
-   Are updated frequently.
    

Examples mentioned:

-   bookings
    
-   orders
    
-   billing details
    
-   refunds
    
-   In the bus booking example, statuses such as active, deactivated, deferred, and verified were described as candidates for master tables.
    
-   Destinations and roles were also described as master data.
    
-   Master table data was described as something typically fetched when a screen loads, often in a single API response to populate dropdowns and selection lists.
    
-   The trainer said fetching all required master data in one request is acceptable because it is lightweight text/reference data and avoids many small API calls.
    

#### Soft Delete and Status Modeling

-   Instead of physically deleting records such as customers, the trainer recommended using **status-based soft delete** patterns.
    
-   Reason given: deleting a customer record directly can break the history of related bookings or billing details.
    
-   Example statuses discussed:
    
    -   active
        
    -   deferred
        
    -   deactivated
        
    -   verified
        

#### Why AI Alone Is Not Enough

-   The trainer repeatedly emphasized that AI can help generate code, but developers still need to understand business logic and architecture.
    
-   The trainer’s position was that relying only on AI can weaken logical thinking if developers stop practicing problem solving themselves.
    
-   The recommendation was to use AI for help, but still write and validate logic independently to train the mind.
    
-   Learners were encouraged to think through the business logic behind everyday applications and imagine how the underlying tables would be designed.
    

#### Normalization Overview

-   The session covered normalization up to **Third Normal Form (3NF)**.
    
-   The reason given for stopping at 3NF was that the trainer considered it the last stage with a clear practical roadmap to achieve.
    
-   The overall purpose of normalization was described as:
    
    -   reducing redundancy
        
    -   improving consistency
        
    -   avoiding insert, update, and delete anomalies
        

#### First Normal Form (1NF)

A table is in 1NF when:

-   it has a key attribute
    
-   each column contains similar type of data
    
-   it has no multi-value attributes
    
-   “Similar type of data” was explained as choosing and sticking to a proper data type for each column.
    
-   To remove a multi-value attribute, the trainer showed that you move that attribute into a separate table and convert repeated values into **multiple rows** instead of multiple comma-separated values in one column.
    
-   Example used:
    
    -   customer with multiple phone numbers
        
    -   solution: create a customer-phone table with rows like customerID + phoneNumber
        
-   This leads to the need for a **composite key**, because customerID alone may repeat and phoneNumber alone may also repeat across people.
    

#### Composite Key

-   A **composite key** is when more than one attribute together forms the primary key.
    
-   Composite keys were introduced as a common consequence of moving multi-value attributes into separate tables in 1NF.
    

#### Second Normal Form (2NF)

-   2NF applies only after the table is already in 1NF.
    
-   The trainer said 2NF matters only for tables with a **composite key**.
    
-   Rule: **no partial dependency**.
    
-   Partial dependency was defined as a non-key attribute depending only on **part** of the composite key rather than the whole key.
    

Example:

-   In a supplier-product relationship table:
    
    -   supplierID + productID could form the composite key
        
    -   product description depends only on productID, not on supplierID + productID together
        
-   Therefore, product description should be removed into a separate product table.
    

#### Third Normal Form (3NF)

-   3NF requires the table to already be in 2NF.
    
-   Rule: **no transitive dependency**.
    
-   Transitive dependency was defined as a non-key attribute depending on another non-key attribute, which in turn depends on the key.
    

Example:

-   customer → area
    
-   area → zip code
    
-   therefore zip code is not directly determined by customer, but by area
    
-   Solution:
    
    -   move area and zip code into a separate table
        
    -   reference area from customer/employee table
        

#### Worked Example: Employee / Skills / Area

The trainer used an example with:

-   employee ID
    
-   employee details
    
-   skills
    
-   skill level
    
-   skill description
    
-   area
    
-   zip code
    

Normalization path:

-   Start with a single source table containing all fields.
    
-   Move multi-value skills into a separate employee-skill table for 1NF.
    
-   Move skill description into a separate skills table for 2NF, because it depends on skill rather than the full employeeID+skill key.
    
-   Move area/zip code into an areas table for 3NF, because zip code depends on area.
    
-   Final design produced four related tables:
    
    -   areas
        
    -   skills
        
    -   employees
        
    -   employee\_skills
        

#### Redundancy and Consistency

-   A major theme in the explanation was that **redundancy causes inconsistency**.
    
-   The trainer used examples like addresses being stored in many places and becoming inconsistent over time.
    
-   Normalization was framed as the mechanism for keeping redundancy low so consistency stays higher.
    

#### SQL / DDL Best Practices

The practical SQL guidance included:

-   Create parent/master tables first, then child tables.
    
-   Start “from the top” to avoid later ALTER TABLE work.
    
-   Use proper **constraint names** instead of relying on engine-generated defaults.
    
-   Consider:
    
    -   primary keys
        
    -   foreign keys
        
    -   not null
        
    -   unique
        
    -   check constraints
        
    -   referential integrity actions like ON DELETE / ON UPDATE
        
-   Think about business logic in constraints, such as valid employee age ranges or skill level bounds.
    
-   Prefer deciding nullability and data types at table creation time rather than changing them later.
    
-   ALTER TABLE was described as useful for:
    
    -   adding/removing columns
        
    -   changing constraints
        
    -   modifying nullability
        
    -   sometimes changing data types, though the trainer recommended avoiding that after data insertion
        
-   DROP TABLE on a parent table requires handling child references first.
    

#### NoSQL vs RDBMS Thinking

-   The session briefly contrasted RDBMS with **NoSQL**.
    
-   NoSQL was suggested for highly fluid or irregular structures where attributes vary heavily between records.
    

Examples given as better NoSQL candidates:

-   Instagram post data
    
-   Facebook post data
    
-   Amazon product descriptions, because a table, a mobile phone, and a mouse all need very different description structures
    
-   The trainer encouraged learners to think about whether a scenario is better suited to SQL/RDBMS or NoSQL based on rigidity vs flexibility of the data model.
    

#### Assignments and Follow-ups

-   Learners were asked to revisit their existing project tables and check:
    
    -   whether master tables were properly separated
        
    -   whether the design was normalized
        
    -   whether statuses/roles should become separate tables
        
-   A post-lunch task was assigned to review project table design from the normalization perspective.
    
-   Two assignments were mentioned:
    
    -   one more guided assignment with clearer database requirements
        
    -   one more abstract assignment requiring learners to design the tables themselves
        
-   The trainer also said additional learning links would be shared, and a **Google Classroom** was created for that purpose.