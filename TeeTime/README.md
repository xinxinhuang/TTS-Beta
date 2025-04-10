# TeeTime Management System

A golf tee time management application built with ASP.NET Core backend and Next.js frontend with Tailwind CSS.

## Development Setup

### Prerequisites

- .NET SDK 
- Node.js (v16+)
- npm or yarn

### Installing Dependencies

1. Install backend dependencies:
   ```
   dotnet restore
   ```

2. Install frontend dependencies:
   ```
   cd ClientApp/teetimeapp
   npm install
   ```

### Running the Application

#### Option 1: Using the dev script (recommended)

For Windows users:
```
.\dev.bat
```

For macOS/Linux users:
```
chmod +x ./dev.sh
./dev.sh
```

This will start both the backend and frontend concurrently.

#### Option 2: Manual startup

1. Start the backend:
   ```
   dotnet run
   ```

2. In a separate terminal, start the frontend:
   ```
   cd ClientApp/teetimeapp
   npm run dev
   ```

### Accessing the Application

- Frontend: http://localhost:3000
- Backend API: https://localhost:5001 (or http://localhost:5000)

## Deployment

See the deployment instructions in [ClientApp/teetimeapp/README.md](ClientApp/teetimeapp/README.md) for information on deploying to Railway.

## Technologies Used

- **Backend**: ASP.NET Core
- **Frontend**: Next.js, React
- **Styling**: Tailwind CSS
- **Database**: SQL Server 