### Meeting: Morning Session | Presidio Training | 2026

**Date/Time:** Wednesday, April 22, 2026, 9:30 AM **Note:** No session Thursday, April 23, 2026 due to Tamil Nadu elections

### Training Program Overview

- 13-week full-stack development program (3 months + 1 week)
- Facilitator: Gayathri Mahadevan (22 years industry experience, 44 years old)
- Assistant: Ponmalar joins post-lunch for hands-on support
- Daily structure:
  - Morning: Learning/concept delivery
  - Afternoon: Hands-on implementation
- Cameras strongly encouraged during delivery sessions
  - Enables better interaction and comprehension monitoring
  - Facilitator can gauge understanding through facial expressions
  - Creates more comfortable learning environment
- Leave policy: Strong recommendation to avoid taking leave
  - Missing single day disrupts continuity
  - Heavy daily content volume makes catch-up extremely difficult
  - Even comfortable topics delivered from industry perspective
- Recording policy: Available only with justification, not automatic access
  - Live participation essential over passive video watching
  - 4+ hour recordings impractical for effective learning
- Interactive expectations:
  - Unmute for responses when asked
  - Use chat for thumbs up/heart reactions as requested
  - Raise hand feature for questions
- AI integration throughout: Reverse engineering approach
  - Generate complete application first using AI
  - Learn by identifying daily topics within generated code
  - Understand AI capabilities and limitations through comparison

### Tech Stack Details

**Core Technologies:**

- Database: PostgreSQL
- Backend: .NET Web API
- Frontend: Angular
- Cloud: Azure

**Additional Components (Later Phases):**

- Azure Event Grid for messaging
- Docker containerization
- Azure Kubernetes Service (AKS) for orchestration
- CI/CD pipeline implementation
- Network configuration and security

**Development Approach:**

- ORM: Code-first approach (primary focus)
- Database-first approach (walkthrough only)
- Non-functional requirements emphasis:
  - Performance optimization
  - Security implementation
  - State management
  - Locale and time zone considerations

### Daily Flow and Logistics

**Break Schedule:**

- Morning break: ~11:00-11:30 AM
- Lunch: 1:00-2:00 PM (switch to afternoon bridge)
- Evening break: ~4:00 PM (30 minutes)

**Meeting Etiquette:**

- Camera on during delivery sessions preferred
- Message in chat if stepping away >5-10 minutes
- Brief absences (washroom) don’t require notification
- Use personal or office laptop as comfortable
- Must join with Presidio IDs for proper identification

**Administrative Setup:**

- Create individual GitHub repository for all session work
- Malar to collect: name, email, phone number, GitHub ID
- Google Classroom registration coming next week
- Day-wise folder structure recommended

### Learning Methodology

**Reverse Engineering Approach:**

- Generate complete application using AI on day one
- Daily learning: Map delivered topics to generated code sections
- Focus on understanding code functionality vs. typing/syntax
- Learn effective AI prompting and limitation management
- Identify where to trust vs. not trust AI assistance

**Learning Sequence:**

1. Database design (30% of application success)
   - Proper data structure enables business logic
   - Understanding data relationships and joins
2. Backend development
   - Business logic implementation
   - API endpoint creation
3. Frontend design first, then coding
   - Understanding data presentation requirements
   - UI/UX planning before implementation

**Industry Perspective Examples:**

- E-commerce site data relationships
- Performance requirements (sub-minute page loads)
- Next-generation user expectations
- Security considerations (chatbot vulnerabilities)

**PLT Exercises:**

- Programming Logic and Techniques challenges
- Logical problem-solving with code solutions
- Discussion of optimal vs. suboptimal approaches

### Project Assignment: Bus Booking System

**Deadline:** Friday morning, April 24, 2026 **Deployment:** Local system only (no cloud deployment required) **Work Style:** Individual initially to assess individual capabilities

#### User Role Requirements

**Search and Booking:**

- View buses without login requirement
- Search with source, destination, date
- Fuzzy logic search for locations (RedBus-style autocomplete)
- Optional round-trip functionality
- Display available seat count in bus listings
- Seat reservation with visual layout
- Seat blocking mechanism: 5-10 minute grace period for payment
- Login required before final booking
- Multi-seat booking capability with passenger details per seat:
  - Name, age, gender for each passenger
