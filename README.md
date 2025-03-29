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

**1. Prerequisites:**
   - Ensure you have the [.NET SDK](https://dotnet.microsoft.com/download) installed (version compatible with the project, likely .NET 6.0 or later based on ASP.NET Core Razor Pages).

**2. Clone Repository:**
   ```bash
   git clone https://github.com/xinxinhuang/TTS-Beta.git
   ```

**3. Navigate to Project Directory:**
   ```bash
   cd TeeTime 
   ```
   *(Make sure you are in the directory containing the `TeeTime.csproj` file)*

**4. Set up EF Core Tools (Required for Database Migrations):**

   *   **Create Tool Manifest (if it doesn't exist):** This file tracks local .NET tools for the project.
       ```bash
       dotnet new tool-manifest 
       ```
   *   **Install EF Core Tools Locally:** This command installs the `dotnet-ef` tool and records it in the manifest.
       ```bash
       dotnet tool install dotnet-ef --local
       ```
   *   **Add EF Core Design Package:** This package is required for EF Core commands.
       ```bash
       dotnet add package Microsoft.EntityFrameworkCore.Design
       ```
   *(Note: If you prefer, you can install `dotnet-ef` globally using `dotnet tool install --global dotnet-ef`. If installed globally, you run EF commands directly, e.g., `dotnet ef database update`. However, local installation is often preferred for better project dependency management.)*

**5. Build the Project:**
   ```bash
   dotnet build
   ```

**6. Update the Database:** Apply any pending Entity Framework migrations.
   ```bash
   # This command works if dotnet-ef is installed globally or accessible via PATH
   dotnet ef database update

   # If using the locally installed tool explicitly, run:
   # dotnet tool run dotnet-ef database update 
   ```

**7. Run the Application:**
   ```bash
   dotnet run
   ```

## Database Updates

Whenever you pull changes from the repository that might include database schema modifications (new migrations), you need to update your local database:

1.  Ensure you are in the project directory (`TeeTime`).
2.  Run the database update command:

    ```bash
    # Use this command if dotnet-ef is installed globally or accessible via PATH
    dotnet ef database update

    # If using the locally installed tool explicitly (check .config/dotnet-tools.json)
    # dotnet tool run dotnet-ef database update
    ```

This ensures your database schema matches the current state of the application's models and migrations.

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

## Membership Structure
The system has multiple membership tiers with different privileges:

### Gold Tier Memberships
| Membership Type | Sponsorship | Standing Tee Times | Access Restrictions |
|-----------------|-------------|-------------------|---------------------|
| Gold Shareholder | ✓ | ✓ | No time restrictions - access to all tee times |
| Gold Associate | ✓ | ✓ | No time restrictions - access to all tee times |
| Gold | ✗ | ✗ | No time restrictions - access to all tee times |

### Other Membership Tiers
| Membership Type | Privileges | Time Restrictions |
|-----------------|------------|-------------------|
| Silver | Standard booking | • Weekdays: Before 3:00 PM or after 5:30 PM only<br>• Weekends/Holidays: After 11:00 AM only |
| Bronze | Standard booking | • Weekdays: Before 3:00 PM or after 6:00 PM only<br>• Weekends/Holidays: After 1:00 PM only |
| Copper | Cannot book tee times or play golf | N/A |

*Note: Only Gold Shareholder and Gold Associate members can sponsor guests and make standing tee time reservations.*

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
| Pro Shop Staff | proshop@teetime.com | Password123! | Access to tee sheet management and pro shop operations |
| Committee Member | committee@teetimeclub.com | Password123! | Reviews and approves membership upgrades |

### Member Accounts
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

## Recent Changes
### Consolidated Tee Time Booking System (March 2025)
- **Removed Redundancy**: Eliminated the separate ScheduledGolfTimes table which was causing disconnection between clerk and member systems
- **Direct Connection**: Connected TeeTime and Reservation tables directly, allowing seamless integration between clerk-generated tee sheets and member reservations
- **Improved Tracking**: Added TotalPlayersBooked property to TeeTime to better track tee time availability
- **Dynamic Availability**: Made IsAvailable a calculated property based on current bookings (max 4 players per tee time)
- **Enhanced UI**: Updated interface to show available slots more clearly to members

These changes fix the previous issue where tee times generated by clerks were not automatically available for booking by members. Now when a clerk generates a tee sheet, members can immediately see and book those slots.

## Contributing
1. Fork the repository
2. Create feature branch
3. Submit PR with detailed description
