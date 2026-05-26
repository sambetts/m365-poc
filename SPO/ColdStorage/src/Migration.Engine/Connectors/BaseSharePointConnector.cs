using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using Entities.Configuration;
using Migration.Engine.Utils;

using Microsoft.Extensions.Logging;
namespace Migration.Engine.Connectors;

public abstract class BaseSharePointConnector(SPOTokenManager tokenManager, ILogger tracer)
{
    private readonly SPOTokenManager tokenManager = tokenManager;
    private readonly ILogger tracer = tracer;

    public ILogger Tracer => tracer;
    public SPOTokenManager TokenManager => tokenManager;
}

public abstract class BaseChildLoader(BaseSharePointConnector parent)
{
    public BaseSharePointConnector Parent { get; } = parent;
}
