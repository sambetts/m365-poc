namespace Bookify.Server.Services;

// Centralised logging event IDs for structured logging across services
internal static class ServiceLogEvents
{
    public const int Fetch = 1000;
    public const int AvailabilityCheck = 1100;
    public const int Create = 2000;
    public const int Update = 3000;
    public const int Delete = 4000;
    public const int Conflict = 4090;
    public const int ValidationFailed = 4220;
    public const int ExternalSync = 5000;
    public const int ExternalFetch = 5100;
    public const int ExternalCreate = 5200;
    public const int ExternalUpdate = 5300;
    public const int ExternalDelete = 5400;
}
