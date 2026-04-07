using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Bot.Admin.Models;

namespace Bot.Admin.Services;

/// <summary>
/// Stores speech scripts as JSON blobs in an Azure Blob Storage container.
/// </summary>
public class ScriptStorageService : IScriptStorageService
{
    private const string ContainerName = "scripts";
    private readonly BlobContainerClient _container;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public ScriptStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureStorage")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:AzureStorage' configuration. " +
                "Add it via user secrets: dotnet user-secrets set \"ConnectionStrings:AzureStorage\" \"UseDevelopmentStorage=true\"");

        var serviceClient = new BlobServiceClient(connectionString,
            new BlobClientOptions(BlobClientOptions.ServiceVersion.V2025_01_05));
        _container = serviceClient.GetBlobContainerClient(ContainerName);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ScriptDto>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct).ConfigureAwait(false);

        var scripts = new List<ScriptDto>();
        await foreach (var blob in _container.GetBlobsAsync(cancellationToken: ct).ConfigureAwait(false))
        {
            var script = await DownloadScriptAsync(blob.Name, ct).ConfigureAwait(false);
            if (script is not null)
                scripts.Add(script);
        }

        return scripts.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    /// <inheritdoc/>
    public async Task<ScriptDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct).ConfigureAwait(false);
        return await DownloadScriptAsync(BlobName(id), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ScriptDto> UpsertAsync(ScriptDto script, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(script.Id))
            script.Id = Guid.NewGuid().ToString();

        script.UpdatedAt = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(script, JsonOptions);
        var blob = _container.GetBlobClient(BlobName(script.Id));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
        }, ct).ConfigureAwait(false);

        return script;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct).ConfigureAwait(false);
        var blob = _container.GetBlobClient(BlobName(id));
        var response = await blob.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        return response.Value;
    }

    /// <inheritdoc/>
    public async Task EnsureDefaultScriptAsync(CancellationToken ct = default)
    {
        await EnsureContainerAsync(ct).ConfigureAwait(false);

        await foreach (var _ in _container.GetBlobsAsync(cancellationToken: ct).ConfigureAwait(false))
        {
            return; // At least one script exists — nothing to do.
        }

        await UpsertAsync(CreateDefaultScript(), ct).ConfigureAwait(false);
    }

    private async Task<ScriptDto?> DownloadScriptAsync(string blobName, CancellationToken ct)
    {
        var blob = _container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(ct).ConfigureAwait(false))
            return null;

        var download = await blob.DownloadContentAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<ScriptDto>(download.Value.Content.ToString(), JsonOptions);
    }

    private async Task EnsureContainerAsync(CancellationToken ct)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_initialized)
            {
                await _container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string BlobName(string id) => $"{id}.json";

    private static ScriptDto CreateDefaultScript()
    {
        var now = DateTime.UtcNow;
        return new ScriptDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Sample Conversation",
            Description = "A default meeting conversation to get started.",
            DefaultLanguage = "en-US",
            CreatedAt = now,
            UpdatedAt = now,
            Paragraphs =
            [
                new() { Text = "Good morning everyone, thanks for joining the call.", PauseAfterSeconds = 2 },
                new() { Text = "Let's start with a quick round of updates. How did last sprint go?", PauseAfterSeconds = 3 },
                new() { Text = "We completed the API integration ahead of schedule, which was great.", PauseAfterSeconds = 2 },
                new() { Text = "Nice work. Were there any blockers on the front-end side?", PauseAfterSeconds = 3 },
                new() { Text = "We hit a small issue with authentication redirects, but it's resolved now.", PauseAfterSeconds = 2 },
                new() { Text = "Good to hear. Let's talk about priorities for this sprint.", PauseAfterSeconds = 3 },
                new() { Text = "I think we should focus on the admin dashboard and script management features.", PauseAfterSeconds = 2 },
                new() { Text = "Agreed. We also need to finalize the deployment pipeline.", PauseAfterSeconds = 3 },
                new() { Text = "I can take the pipeline work. Should have it ready by Wednesday.", PauseAfterSeconds = 2 },
                new() { Text = "Perfect. Any other topics before we wrap up?", PauseAfterSeconds = 3 },
                new() { Text = "Nothing from my side. Looks like we have a solid plan.", PauseAfterSeconds = 2 },
                new() { Text = "Great, thanks everyone. Let's reconvene on Thursday. Have a good day!", PauseAfterSeconds = 0 },
            ]
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
