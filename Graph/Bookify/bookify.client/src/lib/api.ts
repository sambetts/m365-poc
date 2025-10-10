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
  date: string;
  time: string;
  duration: string;
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

  return response.json();
}

export const api = {
  // Fetch all meeting rooms
  async getMeetingRooms(): Promise<MeetingRoom[]> {
    return fetchApi<MeetingRoom[]>('/rooms');
  },

  // Book a meeting room
  async bookRoom(booking: Booking): Promise<{ success: boolean; message: string }> {
    try {
      // Parse date and time to create DateTime values
      const startDateTime = new Date(`${booking.date}T${booking.time}`);
      const durationMinutes = parseInt(booking.duration);
      const endDateTime = new Date(startDateTime.getTime() + durationMinutes * 60000);

      const bookingRequest = {
        roomId: booking.roomId,
        bookedBy: 'Current User', // TODO: Get from authentication
        bookedByEmail: 'user@example.com', // TODO: Get from authentication
        startTime: startDateTime.toISOString(),
        endTime: endDateTime.toISOString(),
        purpose: 'Meeting', // Optional: could be added to the booking interface
      };

      const response = await fetchApi('/bookings', {
        method: 'POST',
        body: JSON.stringify(bookingRequest),
      });

      return {
        success: true,
        message: `ROOM BOOKED! ${booking.roomName} - ${booking.date} at ${booking.time}`,
      };
    } catch (error) {
      return {
        success: false,
        message: error instanceof Error ? error.message : 'BOOKING FAILED! TRY AGAIN',
      };
    }
  },

  // Refresh room availability
  async refreshRooms(): Promise<MeetingRoom[]> {
    // Simply fetch the latest room data from the server
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
/******/
    }

    const availabilityData = await fetchApi<AvailabilityResponse[]>('/rooms/availability', {
      method: 'POST',
      body: JSON.stringify(request),
    });

    // Transform to MeetingRoom format
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
    
    if (startDate) {
      params.append('startDate', startDate.toISOString());
    }
    if (endDate) {
      params.append('endDate', endDate.toISOString());
    }
    
    if (params.toString()) {
      endpoint += `?${params.toString()}`;
    }

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
    
    if (startDate) {
      params.append('startDate', startDate.toISOString());
    }
    if (endDate) {
      params.append('endDate', endDate.toISOString());
    }
    
    if (params.toString()) {
      endpoint += `?${params.toString()}`;
    }

    return fetchApi(endpoint);
  },

  // Cancel a booking
  async cancelBooking(bookingId: number): Promise<void> {
    await fetchApi(`/bookings/${bookingId}`, {
      method: 'DELETE',
    });
  },
};
