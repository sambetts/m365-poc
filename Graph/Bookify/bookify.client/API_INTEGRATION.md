# Bookify Client - API Integration

## Overview
This client application connects to the Bookify ASP.NET backend API to manage meeting room bookings.

## API Configuration

### Development
The Vite development server is configured with a proxy to forward `/api` requests to the ASP.NET backend at `https://localhost:7163`.

No additional configuration is needed for local development - just ensure:
1. The ASP.NET backend is running (Bookify.Server)
2. The client is running with `npm run dev`

### Production
For production deployments, set the `VITE_API_URL` environment variable to your deployed backend URL:

```bash
VITE_API_URL=https://your-backend-api.com/api
```

## API Endpoints Used

### Rooms
- `GET /api/rooms` - Get all meeting rooms
- `GET /api/rooms/{id}` - Get a specific room
- `POST /api/rooms/availability` - Check room availability for a time range
- `GET /api/rooms/{id}/bookings` - Get bookings for a specific room

### Bookings
- `GET /api/bookings` - Get all bookings
- `POST /api/bookings` - Create a new booking
- `DELETE /api/bookings/{id}` - Cancel a booking

## API Service Usage

```typescript
import { api } from './lib/api';

// Get all rooms
const rooms = await api.getMeetingRooms();

// Book a room
const result = await api.bookRoom({
  roomId: "1",
  roomName: "PIXEL PALACE",
  date: "2024-01-15",
  time: "09:00",
  duration: "60"
});

// Check availability
const availableRooms = await api.checkAvailability(
  new Date('2024-01-15T09:00:00'),
  new Date('2024-01-15T10:00:00')
);

// Refresh room data
const updatedRooms = await api.refreshRooms();

// Cancel a booking
await api.cancelBooking(bookingId);
```

## Authentication
Currently, the API uses placeholder values for user authentication (`bookedBy` and `bookedByEmail`). 

**TODO**: Integrate with Microsoft Authentication Library (MSAL) to get actual user information from Azure AD/Entra ID.

## Error Handling
All API calls include error handling. Failed requests will:
- Throw descriptive error messages
- Return error responses in the `bookRoom` method for user-friendly feedback

## CORS
Ensure the ASP.NET backend has CORS configured to allow requests from the client origin during development (configured in `Program.cs`).
