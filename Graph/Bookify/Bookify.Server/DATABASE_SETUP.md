# Database Setup Guide

## Overview
The application uses SQL Server LocalDB with Entity Framework Core and automatically creates/migrates the database on startup.

## Requirements
- SQL Server LocalDB (included with Visual Studio)
- .NET 8 SDK

## Automatic Database Creation
The database is **automatically created and migrated** when you start the application. The `Program.cs` file includes code that runs migrations on startup.

## Connection String
Default connection string (in `appsettings.json`):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookifyDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

## What Happens on Startup
1. The application connects to SQL Server LocalDB
2. If the database doesn't exist, it's created automatically
3. All pending migrations are applied
4. The database is seeded with initial room data (6 gaming-themed meeting rooms)

## Manual Migration (Optional)
If you prefer to run migrations manually, you can use these commands:

```bash
# Navigate to the server project
cd Bookify.Server

# Create a new migration (after changing models)
dotnet ef migrations add YourMigrationName

# Apply migrations to the database
dotnet ef database update

# Remove the last migration (if not applied to DB)
dotnet ef migrations remove
```

## Database Schema

### Rooms Table
- `Id` (nvarchar(50), primary key) - String-based room identifier
- `Name` (nvarchar(100)) - Room name
- `Capacity` (int) - Number of people the room can hold
- `Amenities` (nvarchar(max)) - Comma-separated list of amenities
- `Available` (bit) - Whether the room is available
- `Floor` (int) - Floor number where the room is located

### Bookings Table
- `Id` (int, primary key, auto-increment)
- `RoomId` (nvarchar(50), foreign key) - References Rooms.Id
- `BookedBy` (nvarchar(100)) - Name of person who booked
- `BookedByEmail` (nvarchar(200)) - Email of person who booked
- `StartTime` (datetime2) - Booking start time
- `EndTime` (datetime2) - Booking end time
- `Purpose` (nvarchar(500), nullable) - Purpose of the booking
- `CreatedAt` (datetime2) - When the booking was created

## Seeded Data
The database is pre-populated with 6 meeting rooms:
1. **PIXEL PALACE** (Floor 2, Capacity 8)
2. **8-BIT BOARDROOM** (Floor 3, Capacity 12)
3. **RETRO RETREAT** (Floor 2, Capacity 6)
4. **ARCADE ARENA** (Floor 1, Capacity 4)
5. **SPRITE SUMMIT** (Floor 3, Capacity 10)
6. **CONSOLE CHAMBER** (Floor 1, Capacity 6)

## Troubleshooting

### "Cannot open database" error
This error should no longer occur since the database is created automatically. If you still see it:
1. Ensure SQL Server LocalDB is installed
2. Check the connection string in `appsettings.json`
3. Try deleting the database and restart the application

### Check if LocalDB is running
```bash
sqllocaldb info
sqllocaldb start mssqllocaldb
```

### Delete the database (fresh start)
```bash
dotnet ef database drop
```
Then restart the application - it will recreate everything.

### View migration history
```bash
dotnet ef migrations list
```

## Alternative Connection Strings

### SQL Server Express
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BookifyDb;Trusted_Connection=True;MultipleActiveResultSets=true"
```

### SQL Server with credentials
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=BookifyDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true"
```

### Azure SQL
```json
"DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=BookifyDb;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```
