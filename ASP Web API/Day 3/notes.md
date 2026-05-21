### Password Security & Hashing (Deeper Notes)

- Storing clear-text passwords is prohibited; database must hold only derived values.
- Hashing vs encryption:
  - Hashing is one-way and non-reversible; used to verify equality of inputs without revealing the original password.
  - Encryption is two-way (supports decryption); even with salting it can be decrypted if keys/algorithms are exposed, so it’s not preferred for passwords.
  - Team preference: hash passwords to avoid any server-side decryption risk and reduce blast radius if data is leaked.
- Keys, salts, and comparison workflow (as discussed):
  1. Registration: accept username/password; hash password using a key; store {hashed password + key} with the user record in DB.
  2. Login: read stored key for that user; hash the submitted password with the same key; compare hash vs stored hash; if equal → authenticate.
  3. Rationale offered: encryption implies a reversible step; hashing removes that risk. Salt can differentiate identical user-chosen passwords; if used, store per-user salt alongside hash.
- Operational notes called out:
  - If two users choose the same password, adding salt prevents identical stored values.
  - Username should be unique to reliably fetch the correct key/hash at login.
  - Option to auto-generate initial passwords and force reset via emailed link.

### Authentication Methods & OAuth (What was covered)

- Direct (first-party) registration/login: app collects and owns credentials.
- Federated/OAuth (Google/Microsoft/others):
  - Credential custody sits with the provider; our app relies on provider’s assertion.
  - Flow: app ↔ provider handshake; provider returns token + requested claims; app maps provider identity (e.g., OID/SID/GID) to local user/roles.
  - Pros: no repeated sign-ups; reduced credential risk surface; faster UX.
  - Cons: less first-party user data; dependency on provider pricing/availability; possible lock-in.
- Consent & scopes (as seen in MSAL example):
  - Users see a consent screen and may granularly allow sharing (e.g., email, name, phone); some identifiers (unique ID) are mandatory to uniquely identify the user.
  - App chooses which claims to request and how to persist/map them locally for roles/policies.

### Token-Based Authentication (JWT focus)

- JWT structure: header + payload (both readable) + signature (hashed/validated).
  - Typical payload includes subject/unique ID, roles or policy indicators, and exp (expiry).
  - What to embed is a trade-off: include the minimum needed; avoid sensitive content.
- Where tokens live client-side:
  - Options mentioned: session storage, local storage, HTTP-only cookies, or secure vaults.
  - Browser storage is inherently inspectable; vault improves security but adds latency/UX friction. Some apps encrypt the token-at-rest on the client.
- Using tokens on requests:
  - Sent primarily in Authorization header; can also ride in cookies.
  - Backend applies an authentication filter: validate signature/key and check expiry before hitting controller actions.
- Refresh token concept as described:
  - "Main" token may be heavier (policy/permissions). To reduce payload churn, a lighter refresh token is used on repeated calls until expiry.
  - When refresh/main token expires, user may be prompted to sign in again depending on configuration and risk posture.
- Errors and logout handling:
  - 401 when unauthenticated/expired; 403 (forbidden) when authenticated but lacking rights.
  - No server-side revocation list was proposed; logout relies on the frontend deleting stored tokens and redirecting to login.

### Data Models & Relationships (clarified)

- User: username (unique), password hash, per-user hash key, role.
- Customer: name, email, phone, status, date of birth; one-to-one with User.
- DTOs:
  - Registration DTO: username, password, and customer fields.
  - Login DTO: username + password.
- Responses:
  - Registration may return customer ID as success signal.
  - Login returns an auth token (and optionally username); policy/role is interpreted client-side and cached.

### Implementation Walkthrough & Migration Notes

- Controllers:
  - AuthenticationController with POST /register and POST /login.
  - Other controllers protected by an auth filter; login endpoint intentionally left open.
- Filter behavior:
  - On protected routes, backend checks for token presence → validates key/signature and expiry → allows/denies before action execution.
- Repository/EF setup:
  - User and Customer created with one-to-one mapping; Customer.username is populated from created User to keep linkage consistent.
  - Migrations created tables and foreign key constraints successfully after fixes.
- Troubleshooting items encountered:
  - Username binding: ensured Customer.username set from User before save.
  - Date/timestamp: adjusted column type to resolve timestamp-with-timezone mismatch.
  - Constructor/injection: corrected abstract/public constructor mismatch for repository registration.
  - Verified that both User and Customer rows are created during registration.
- Exception handling: added a general catch as a safety net in the auth controller (not typical in production, but used here during bring-up).

### Practical Handling & UX Considerations

- Initial password delivery options: auto-generate and email, masked paper delivery (analogy), or force-reset link immediately after registration.
- Token placement: prefer Authorization header; cookies/session/caching are alternatives with trade-offs (security vs convenience).
- Safe minima in JWT: include only identity and role/policy pointers; keep sensitive details server-side.

### Next Steps (from the session)

- Implement and verify hashing routine end-to-end (registration + login path).
- Finish repository layer wiring and tests.
- Add/verify auth filter on protected endpoints; confirm 401/403 behaviors.
- Decide on client storage strategy (session vs local vs cookie vs vault) and implement consistently.
- Team to pull latest code and review, then proceed to full authentication flow testing.