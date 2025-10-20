# Bookify - Meeting Room Booking System

## Overview
Bookify is a meeting room booking system for Office 365 with a SQL Server backend. It allows users to view available rooms and book them for meetings.

## Database Schema

### Rooms Table
- `Id` (int, primary key)
- `Name` (string, required)
- `Location` (string, required)
- `Capacity` (int)
- `Equipment` (string, optional)

### Bookings Table
- `Id` (int, primary key)
- `RoomId` (int, foreign key)
- `BookedBy` (string, required)
- `BookedByEmail` (string, required)
- `StartTime` (DateTime)
- `EndTime` (DateTime)
- `Title` (string, optional)
- `Body` (string, optional) // Meeting description / purpose
- `CreatedAt` (DateTime)

## API Endpoints

### Rooms

#### GET `/api/rooms`
Get all available rooms.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Conference Room A",
    "location": "Floor 1, Building A",
    "capacity": 10,
    "equipment": null
  }
]
```

#### GET `/api/rooms/{id}`
Get a specific room by ID.

#### POST `/api/rooms/availability`
Check which rooms are available for a specific time range.

**Request:**
```json
{
  "startTime": "2024-01-15T09:00:00Z",
  "endTime": "2024-01-15T10:00:00Z"
}
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Conference Room A",
    "location": "Floor 1, Building A",
    "capacity": 10,
    "equipment": null,
    "isAvailable": true,
    "existingBookings": []
  },
  {
    "id": 2,
    "name": "Conference Room B",
    "location": "Floor 2, Building A",
    "capacity": 6,
    "equipment": null,
    "isAvailable": false,
    "existingBookings": [
      {
        "id": 1,
        "bookedBy": "John Doe",
        "startTime": "2024-01-15T09:00:00Z",
        "endTime": "2024-01-15T10:00:00Z",
        "body": "Team Standup"
      }
    ]
  }
]
```

#### GET `/api/rooms/{id}/bookings`
Get all bookings for a specific room.

**Query Parameters:**
- `startDate` (optional): Filter bookings from this date
- `endDate` (optional): Filter bookings until this date

### Bookings

#### GET `/api/bookings`
Get all bookings.

**Query Parameters:**
- `startDate` (optional): Filter bookings from this date
- `endDate` (optional): Filter bookings until this date

#### GET `/api/bookings/{id}`
Get a specific booking by ID.

#### POST `/api/bookings`
Create a new booking.

**Request:**
```json
{
  "roomId": 1,
  "bookedBy": "John Doe",
  "bookedByEmail": "john.doe@company.com",
  "startTime": "2024-01-15T09:00:00Z",
  "endTime": "2024-01-15T10:00:00Z",
  "body": "Team Meeting"
}
```

**Response:**
```json
{
  "id": 1,
  "roomId": 1,
  "roomName": "Conference Room A",
  "bookedBy": "John Doe",
  "bookedByEmail": "john.doe@company.com",
  "startTime": "2024-01-15T09:00:00Z",
  "endTime": "2024-01-15T10:00:00Z",
  "body": "Team Meeting",
  "createdAt": "2024-01-14T15:30:00Z"
}
```

#### PUT `/api/bookings/{id}`
Update an existing booking.

#### DELETE `/api/bookings/{id}`
Delete a booking.

#### GET `/api/bookings/user/{email}`
Get all bookings for a specific user by email.

## Setup Instructions

### Prerequisites
- .NET 8 SDK
- SQL Server or SQL Server LocalDB

### Database Setup

1. **Update Connection String** (if needed)
   
   Edit `appsettings.json` or `appsettings.Development.json` to configure your database connection:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookifyDb;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

2. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

   This will:
   - Create the `BookifyDb` database
   - Create the `Rooms` and `Bookings` tables
   - Seed initial room data

### Running the Application

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI:**
   Navigate to `https://localhost:{port}/swagger` to test the API endpoints.

## Sample Data

The database is seeded with the following rooms:
- Conference Room A (Floor 1, Building A) - Capacity: 10
- Conference Room B (Floor 2, Building A) - Capacity: 6
- Board Room (Floor 3, Building A) - Capacity: 20, Equipment: Projector, Video Conferencing
- Training Room (Floor 1, Building B) - Capacity: 30, Equipment: Whiteboard, Projector

## Features

? **Room Management**
- View all available rooms
- Check room details including capacity and equipment

? **Availability Checking**
- Check which rooms are available for a specific time range
- See existing bookings that conflict with requested time

? **Booking Management**
- Create new bookings
- Update existing bookings
- Delete bookings
- View user-specific bookings

? **Conflict Detection**
- Prevents double-booking of rooms
- Shows overlapping bookings when checking availability

## Future Enhancements

- Integration with Microsoft Graph API for calendar sync
- Email notifications for booking confirmations
- Recurring bookings
- Room search and filtering
- User authentication and authorization
- Booking approval workflow

## Technologies Used

- **ASP.NET Core 8** - Web API framework
- **Entity Framework Core 9** - ORM for database operations
- **SQL Server** - Database
- **Swagger/OpenAPI** - API documentation
