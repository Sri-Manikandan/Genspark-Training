### Postman Setup

- Key tool for managing and testing API endpoints
- Create workspaces, collections, and environments
- Store base URL as environment variable, reuse across requests
- Save bearer tokens as variables; no need to re-enter per request
- Categorize collections by method type or access level (public/protected)
- Recommended for project demo prep: save all requests in advance

### AutoMapper

- Replaces manual DTO-to-model mapping methods
- Packages: AutoMapper + AutoMapper.Extensions.Microsoft.DependencyInjection
  - Note: older combined package is deprecated
- Setup:
  1. Create MappingProfile class inheriting from Profile
  2. Define mappings in constructor (e.g. RegisterUserRequest → Customer)
  3. Inject via builder.Services.AddAutoMapper(cfg => cfg.AddProfile(new MappingProfile()))
- If DTO and model property names differ, configure explicitly in profile
- “Flattening”: child objects in source can map to flat properties in destination

### Rate Limiting / Throttling

- Built into .NET, no NuGet package required
- Config in Program.cs:
  - Rejection status code: 429 Too Many Requests
  - Fixed window limiter: 5 requests/minute, queue size 2
- Apply per endpoint via attribute using the limiter’s name
- Demo: 6th request within window returns 429; resets after 1 minute
- Rate limit per user/IP; can define multiple named profiles
- Front end should handle 429 with a spinner or wait state

### Idempotency

- Important concept for web APIs, especially payments and transfers
- Repeated identical requests should not create duplicate records
- Example: alert user if same payment amount/recipient submitted twice
- Handling strategy varies by context; no single mandatory approach

### Project Architecture Deliverables

- Submit by Monday/Tuesday next week
- Expected: high-level overview (ER diagram, flow diagram, rough sketch, text/MD file all acceptable)
- Include: tech stack, external APIs, connectivity plan, project scope per stage
- Recommended (not mandatory): use case diagram, sequence diagram, class/interface diagram
  - Sequence diagram mapping to class diagram = well-designed architecture
- Recommended tools: Microsoft Visio, draw.io (free)
- Layered architecture confirmed: repository, service, API layers

### Trainer Notes and Expectations

- All covered concepts (AutoMapper, Postman, rate limiting) mandatory in capstone project
- AI usage: acceptable, but must understand every line generated
- C# basics: strongly recommended to self-study this weekend if uncomfortable
- Next week: a couple of days reserved for full project work (floor simulation)
- Peer teaching sessions planned for next week
  - Volunteers to pick a basics topic (10-15 min each)
  - Topics: JSON, string literals, abstract classes, collections, OOP concepts, etc.
  - Sheet to be shared for topic sign-ups

### Next Steps

- Install and configure Postman; raise IT request if needed
- Pull latest pushed code (AutoMapper + rate limiting implementation)
- Prepare project architecture overview for Monday/Tuesday review
- Sign up for a peer teaching topic on the shared sheet (all participants)
- Review C# basics over the weekend if needed