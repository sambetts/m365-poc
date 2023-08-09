using Engine;
using Entities;
using Entities.Configuration;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers;

/// <summary>
/// Handles React app requests for app configuration
/// </summary>
[ApiController]
[Route("[controller]")]
public class AppConfigurationController : ControllerBase
{
    private readonly ILocationIpRuleLoader _locationIpRuleLoader;
    private readonly AppDbContext _context;
    private readonly AppConfig _config;

    public AppConfigurationController(ILocationIpRuleLoader locationIpRuleLoader, AppDbContext context, AppConfig config)
    {
        _locationIpRuleLoader = locationIpRuleLoader;
        this._context = context;
        this._config = config;
    }

    // Generate app ServiceConfiguration + storage configuration + key to read blobs
    // GET: AppConfiguration/ServiceConfiguration
    [HttpGet("[action]")]
    public async Task<ActionResult<ServiceConfiguration>> GetServiceConfiguration()
    {
        var r = new ClientNameResolver(_locationIpRuleLoader);

        // Return for react app
        return new ServiceConfiguration
        {
            ClientLocationInfo = await r.GetClientTerminalName(),
            AcsAccessKeyVal = _config.AcsAccessKeyVal,
            AcsEndpointVal = _config.AcsEndpointVal
        };
    }
}
