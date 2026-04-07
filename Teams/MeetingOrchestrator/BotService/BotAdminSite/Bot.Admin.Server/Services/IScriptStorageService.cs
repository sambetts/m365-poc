using Bot.Admin.Models;

namespace Bot.Admin.Services;

/// <summary>
/// Manages CRUD operations for speech scripts in Azure Blob Storage.
/// </summary>
public interface IScriptStorageService
{
    /// <summary>
    /// Returns all saved scripts.
    /// </summary>
    Task<IReadOnlyList<ScriptDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single script by ID, or <c>null</c> if not found.
    /// </summary>
    Task<ScriptDto?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a script.
    /// </summary>
    Task<ScriptDto> UpsertAsync(ScriptDto script, CancellationToken ct = default);

    /// <summary>
    /// Deletes a script by ID. Returns <c>true</c> if it existed.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Seeds storage with a default sample script if no scripts exist.
    /// </summary>
    Task EnsureDefaultScriptAsync(CancellationToken ct = default);
}
