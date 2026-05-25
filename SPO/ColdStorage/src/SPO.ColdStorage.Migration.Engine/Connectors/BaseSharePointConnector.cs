using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine.Utils;

namespace SPO.ColdStorage.Migration.Engine.Connectors;

public abstract class BaseSharePointConnector(SPOTokenManager tokenManager, DebugTracer tracer)
{
    private readonly SPOTokenManager tokenManager = tokenManager;
    private readonly DebugTracer tracer = tracer;

    public DebugTracer Tracer => tracer;
    public SPOTokenManager TokenManager => tokenManager;
}

public abstract class BaseChildLoader(BaseSharePointConnector parent)
{
    public BaseSharePointConnector Parent { get; } = parent;
}
