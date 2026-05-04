### Morning Session | Presidio Training | 2026 — Detailed Notes (Apr 30, 2026)

#### Session Overview & Participation

- Programming training commenced for the Presidio cohort at 9:30 AM
- Mandatory: join using Presidio (office) email ID only
  - Hand-raise verification performed; participants not on office ID were asked to log out and rejoin with Presidio credentials
  - Temporary allowance: a few attendees joined from personal laptops due to office laptop slowness (PG Admin running slowly). They must switch back to office email from tomorrow; peers to leave a chat message on their behalf if chat posting is restricted
- Language support: Tamil, Telugu, Kannada, Malayalam, Hindi. (Humor note: use Chinese if you truly don’t want the instructor to understand)

#### Plan & Methodology

- Start by solving LeetCode problems in any comfortable language; transition to C by end of day
- Emphasis on integrity: no cheating; attempt independently; “train your mind to think”
- Instructor wants to see focused “programming faces” and seriousness during exercises

#### Schedule & Logistics

- Afternoon: instructor may be unavailable for ~1 hour for a college project meeting
  - Will stay connected to the meeting but will not respond during that period; will post a notice before stepping away
- Weekly rhythm: Every Thursday at 4:00 PM (post-break), brief share-out of new learnings from the week
- Upcoming long weekend (3 days): self-study recommended (see Learning Tasks below)

#### Environment & Project Setup (C#/.NET)

- Participants using VS Code should create a console app via CLI:
  - dotnet new console -n <ProjectName>; open the folder in VS Code; run with dotnet run
- Instructor demonstrated in Visual Studio (not VS Code), but codebases are equivalent for this session
- Program structure (non–top-level statements):
  - Entry point in Program.cs with Main
  - Default class access is internal; default member access inside a class is private
  - Prefer minimal access by default; avoid making everything public
- Coding standards & naming conventions:
  - Follow Microsoft (MSDN) conventions; method names start with UpperCamelCase (differs from Python/JavaScript/Java examples)
  - Use clear identifier names; enforce standards during project validation
- Productivity tips:
  - Snippets: cw + Tab expands to Console.WriteLine
  - String interpolation: prefix string with $ to avoid concatenation

#### Runtime Concepts & Best Practices

- Execution engine & memory management:
  - Garbage Collector (GC) generations 0/1/2; new objects enter Gen0; GC invokes when Gen0 fills; unreachable objects are marked and swept; surviving objects are promoted to higher generations
  - OutOfMemory can occur if Gen0/1/2 all hold live referenced objects (e.g., cyclical references retaining objects). Avoid designs that keep unnecessary references alive
- Structured Exception Handling (SEH): try/catch/finally
  - Prefer programmatic guards (e.g., null checks) over broad try/catch for expected conditions
  - Analogy: ATMs often validate late (exception-like flow) vs mobile apps that validate inline; aim for early validation in code when feasible

#### Types & Conversions (C# specifics covered)

- Implicit vs explicit casting:
  - Implicit: int → float allowed
  - Not allowed implicitly: float → int (risk of data loss). Use explicit cast only when you accept truncation/rounding implications
  - Financial examples highlighted the danger of silent truncation/rounding
- Safer numeric conversion patterns:
  - Use Math.Round, Math.Floor, or Math.Ceiling deliberately based on business rules, then convert via Convert.ToInt32(...) if needed
- Boxing / Unboxing and value vs reference types:
  - Value types store the value directly; reference types store an address to data on the heap
  - Console.ReadLine() returns a string (reference type). Converting to int is unboxing to a value type via Convert.ToInt32(...)
- Nullability & defaults:
  - Value types cannot be null unless declared nullable (int?)
  - Use the null-coalescing operator ?? to supply safe defaults (e.g., num = maybeNull ?? 0)
- Parsing APIs and behavior differences:
  - Convert.ToInt32(string) handles null by returning 0 (base value for int)
  - Int32.Parse(string) throws on null or invalid input
  - Int32.TryParse(string, out int result) returns true/false and sets result without throwing; preferred for user input loops
  - Demonstrated input loop enforcing numeric entry using while (!Int32.TryParse(Console.ReadLine(), out num)) { Console.WriteLine("Invalid entry. Please try again."); }
- Numeric limits & overflow:
  - int.MaxValue + 1 cycles to int.MinValue (wrap-around). Use checked { ... } to throw on overflow instead of cycling

#### Guidance on Data Types & Architecture

- Choose data types with end-to-end flow in mind: database compatibility, display, and long-term maintenance
- For constrained IoT devices, custom lightweight types may be preferable to reduce overhead
- Principle: write “mindful code” — own every line; prioritize scalability, performance, and clarity

#### Q&A Highlights (Technology Choices)

- Why C#/.NET for this training?
  - Strong type safety; runtime prevents unsafe implicit conversions; reduces certain classes of defects
  - Security and accountability considerations compared with ecosystems built primarily on loosely typed or heavily fragmented open-source packages
  - Note: TypeScript emerged to add type safety to JavaScript; similar rationale
- Linux/security discussion:
  - Distinction between frequency of attacks and strength of defense; open-source projects may prioritize new features over defensive hardening; robust defense is essential regardless of stack
- Tech stack selection for projects:
  - Client preference comes first; otherwise weigh timeline, complexity, and cost
  - Examples:
    - Quick PDF ingestion/ETL for legal docs: Python for speed of development
    - Enterprise/web+mobile:
      - Front end: React preferred for lightweight and React Native parity; some teams choose Angular for web and React Native for mobile
      - Back end: Java or .NET preferred; Node/Express acceptable when required
      - Data: MongoDB for fluid/rapidly changing domains; RDBMS for user/account data

#### Learning Tasks (before next sessions)

- Study C# fundamentals on MSDN, with focus on:
  - Coding standards and naming conventions
  - Data types, conversions, nullability, and overflow behavior
- Reattempt the provided problem links; implement solutions in C after reviewing C# basics
- Be prepared to share one “new thing learned” at the 4:00 PM Thursday share-out

#### Miscellaneous

- Encouragement to explore 3D printing trends (consumer printers and large-scale builds, e.g., schools/housing) as an adjacent technology interest
- Classroom culture: serious, focused effort; integrity in problem-solving; enjoy coding but be fully present when writing code

