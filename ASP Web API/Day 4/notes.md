### JWT Token Implementation

- Created comprehensive token service with complete authentication flow
  - TokenRequest DTO contains three fields: username, role, given name
  - Secret key stored in appsettings.json under “JWT:Key” section (will migrate to Azure Key Vault later)
  - Additional JWT configuration: issuer name, audience (optional), validity duration in minutes
- Token creation process detailed:
  - Claims collection forms payload: NameIdentifier (username), Name (given name), Role
  - Claims represent “who the user claims to be” when presenting token
  - SymmetricSecurityKey created via Encoding.UTF8.GetBytes(key) from configuration
  - SigningCredentials uses HmacSha256 algorithm with the symmetric key
  - JwtSecurityToken combines issuer, claims, expiration (DateTime.Now.AddMinutes), signing credentials
  - JwtSecurityTokenHandler.WriteToken() generates final JWT string
- Security model explained:
  - Payload visible to anyone (can decode at jwt.io)
  - Signature validation requires secret key - prevents token forgery
  - Only applications with correct secret can validate token authenticity

### Authentication Setup & Configuration

- Package installation: Microsoft.AspNetCore.Authentication.JwtBearer for JWT support
- Program.cs authentication configuration:
  - builder.Services.AddAuthentication() with options setup
  - DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme
  - DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme
- TokenValidationParameters comprehensive setup:
  - ValidateIssuer = true with ValidIssuer from configuration[“JWT:Issuer”]
  - ValidateIssuerSigningKey = true with IssuerSigningKey from configuration[“JWT:Key”]
  - ValidateLifetime = true (uses expiration from token claims)
  - ValidateAudience = false (audience validation optional, not implemented)
- Critical middleware ordering: app.UseAuthentication() before app.UseAuthorization()
- Controller protection via [Authorize] attribute on methods/controllers
  - Returns 401 Unauthorized when no valid token provided

### Testing & Implementation Issues

- Postman configuration for token testing:
  - Authorization tab → Bearer Token type
  - Paste JWT token from login response into token field
- Swagger UI integration for easier testing:
  - Added OpenAPI security definition in Program.cs
  - Enables “Authorize” button in Swagger interface
  - Token format: “Bearer {token}” automatically injected into Authorization header
- Debugging incident resolved:
  - Initial “invalid token” error caused by extra space in copied secret key
  - Corrected key in appsettings.json fixed validation immediately
- Token lifecycle management:
  - Expiration triggers 401 Unauthorized, requires user re-login
  - Current demo: 60-minute validity period
  - Enterprise options: refresh tokens for extended sessions or longer durations

### Q&A Highlights & Technical Clarifications

- JWT audience concept: defines “who is consuming the token” (customer vs admin applications)
- Enterprise security practices:
  - Secret keys generated randomly and stored in secure vaults (not hardcoded)
  - Current demo approach only for learning purposes
- Version compatibility issues addressed:
  - Project built on .NET 8, attendee using .NET 10 causing conflicts
  - Solution: upgrade EF Core versions to match .NET version
- Hashing algorithm flexibility:
  - Demo uses SHA-256 for consistency with registration process
  - bcrypt acceptable alternative based on data criticality and enterprise requirements
- Database approach: either code-first or database-first acceptable for projects

### Next Steps & Schedule

- Next week’s curriculum roadmap:
  - Filters and pipes in Web API for request/response processing
  - Application deployment strategies and implementation
  - SignalR for real-time two-way communication
  - AutoMapper and mapping tools for object transformation
  - Dynamic endpoints with parameter-based response routing
- Project deliverables and timeline:
  - Add JWT authentication to individual capstone projects immediately
  - Backend evaluation scheduled for early June (first week)
  - Parallel development: continue capstone while learning new concepts
- Friday session kept lighter:
  - No formal assignment given
  - Focus on exploring JWT concepts and implementation
  - Three attendees scheduled to present new articles/contributions