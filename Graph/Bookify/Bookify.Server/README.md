# Bookify / GraphNotifications

## Overview
Bookify is a .NET8 meeting room booking API that keeps local SQL Server booking data in sync with Microsoft365 (Exchange Online) room resource calendars. It provides:
- REST endpoints for rooms, bookings, availability, subscriptions and Graph webhook notifications.
- Automatic creation / update / deletion of Microsoft Graph calendar events for bookings (best?effort, failures logged, DB state preserved).
- One?way inbound sync of external calendar changes (via encrypted Graph change notifications) back into local bookings.
- Audit trail of all local + external mutations (`UpdateLog`).

The solution contains two projects:
- `Bookify.Server` – ASP.NET Core Web API, EF Core, Graph client, webhook receiver.
- `GraphNotifications` – helper library (webhook subscription + decryption utilities).

## Architecture
Layers / services:
- Controllers (`BookingsController`, `RoomsController`, `SubscriptionsController`, `NotificationsController`).
- Core domain services: `BookingService`, `RoomService`.
- External calendar abstraction: `IExternalCalendarService` implemented by `GraphCalendarService` (Microsoft Graph calls).
- Inbound sync service: `BookingCalendarSyncService` implements `IBookingCalendarSyncService` (applies webhook changes).
- Validation: FluentValidation (`CreateBookingRequestValidator`, `RoomAvailabilityRequestValidator`).
- Persistence: EF Core `BookifyDbContext` (SQL Server). Simple seeding of example rooms, later normalised mailbox UPN on startup.

### Booking lifecycle (outbound)
1. Client POST `/api/bookings`.
2. `BookingService` persists booking, writes `UpdateLog`.
3. If external create requested, `GraphCalendarService` creates an Exchange Online event in the room mailbox and stores the returned EventId.
4. Subsequent updates PATCH the Graph event or recreate it if the room changes.
5. Deletes remove the Graph event (if tagged as Bookify) then the local booking.

### External change sync (inbound)
1. Subscriptions created with `/api/subscriptions` (server chooses resource & change types).
2. Microsoft Graph posts change notifications to configured webhook URL.
3. `NotificationsController` performs validation handshake (GET) or processes POST notifications.
4. Encrypted resource data (if present) is decrypted via Key Vault certificate, converted to partial `Event` model.
5. `BookingCalendarSyncService` applies fragments or fetches full event when necessary.
6. Differences (start/end/title/attendees) update the local `Booking`; `UpdateLog` row recorded.
7. Deleted events remove the associated booking.

## Data Model
```
Room: Id, Name, Capacity, Amenities(List<string>), Available, Floor, MailboxUpn, Bookings (nav)
Booking: Id, RoomId, BookedBy, BookedByEmail, StartTime(UTC), EndTime(UTC), Title, Body, CreatedAt, CalendarEventId, Attendees(List<string>), Room(nav)
UpdateLog: Id, BookingId?, CalendarEventId?, OccurredAtUtc, Source(web-app|notification), Action
```
Collections `Amenities` and `Attendees` are stored as comma?separated strings with EF Core value converters + custom value comparer.

## Configuration
All required at startup (throws if missing). Set in `appsettings*.json`, environment variables, Azure App Configuration, or Key Vault references.

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (retry enabled). |
| `AzureAd:TenantId` | Entra ID (Azure AD) tenant GUID. |
| `AzureAd:ClientId` | App registration (client) ID with Calendar.ReadWrite, offline access. |
| `AzureAd:ClientSecret` | Client secret for confidential app flow. |
| `SharedRoomMailboxUpn` | UPN of the shared mailbox whose calendar hosts all room events (room IDs seeded then normalised to this value). |
| `KeyVaultUrl` or `KeyVault:Url` | URI of Azure Key Vault containing the `webhooks` certificate for decrypting encrypted change notifications. |
| `WebhookUrlOverride` or `Graph:WebhookUrlOverride` | Public HTTPS endpoint Graph should call for change notifications (used when behind a tunnel / dev proxy). |