- Payment integration:
  - Dummy gateway acceptable
  - Stripe/Razorpay if within free tier limits
  - No real money transactions required
- Ticket download functionality
- Email confirmations via SMTP (no cost)
- Optional SMS confirmations (cost consideration)

**Account Management:**

- User registration (no approval required)
- Profile management (email or phone identification)
- Booking history views:
  - Upcoming travel
  - Past travel history
  - Cancelled bookings
- Cancellation functionality with configurable rules:
  - Example: No cancellation <24 hours before departure
  - Refund percentages: 12 hours = 50%, 24 hours = 80%
  - Business rule flexibility

#### Bus Operator Role Requirements

**Registration and Approval:**

- Register as customer first
- Request operator role elevation
- Admin approval required for role activation
- Cannot operate until approved

**Bus Management:**

- Add new buses (requires admin approval per bus)
- Cannot add new routes (admin-only function)
- Operate only on existing admin-created routes
- Temporarily remove buses (maintenance, private charter)
- Permanently remove buses (no approval required for removal)
- Bus identification via registration number (vehicle plate)
- Set uniform pricing per bus (no dynamic/seat-specific pricing)

**Operational Requirements:**

- Provide office addresses in operational locations
- Office addresses auto-map to pickup/drop points for routes
- Set bus schedules and timings (operator choice)
- View bookings made on their buses
- Revenue tracking and reporting
- Seat layout options:
  - Upload custom layouts
  - Choose from predefined layouts
  - Simple capacity-based generation acceptable

#### Admin Role Requirements

**Operator and Bus Management:**

- Approve operator role requests
- Approve new bus additions
- Enable/disable operators
- View platform-wide revenue and booking analytics

**Route Management:**

- Add sources and destinations (places/cities/districts)
- Create point-to-point routes between locations
- No intermediate stops or multiple pickup points
- Operator office addresses automatically become pickup/drop locations

**Platform Configuration:**

- Set convenience/platform fee (fixed amount or percentage)
- Revenue oversight across all operators

**Operator Disablement Process:**

- Notify operator of deactivation and reason
- Email all affected customers about cancellations
- Inform customers of full refund processing
- Optional enhancements:
  - Suggest alternative operators with available seats
  - Provide discount coupons for rebooking
  - Terms and conditions acknowledgment during registration

### Technical Requirements

**Security and Data:**

- Password encryption mandatory (no plaintext storage)
- Concurrency handling for seat booking conflicts
- Microsecond-level seat blocking for simultaneous users
- Multiple buses allowed per route with different timings

**Communication:**

- Email notifications via SMTP (no additional cost)
- SMS notifications optional due to cost considerations
- Booking confirmations and cancellation notices

**Deployment and UI:**

- Local deployment sufficient for assignment
- Decent UI baseline required
- Beautiful UI welcomed but not mandatory
- No MFA implementation required
- Single Sign-On optional if free tier available

### Deadline and Expectations

**Timeline:** Complete by Friday morning, April 24, 2026 **Work Approach:**

- Individual work initially to assess personal capabilities
- Post-lunch screen sharing sessions to observe development approach
- No judgment on current skill level - focus on learning approach
- Freestyle working methodology

**Tool Usage:**

- Any AI tools permitted (Cursor, Claude, etc.)
- Any development environment acceptable
- Manage token usage within free tier limits
- Personal or office laptop flexibility

**Assessment Focus:**

- Development approach and problem-solving methodology
- Not final output quality or completeness
- Understanding of requirements and technical decisions

### Optional Enhancements

- Round-trip booking functionality
- Female traveler seat indicators (pink highlighting)
- Alternative operator suggestions on cancellations
- Discount coupon system for service disruptions
- Enhanced seat layout rendering
- Single Sign-On integration (no MFA)
- Pickup point selection within cities

### Next Steps

**Immediate Actions:**

- Create individual GitHub repository for session work
- Prepare for afternoon screen sharing sessions
- Begin requirement analysis and solution visualization
- Optimize AI prompting strategy to minimize token usage

**Upcoming Setup:**

- Google Classroom registration (next week)
- Ensure Presidio ID usage for all meetings
- Await data collection sheet from Malar

**Development Strategy:**

- Visualize complete solution before coding
- Think destination-first rather than source-first
- Pull yourself up to the solution rather than pushing from current state
- Focus on understanding over implementation speed