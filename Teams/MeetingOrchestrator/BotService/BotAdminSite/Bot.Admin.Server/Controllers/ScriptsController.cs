using Bot.Admin.Models;
using Bot.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bot.Admin.Controllers;

/// <summary>
/// CRUD API for speech scripts stored in Azure Blob Storage.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScriptsController(IScriptStorageService storage) : ControllerBase
{
    /// <summary>
    /// Returns all saved scripts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var scripts = await storage.GetAllAsync(ct).ConfigureAwait(false);
        return Ok(scripts);
    }

    /// <summary>
    /// Returns a single script by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var script = await storage.GetByIdAsync(id, ct).ConfigureAwait(false);
        return script is null ? NotFound() : Ok(script);
    }

    /// <summary>
    /// Creates a new script.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ScriptDto script, CancellationToken ct)
    {
        script.Id = Guid.NewGuid().ToString();
        script.CreatedAt = DateTime.UtcNow;
        script.UpdatedAt = DateTime.UtcNow;

        var saved = await storage.UpsertAsync(script, ct).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, saved);
    }

    /// <summary>
    /// Updates an existing script.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ScriptDto script, CancellationToken ct)
    {
        var existing = await storage.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (existing is null)
            return NotFound();

        script.Id = id;
        script.CreatedAt = existing.CreatedAt;
        script.UpdatedAt = DateTime.UtcNow;

        var saved = await storage.UpsertAsync(script, ct).ConfigureAwait(false);
        return Ok(saved);
    }

    /// <summary>
    /// Deletes a script by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await storage.DeleteAsync(id, ct).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }
}
