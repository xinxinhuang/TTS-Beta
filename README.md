# TeeTime Management System

A comprehensive golf course management system for handling tee times, member reservations, events, and staff operations.

## Key Features
- **Tee Sheet Management**
  - Weekly view of available tee times
  - Event creation and deletion with proper context preservation
  - Color-coded event visualization
- **Member Management**
  - Multiple membership tiers:
    - Gold (with Shareholder and Associate upgrade options)
    - Silver
    - Bronze
    - Copper
  - Membership upgrade workflows requiring committee approval
  - Standing tee time reservations for Gold Shareholder and Associate members
- **Staff Features**
  - Clerk dashboard for daily operations
  - Pro Shop staff interface for tee time management
  - Committee member approval workflows for membership upgrades
- **Security**
  - Role-based access control (Member, Clerk, Pro Shop Staff, Committee Member)
  - Anti-forgery token protection
  - Input validation and error handling

## Technology Stack
- **Backend**: ASP.NET Core with Razor Pages
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5, jQuery, JavaScript
- **Authentication**: ASP.NET Core Identity

## Installation
1. Clone repository:
   ```bash
   git clone https://github.com/xinxinhuang/TTS-Beta.git
   ```
2. Navigate to the project directory:
   ```bash
   cd TTS-Beta/teetime
   ```
3. Database setup:
   ```bash
   dotnet ef database update
   ```
4. Run application:
   ```bash
   dotnet run
   ```

## Usage
1. Access the application at `https://localhost:5001`
2. Login with appropriate credentials based on role:
   - Clerk: Access to manage daily operations
   - Pro Shop Staff: Manage tee sheet and events
   - Committee Member: Review and approve membership upgrades
   - Member: Book tee times based on membership tier privileges
3. Key workflows:
   - **Create Events**: Select date/time range from tee sheet, add event details
   - **Manage Tee Times**: View, block, or release tee times
   - **Process Upgrades**: Review membership upgrade requests
   - **Book Tee Times**: Members can book available times based on their tier

## Security Features
- CSRF protection with anti-forgery tokens
- Input validation on all forms
- Role-based authorization
- Data integrity through proper validation

## Project Structure
```
TeeTime/
├── Data/               # Database context and migrations
├── Models/             # Entity models
├── Pages/              # Razor pages
│   ├── TeeSheet/       # Tee sheet management interface
│   ├── TeeTime/        # Tee time booking
│   ├── Membership/     # Member management
│   └── Dashboard.cshtml# Staff dashboard
├── Services/           # Business logic
└── wwwroot/            # Static assets
```

## Recent Improvements
- Fixed event handling to maintain page context after adding/deleting events
- Corrected timezone issues in event date handling
- Enhanced user experience with confirmation modals
- Improved redirection logic to maintain user context

## Test Accounts
For development and testing purposes, the following accounts are available:

### Staff Accounts
| Role | Email | Password | Description |
|------|-------|----------|-------------|
| Clerk | clerk@teetime.com | Password123! | Access to daily operations and tee sheet management |
| Pro Shop Staff | proshop@teetime.com | Password123! | Manages tee times and events |
| Committee Member | committee@teetime.com | Password123! | Reviews and approves membership upgrades |

### Member Accounts
| Membership Tier | Email | Password | Description |
|-----------------|-------|----------|-------------|
| Gold Shareholder | goldshareholder@example.com | Password123! | Premium member with sponsorship and standing tee time privileges |
| Gold Associate | goldassociate@example.com | Password123! | Premium member with sponsorship and standing tee time privileges |
| Gold | gold@example.com | Password123! | Basic gold membership |
| Silver | silver@example.com | Password123! | Standard membership with limited privileges |
| Bronze | bronze@example.com | Password123! | Basic membership with restricted access |
| Copper | copper@example.com | Password123! | Entry-level membership with minimal privileges |

*Note: These are test accounts with standardized passwords for development purposes only. In production, secure password policies should be enforced.*

## Contributing
1. Fork the repository
2. Create feature branch
3. Submit PR with detailed description
