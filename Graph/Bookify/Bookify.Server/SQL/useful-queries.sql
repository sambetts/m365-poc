-- Bookify Database - Useful SQL Queries

-- ==========================================
-- View all rooms
-- ==========================================
SELECT * FROM Rooms
ORDER BY Name;

-- ==========================================
-- View all bookings with room details
-- ==========================================
SELECT 
    b.Id AS BookingId,
    b.BookedBy,
    b.BookedByEmail,
    b.StartTime,
    b.EndTime,
    b.Purpose,
    b.CreatedAt,
    r.Name AS RoomName,
    r.Location,
    r.Capacity
FROM Bookings b
INNER JOIN Rooms r ON b.RoomId = r.Id
ORDER BY b.StartTime DESC;

-- ==========================================
-- Check room availability for today
-- ==========================================
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

SELECT 
    r.Id,
    r.Name,
    r.Location,
    r.Capacity,
    r.Equipment,
    COUNT(b.Id) AS BookingsToday
FROM Rooms r
LEFT JOIN Bookings b ON r.Id = b.RoomId 
    AND b.StartTime >= @Today 
    AND b.StartTime < @Tomorrow
GROUP BY r.Id, r.Name, r.Location, r.Capacity, r.Equipment
ORDER BY r.Name;

-- ==========================================
-- Find available rooms for a specific time
-- ==========================================
-- Example: Find rooms available on 2024-01-15 from 9:00 to 10:00
DECLARE @RequestedStart DATETIME = '2024-01-15 09:00:00';
DECLARE @RequestedEnd DATETIME = '2024-01-15 10:00:00';

SELECT 
    r.Id,
    r.Name,
    r.Location,
    r.Capacity,
    r.Equipment,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM Bookings b 
            WHERE b.RoomId = r.Id 
            AND b.StartTime < @RequestedEnd 
            AND b.EndTime > @RequestedStart
        ) THEN 'Booked'
        ELSE 'Available'
    END AS Status
FROM Rooms r
ORDER BY r.Name;

-- ==========================================
-- Get bookings for a specific room
-- ==========================================
DECLARE @RoomId INT = 1;

SELECT 
    b.Id,
    b.BookedBy,
    b.BookedByEmail,
    b.StartTime,
    b.EndTime,
    b.Purpose,
    DATEDIFF(MINUTE, b.StartTime, b.EndTime) AS DurationMinutes
FROM Bookings b
WHERE b.RoomId = @RoomId
    AND b.EndTime >= GETDATE()  -- Only future bookings
ORDER BY b.StartTime;

-- ==========================================
-- Get bookings for a specific user
-- ==========================================
DECLARE @UserEmail VARCHAR(200) = 'john.doe@company.com';

SELECT 
    b.Id,
    r.Name AS RoomName,
    r.Location,
    b.StartTime,
    b.EndTime,
    b.Purpose
FROM Bookings b
INNER JOIN Rooms r ON b.RoomId = r.Id
WHERE b.BookedByEmail = @UserEmail
    AND b.EndTime >= GETDATE()  -- Only future bookings
ORDER BY b.StartTime;

-- ==========================================
-- Find most popular rooms (by booking count)
-- ==========================================
SELECT 
    r.Name,
    r.Location,
    COUNT(b.Id) AS TotalBookings,
    SUM(DATEDIFF(MINUTE, b.StartTime, b.EndTime)) AS TotalMinutesBooked
FROM Rooms r
LEFT JOIN Bookings b ON r.Id = b.RoomId
GROUP BY r.Id, r.Name, r.Location
ORDER BY TotalBookings DESC;

-- ==========================================
-- Find booking conflicts (overlapping bookings for same room)
-- ==========================================
SELECT 
    b1.RoomId,
    r.Name AS RoomName,
    b1.Id AS Booking1Id,
    b1.BookedBy AS Booking1User,
    b1.StartTime AS Booking1Start,
    b1.EndTime AS Booking1End,
    b2.Id AS Booking2Id,
    b2.BookedBy AS Booking2User,
    b2.StartTime AS Booking2Start,
    b2.EndTime AS Booking2End
FROM Bookings b1
INNER JOIN Bookings b2 ON b1.RoomId = b2.RoomId AND b1.Id < b2.Id
INNER JOIN Rooms r ON b1.RoomId = r.Id
WHERE b1.StartTime < b2.EndTime AND b1.EndTime > b2.StartTime
ORDER BY b1.RoomId, b1.StartTime;

-- ==========================================
-- Delete old bookings (cleanup)
-- ==========================================
-- Uncomment to delete bookings older than 30 days
-- DELETE FROM Bookings 
-- WHERE EndTime < DATEADD(DAY, -30, GETDATE());

-- ==========================================
-- Add a new room
-- ==========================================
-- INSERT INTO Rooms (Name, Location, Capacity, Equipment)
-- VALUES ('Meeting Room C', 'Floor 2, Building B', 8, 'TV Screen, Whiteboard');

-- ==========================================
-- Update room details
-- ==========================================
-- UPDATE Rooms
-- SET Equipment = 'Projector, Video Conferencing, Whiteboard'
-- WHERE Id = 1;

-- ==========================================
-- Cancel a booking
-- ==========================================
-- DELETE FROM Bookings WHERE Id = 1;
