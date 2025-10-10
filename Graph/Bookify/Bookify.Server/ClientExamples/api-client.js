// Example API calls for the Bookify frontend client

// Base URL for API (adjust based on your environment)
const API_BASE_URL = 'https://localhost:7000/api'; // Update with your actual port

// ==========================================
// ROOM APIs
// ==========================================

/**
 * Get all rooms
 */
export async function getAllRooms() {
  const response = await fetch(`${API_BASE_URL}/rooms`);
  return await response.json();
}

/**
 * Get a specific room by ID
 */
export async function getRoom(roomId) {
  const response = await fetch(`${API_BASE_URL}/rooms/${roomId}`);
  return await response.json();
}

/**
 * Check room availability for a specific time range
 * @param {Date} startTime - Start time of the booking
 * @param {Date} endTime - End time of the booking
 * @returns {Promise<Array>} List of rooms with availability status
 */
export async function checkRoomAvailability(startTime, endTime) {
  const response = await fetch(`${API_BASE_URL}/rooms/availability`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
    }),
  });
  return await response.json();
}

/**
 * Get bookings for a specific room
 */
export async function getRoomBookings(roomId, startDate = null, endDate = null) {
  let url = `${API_BASE_URL}/rooms/${roomId}/bookings`;
  const params = new URLSearchParams();
  
  if (startDate) {
    params.append('startDate', startDate.toISOString());
  }
  if (endDate) {
    params.append('endDate', endDate.toISOString());
  }
  
  if (params.toString()) {
    url += '?' + params.toString();
  }
  
  const response = await fetch(url);
  return await response.json();
}

// ==========================================
// BOOKING APIs
// ==========================================

/**
 * Get all bookings
 */
export async function getAllBookings(startDate = null, endDate = null) {
  let url = `${API_BASE_URL}/bookings`;
  const params = new URLSearchParams();
  
  if (startDate) {
    params.append('startDate', startDate.toISOString());
  }
  if (endDate) {
    params.append('endDate', endDate.toISOString());
  }
  
  if (params.toString()) {
    url += '?' + params.toString();
  }
  
  const response = await fetch(url);
  return await response.json();
}

/**
 * Get a specific booking by ID
 */
export async function getBooking(bookingId) {
  const response = await fetch(`${API_BASE_URL}/bookings/${bookingId}`);
  return await response.json();
}

/**
 * Create a new booking
 * @param {Object} bookingData - Booking information
 * @param {number} bookingData.roomId - Room ID to book
 * @param {string} bookingData.bookedBy - Name of the person booking
 * @param {string} bookingData.bookedByEmail - Email of the person booking
 * @param {Date} bookingData.startTime - Start time of the booking
 * @param {Date} bookingData.endTime - End time of the booking
 * @param {string} bookingData.purpose - Purpose of the booking (optional)
 */
export async function createBooking(bookingData) {
  const response = await fetch(`${API_BASE_URL}/bookings`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      roomId: bookingData.roomId,
      bookedBy: bookingData.bookedBy,
      bookedByEmail: bookingData.bookedByEmail,
      startTime: bookingData.startTime.toISOString(),
      endTime: bookingData.endTime.toISOString(),
      purpose: bookingData.purpose || null,
    }),
  });
  
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error);
  }
  
  return await response.json();
}

/**
 * Update an existing booking
 */
export async function updateBooking(bookingId, bookingData) {
  const response = await fetch(`${API_BASE_URL}/bookings/${bookingId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      roomId: bookingData.roomId,
      bookedBy: bookingData.bookedBy,
      bookedByEmail: bookingData.bookedByEmail,
      startTime: bookingData.startTime.toISOString(),
      endTime: bookingData.endTime.toISOString(),
      purpose: bookingData.purpose || null,
    }),
  });
  
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error);
  }
}

/**
 * Delete a booking
 */
export async function deleteBooking(bookingId) {
  const response = await fetch(`${API_BASE_URL}/bookings/${bookingId}`, {
    method: 'DELETE',
  });
  
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error);
  }
}

/**
 * Get bookings for a specific user
 */
export async function getUserBookings(email) {
  const response = await fetch(`${API_BASE_URL}/bookings/user/${encodeURIComponent(email)}`);
  return await response.json();
}

// ==========================================
// EXAMPLE USAGE
// ==========================================

/*
// Example: Check which rooms are available from 9 AM to 10 AM today
const startTime = new Date();
startTime.setHours(9, 0, 0, 0);

const endTime = new Date();
endTime.setHours(10, 0, 0, 0);

const availableRooms = await checkRoomAvailability(startTime, endTime);
console.log('Available rooms:', availableRooms.filter(r => r.isAvailable));

// Example: Book a room
try {
  const booking = await createBooking({
    roomId: 1,
    bookedBy: 'John Doe',
    bookedByEmail: 'john.doe@company.com',
    startTime: startTime,
    endTime: endTime,
    purpose: 'Team Standup'
  });
  console.log('Booking created:', booking);
} catch (error) {
  console.error('Booking failed:', error.message);
}

// Example: Get all my bookings
const myBookings = await getUserBookings('john.doe@company.com');
console.log('My bookings:', myBookings);
*/
