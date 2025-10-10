// Mock API service with fake delays to simulate real API calls

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

const mockRooms: MeetingRoom[] = [
  {
    id: "1",
    name: "PIXEL PALACE",
    capacity: 8,
    amenities: ["TV Screen", "WiFi", "Coffee"],
    available: true,
    floor: 2,
  },
  {
    id: "2",
    name: "8-BIT BOARDROOM",
    capacity: 12,
    amenities: ["TV Screen", "WiFi"],
    available: true,
    floor: 3,
  },
  {
    id: "3",
    name: "RETRO RETREAT",
    capacity: 6,
    amenities: ["WiFi", "Coffee"],
    available: false,
    floor: 2,
  },
  {
    id: "4",
    name: "ARCADE ARENA",
    capacity: 4,
    amenities: ["TV Screen", "WiFi"],
    available: true,
    floor: 1,
  },
  {
    id: "5",
    name: "SPRITE SUMMIT",
    capacity: 10,
    amenities: ["TV Screen", "WiFi", "Coffee"],
    available: true,
    floor: 3,
  },
  {
    id: "6",
    name: "CONSOLE CHAMBER",
    capacity: 6,
    amenities: ["WiFi"],
    available: false,
    floor: 1,
  },
];

// Simulate API delay
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

export const api = {
  // Fetch all meeting rooms
  async getMeetingRooms(): Promise<MeetingRoom[]> {
    await delay(1200); // Simulate network delay
    return [...mockRooms];
  },

  // Book a meeting room
  async bookRoom(booking: Booking): Promise<{ success: boolean; message: string }> {
    await delay(800); // Simulate processing time
    
    // Simulate occasional failures (10% chance)
    if (Math.random() < 0.1) {
      return {
        success: false,
        message: "BOOKING FAILED! TRY AGAIN"
      };
    }

    return {
      success: true,
      message: `ROOM BOOKED! ${booking.roomName} - ${booking.date} at ${booking.time}`
    };
  },

  // Refresh room availability
  async refreshRooms(): Promise<MeetingRoom[]> {
    await delay(600);
    
    // Randomly update availability for demo purposes
    return mockRooms.map(room => ({
      ...room,
      available: Math.random() > 0.3
    }));
  }
};
