# Migration.SiteSnapshotBuilder

Builds per-site snapshots (drives + files) of a SharePoint Online tenant via the
Microsoft Graph API only. There is no CSOM/PnP code path and no certificate or
Key Vault dependency — authentication is always client-credential (client secret).

Scope is intentionally narrow: **document libraries (drives) and files only**.
Custom lists, list items, and attachments are out of scope.

## Configuration

`appsettings.json`:

```json
{
  "BaseServerAddress": "https://yourtenant.sharepoint.com",
  "ConnectionStrings": {
    "SQLConnectionString": "Server=localhost;Database=SPOColdStorage;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientID": "your-client-id",
    "Secret": "your-client-secret"
  }
}
```

| Setting | Required | Description |
|---------|----------|-------------|
| `BaseServerAddress` | Yes | SharePoint root URL (e.g. `https://contoso.sharepoint.com`) |
| `ConnectionStrings:SQLConnectionString` | Yes | SQL Server connection used to persist the snapshot |
| `AzureAd:TenantId` | Yes | Azure AD tenant GUID |
| `AzureAd:ClientID` | Yes | App registration (client) ID |
| `AzureAd:Secret` | Yes | App registration client secret value |
| `BlobContainerName` | No | Only required when running migration/indexer projects |
| `SearchIndexName` | No | Only required when running search indexing |
| `AppInsightsInstrumentationKey` | No | Optional telemetry |

## Azure AD application permissions

The app registration needs the following **application** Microsoft Graph permissions
(admin consent required):

- `Sites.Read.All`
- `Files.Read.All`

No SharePoint REST or CSOM permissions are required.

## Running

```powershell
cd Migration.SiteSnapshotBuilder
dotnet run
```

The snapshot builder enumerates sites via `GET /sites/getAllSites`, then for each
site walks every drive and uses Graph delta queries to discover files. Results are
persisted to the SQL database.

## Environment variable equivalents

```powershell
$env:BaseServerAddress = "https://yourtenant.sharepoint.com"
$env:ConnectionStrings__SQLConnectionString = "Server=localhost;..."
$env:AzureAd__TenantId = "your-tenant-id"
$env:AzureAd__ClientID = "your-client-id"
$env:AzureAd__Secret = "your-client-secret"

dotnet run
```

Double underscores (`__`) map to nested configuration sections.

## Troubleshooting

- **`AADSTS7000215` (Invalid client secret)** — confirm you copied the secret **Value**, not the ID, and that it has not expired.
- **`Sites.Read.All` permission error** — make sure admin consent has been granted for the application permissions and that you waited a minute or two for propagation.
- **SQL connection error** — verify SQL Server is reachable and the connection string is correct.