### Optional / notes
- If developing locally without encrypted resource data you can still process basic notifications (resource IDs) but must supply dummy `KeyVaultUrl` + `WebhookUrlOverride` to satisfy config guards.
- CORS policy `AllowAll` is enabled only in Development.

## Local Development Setup
1. Install .NET8 SDK & SQL Server / LocalDB.
2. Create `appsettings.Development.json` (example):
```json
{
 "ConnectionStrings": {
 "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookifyDb;Trusted_Connection=True;"
 },
 "AzureAd": {
 "TenantId": "<tenant-guid>",
 "ClientId": "<app-client-id>",
 "ClientSecret": "<secret>"
 },
 "SharedRoomMailboxUpn": "rooms@contoso.com",
 "KeyVaultUrl": "https://<your-kv>.vault.azure.net/",
 "WebhookUrlOverride": "https://<public-host>/api/notifications"
}
```
3. Run `dotnet run` in `Bookify.Server`. Database is created via `EnsureCreated()` and seed rooms are adjusted to configured `SharedRoomMailboxUpn`.
4. Browse Swagger UI (Development only): `https://localhost:PORT/swagger`.
5. Create / refresh a subscription: `POST /api/subscriptions` with body `{ "upn": "rooms@contoso.com" }` (UPN currently ignored; server uses configured shared mailbox internally).

## API Summary
- `GET /api/rooms` – list rooms.
- `GET /api/rooms/{id}` – room details.
- `POST /api/rooms/availability` – availability across rooms for a time range.
- `GET /api/rooms/{id}/bookings` – bookings for a room (optional date filter).
- `GET /api/bookings` – list bookings (optional overlapping window filter).
- `GET /api/bookings/{id}` – booking by id.
- `POST /api/bookings` – create booking (automatically attempts Graph event create).
- `PUT /api/bookings/{id}` – update booking (sync Graph event).
- `DELETE /api/bookings/{id}` – delete booking (delete Graph event if managed).
- `GET /api/bookings/user/{email}` – bookings for user.
- `GET /api/subscriptions` / `POST /api/subscriptions` / `DELETE /api/subscriptions/{id}` – manage Graph subscriptions.
- `GET /api/notifications?validationToken=...` – Graph validation handshake.
- `POST /api/notifications` – receive change notifications (always200, internal processing).

## Event Tagging & Safety
Bookify tags created events with an open extension (`com.bookify.metadata` + `source=bookify`). Update/delete operations verify this tag to avoid mutating user?created events in the shared mailbox.

## Attendee Sync
Inbound webhook updates (or proactive fetches) normalise attendee email addresses (trim + lowercase + distinct) into `Booking.Attendees`. Outbound create/update currently sets organiser only; room resource is implicit.

## Logging & Audit
`UpdateLog` captures all logical actions with timestamp + source (`web-app` for outbound user actions, `notification` for inbound Graph changes). Structured log event IDs defined in `ServiceLogEvents` categorize operations (fetch/create/update/delete/external sync).

## Security Notes
- Client secret should be stored securely (User Secrets / Key Vault). Do not commit secrets.
- Webhook endpoint must be publicly reachable & HTTPS; firewall Graph IPs if needed.
- Always validate `validationToken` during subscription handshake (already implemented).
- Encrypted resource data requires certificate named `webhooks` in Key Vault (private key exportable to decrypt payload).

## Extensibility
Potential next steps:
- Support recurring events (expand + conflict detection).
- Add authentication / authorization around booking operations.
- Multi?mailbox (per?room) support (remove shared mailbox assumption).
- Enhanced attendee management outbound (mirror `Booking.Attendees`).
- Replace `EnsureCreated()` with migrations for schema evolution.

## Quick Test (PowerShell)
```powershell
Invoke-RestMethod https://localhost:PORT/api/rooms
Invoke-RestMethod https://localhost:PORT/api/bookings -Method POST -Body '{"roomId":"1","bookedBy":"Alice","bookedByEmail":"alice@contoso.com","startTime":"2025-01-01T10:00:00Z","endTime":"2025-01-01T11:00:00Z","title":"Planning"}' -ContentType application/json
```

## License
Internal proof of concept – add license details if distributing externally.
