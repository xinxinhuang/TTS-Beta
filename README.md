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
- **Authentication**: Custom authentication system

## Installation and Setup
1. Clone repository:
   ```bash
   git clone https://github.com/xinxinhuang/TTS-Beta.git
   ```
2. Navigate to the project directory:
   ```bash
   cd TeeTime
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Update the database:
   ```bash
   dotnet ef database update
   ```
5. Run application:
   ```bash
   dotnet run
   ```

## Usage
1. Access the application at the URL shown in the console after running `dotnet run` (typically something like `http://localhost:5000` or `https://localhost:5001`)
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

## Test Accounts
For development and testing purposes, the following accounts are available:

### Staff Accounts
| Role | Email | Password | Description |
|------|-------|----------|-------------|
| Clerk | clerk@t.t | 123123 | Access to daily operations and tee sheet management |
| Committee Member | committee@teetimeclub.com | Password123! | Reviews and approves membership upgrades |

### Member Accounts (Sponsors)
| Membership Tier | Email | Password | Description |
|-----------------|-------|----------|-------------|
| Gold Shareholder | sponsor1@teetimeclub.com | Password123! | Premium member with sponsorship and standing tee time privileges |
| Gold Shareholder | sponsor2@teetimeclub.com | Password123! | Premium member with sponsorship and standing tee time privileges |
| Gold Associate | sponsor3@teetimeclub.com | Password123! | Premium member with sponsorship and standing tee time privileges |
| Gold Associate | sponsor4@teetimeclub.com | Password123! | Premium member with sponsorship and standing tee time privileges |

*Note: These are test accounts with standardized passwords for development purposes only. In production, secure password policies should be enforced.*

## Troubleshooting
- If you see warnings about "Failed to determine the https port for redirect" in development, you can either:
  - Configure the HTTPS port explicitly in Program.cs
  - Remove the `app.UseHttpsRedirection()` line if not using HTTPS in development
- Build warnings about "Dereference of a possibly null reference" can be addressed by adding proper null checks in the code

## Contributing
1. Fork the repository
2. Create feature branch
3. Submit PR with detailed description
