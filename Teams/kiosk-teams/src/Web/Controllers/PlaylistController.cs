using Entities;
using Entities.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers;

[ApiController]
[Route("[controller]")]
public class PlaylistController : ControllerBase
{
    private readonly AppDbContext _context;

    public PlaylistController(ILogger<PlaylistController> logger, AppDbContext context, AppConfig config)
    {
        this._context = context;
    }

    // GET: Playlist
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayListItem>>> GetPlaylistForClientScope(string scope)
    {
        if (string.IsNullOrEmpty(scope))
        {
            return BadRequest("No search term defined");
        }
        else
        {
            return await _context.PlayList
                .Where(m => m.Scope == null || m.Scope.ToLower() == (scope))
                .ToListAsync();
        }
    }

    // GET: Playlist/now
    [HttpGet("now")]
    public async Task<ActionResult<PlayListItem?>> GetCurrentPlaylistForClientScope(string scope)
    {
        if (string.IsNullOrEmpty(scope))
        {
            return BadRequest("No search term defined");
        }
        else
        {
            var onRightNow = await _context.PlayList
                .Where(m => (m.Scope == null || m.Scope.ToLower() == scope.ToLower()) && m.Start <= DateTime.Now && m.End >= DateTime.Now)
                .FirstOrDefaultAsync();

            return onRightNow;
        }
    }
}
