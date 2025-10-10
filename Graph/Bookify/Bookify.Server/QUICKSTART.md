# Bookify - Quick Start Guide

## ?? Getting Started in 5 Minutes

### Step 1: Apply Database Migrations
Open a terminal in the `Bookify.Server` folder and run:

```bash
dotnet ef database update
```

This will create the database and seed it with sample rooms.

### Step 2: Run the Application
```bash
dotnet run
```

### Step 3: Test the API
Open your browser and navigate to:
```
https://localhost:{port}/swagger
```

You'll see the Swagger UI with all available endpoints.

## ?? Quick API Tests

### Test 1: Get All Rooms
```bash
GET https://localhost:{port}/api/rooms
```

Expected response: List of 4 sample rooms

### Test 2: Check Room Availability
```bash
POST https://localhost:{port}/api/rooms/availability
Content-Type: application/json

{
  "startTime": "2024-01-15T09:00:00Z",
  "endTime": "2024-01-15T10:00:00Z"
}
```

Expected response: List of all rooms with availability status

### Test 3: Create a Booking
```bash
POST https://localhost:{port}/api/bookings
Content-Type: application/json

{
  "roomId": 1,
  "bookedBy": "John Doe",
  "bookedByEmail": "john.doe@company.com",
  "startTime": "2024-01-15T14:00:00Z",
  "endTime": "2024-01-15T15:00:00Z",
  "purpose": "Team Meeting"
}
```

Expected response: Created booking with ID and details

### Test 4: Get User's Bookings
```bash
GET https://localhost:{port}/api/bookings/user/john.doe@company.com
```

Expected response: List of bookings for that user

## ?? Configuration

### Change Database Connection
Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING_HERE"
  }
}
```

### Common Connection Strings

**LocalDB (default):**
```
Server=(localdb)\\mssqllocaldb;Database=BookifyDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server Express:**
```
Server=localhost\\SQLEXPRESS;Database=BookifyDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with credentials:**
```
Server=YOUR_SERVER;Database=BookifyDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true
```

**Azure SQL:**
```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=BookifyDb;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## ?? Sample Data

The database is pre-seeded with these rooms:

| ID | Name | Location | Capacity | Equipment |
|----|------|----------|----------|-----------|
| 1 | Conference Room A | Floor 1, Building A | 10 | - |
| 2 | Conference Room B | Floor 2, Building A | 6 | - |
| 3 | Board Room | Floor 3, Building A | 20 | Projector, Video Conferencing |
| 4 | Training Room | Floor 1, Building B | 30 | Whiteboard, Projector |

## ?? Integration with Frontend

### For React/Vue/Angular Applications

1. Copy the API client code from `ClientExamples/api-client.js`
2. Update the `API_BASE_URL` constant with your backend URL
3. Import and use the functions in your components

Example (React):
```javascript
import { checkRoomAvailability, createBooking } from './api-client';

// In your component
const handleCheckAvailability = async () => {
  const startTime = new Date('2024-01-15T09:00:00Z');
  const endTime = new Date('2024-01-15T10:00:00Z');
  
  const rooms = await checkRoomAvailability(startTime, endTime);
  const available = rooms.filter(r => r.isAvailable);
  console.log('Available rooms:', available);
};
```

### CORS Configuration

The API is configured to allow all origins in development mode. For production, update `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy",
        builder => builder
            .WithOrigins("https://your-frontend-domain.com")
            .AllowAnyMethod()
            .AllowAnyHeader());
});
```

## ?? Troubleshooting

### Database Connection Issues
- Ensure SQL Server LocalDB is installed
- Check connection string in `appsettings.json`
- Run `dotnet ef database update` to create/update database

### Migration Errors
- Delete the `Migrations` folder
- Run `dotnet ef migrations add InitialCreate`
- Run `dotnet ef database update`

### Port Conflicts
- Check `Properties/launchSettings.json` to see configured ports
- Update the port number if needed

## ?? Next Steps

1. **Add Authentication**: Integrate Azure AD or Identity Server
2. **Email Notifications**: Send booking confirmations via email
3. **Calendar Integration**: Sync with Outlook/Google Calendar using Microsoft Graph
4. **Recurring Bookings**: Add support for weekly/daily recurring meetings
5. **Reports**: Add analytics and usage reports

## ??? Useful Commands

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to a specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script

# Build the project
dotnet build

# Run the project
dotnet run

# Run with watch (auto-reload on changes)
dotnet watch run
```

## ?? Support

For issues or questions:
1. Check the main README.md for detailed documentation
2. Review the Swagger documentation at `/swagger`
3. Check the SQL queries in `SQL/useful-queries.sql` for database inspection
