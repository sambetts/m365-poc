// API service for connecting to ASP.NET backend

export interface MeetingRoom {
  id: string;
  name: string;
  capacity: number;
  amenities: string[];
  available: boolean;
  floor: number;
}

export interface Booking {
  roomId: string;
  roomName: string;
  date: string; // ISO date part (YYYY-MM-DD)
  time: string; // HH:mm
  duration: string; // e.g. "1H", "2H"
  title?: string;
  purpose?: string;
}

export interface BookingUpdate extends Booking {
  id: number; // Existing booking id
}

// API base URL - adjust based on your environment
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

// Helper function for API calls
async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`API Error: ${response.status} - ${errorText}`);
  }

  // Some endpoints (PUT/DELETE) may return no content
  if (response.status === 204) {
    return undefined as unknown as T;
  }
  return response.json();
}

export const api = {
  // Fetch all meeting rooms
  async getMeetingRooms(): Promise<MeetingRoom[]> {
    return fetchApi<MeetingRoom[]>('/rooms');
  },

  // Get a single booking (for editing scenarios)
  async getBooking(id: number): Promise<{
    id: number;
    roomId: string;
    roomName: string;
    bookedBy: string;
    bookedByEmail: string;
    startTime: string;
    endTime: string;
    title?: string;
    purpose?: string;
    createdAt: string;
    calendarEventId?: string;
  }> {
    return fetchApi(`/bookings/${id}`);
  },

  // Book a meeting room (create)
  async bookRoom(booking: Booking): Promise<{ success: boolean; message: string }> {
    try {
      const { startDateTime, endDateTime } = computeDateTimes(booking.date, booking.time, booking.duration);

      const bookingRequest = {
        roomId: booking.roomId,
        bookedBy: 'Current User', // TODO: Replace with authenticated user name
        bookedByEmail: 'user@example.com', // TODO: Replace with authenticated user email
        startTime: startDateTime.toISOString(),
        endTime: endDateTime.toISOString(),
        title: booking.title,
        purpose: booking.purpose ?? 'Meeting',
      };

      await fetchApi('/bookings', {
        method: 'POST',
        body: JSON.stringify(bookingRequest),
      });

      return {
        success: true,
        message: `ROOM BOOKED! ${booking.roomName} - ${booking.date} at ${booking.time}`,
      };
    } catch (error) {
      return formatError(error, 'BOOKING FAILED! TRY AGAIN');
    }
  },

  // Update an existing booking
  async updateRoomBooking(update: BookingUpdate): Promise<{ success: boolean; message: string }> {
    try {
      const { startDateTime, endDateTime } = computeDateTimes(update.date, update.time, update.duration);

      const bookingRequest = {
        roomId: update.roomId,
        bookedBy: 'Current User', // TODO: Replace with authenticated user name
        bookedByEmail: 'user@example.com', // TODO: Replace with authenticated user email
        startTime: startDateTime.toISOString(),
        endTime: endDateTime.toISOString(),
        title: update.title,
        purpose: update.purpose ?? 'Meeting',
      };

      await fetchApi(`/bookings/${update.id}`, {
        method: 'PUT',
        body: JSON.stringify(bookingRequest),
      });

      return {
        success: true,
        message: `BOOKING UPDATED! ${update.roomName} - ${update.date} at ${update.time}`,
      };
    } catch (error) {
      return formatError(error, 'UPDATE FAILED! TRY AGAIN');
    }
  },

  // Raw update (if caller already has ISO start/end times)
  async updateBookingRaw(id: number, data: { roomId: string; startTime: string; endTime: string; title?: string; purpose?: string; }): Promise<void> {
    const bookingRequest = {
      roomId: data.roomId,
      bookedBy: 'Current User', // TODO: Replace with authenticated user name
      bookedByEmail: 'user@example.com', // TODO: Replace with authenticated user email
      startTime: data.startTime,
      endTime: data.endTime,
      title: data.title,
      purpose: data.purpose,
    };
    await fetchApi(`/bookings/${id}`, { method: 'PUT', body: JSON.stringify(bookingRequest) });
  },

  // Refresh room availability
  async refreshRooms(): Promise<MeetingRoom[]> {
    return this.getMeetingRooms();
  },

  // Check room availability for a specific time range
  async checkAvailability(startTime: Date, endTime: Date): Promise<MeetingRoom[]> {
    const request = {
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
    };

    interface AvailabilityResponse {
      id: string;
      name: string;
      capacity: number;
      amenities: string[];
      isAvailable: boolean;
      floor: number;
      existingBookings: Array<{
        id: number;
        bookedBy: string;
        startTime: string;
        endTime: string;
        purpose?: string;
      }>;
    }

    const availabilityData = await fetchApi<AvailabilityResponse[]>('/rooms/availability', {
      method: 'POST',
      body: JSON.stringify(request),
    });

    return availabilityData.map(room => ({
      id: room.id,
      name: room.name,
      capacity: room.capacity,
      amenities: room.amenities,
      available: room.isAvailable,
      floor: room.floor,
    }));
  },

  // Get bookings for a specific room
  async getRoomBookings(
    roomId: string,
    startDate?: Date,
    endDate?: Date
  ): Promise<Array<{
    id: number;
    bookedBy: string;
    startTime: string;
    endTime: string;
    purpose?: string;
  }>> {
    let endpoint = `/rooms/${roomId}/bookings`;
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    if (params.toString()) endpoint += `?${params.toString()}`;
    return fetchApi(endpoint);
  },

  // Get all bookings
  async getAllBookings(
    startDate?: Date,
    endDate?: Date
  ): Promise<Array<{
    id: number;
    roomId: string;
    roomName: string;
    bookedBy: string;
    bookedByEmail: string;
    startTime: string;
    endTime: string;
    purpose?: string;
    createdAt: string;
  }>> {
    let endpoint = '/bookings';
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate.toISOString());
    if (endDate) params.append('endDate', endDate.toISOString());
    if (params.toString()) endpoint += `?${params.toString()}`;
    return fetchApi(endpoint);
  },

  // Cancel a booking
  async cancelBooking(bookingId: number): Promise<void> {
    await fetchApi(`/bookings/${bookingId}`, { method: 'DELETE' });
  },
};

// Utility: compute start & end datetimes from form inputs
function computeDateTimes(date: string, time: string, duration: string) {
  const startDateTime = new Date(`${date}T${time}`);
  const durationHours = parseFloat(duration.replace('H', ''));
  const endDateTime = new Date(startDateTime.getTime() + durationHours * 60 * 60000);
  return { startDateTime, endDateTime };
}

// Utility: standard error formatting
function formatError(error: unknown, fallback: string) {
  return {
    success: false,
    message: error instanceof Error ? error.message : fallback,
  };
}
