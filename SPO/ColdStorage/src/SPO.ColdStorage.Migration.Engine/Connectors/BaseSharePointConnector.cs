using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine.Utils;

namespace SPO.ColdStorage.Migration.Engine.Connectors
{
    public abstract class BaseSharePointConnector
    {
        private readonly SPOTokenManager tokenManager;
        private readonly DebugTracer tracer;

        public BaseSharePointConnector(SPOTokenManager tokenManager, DebugTracer tracer)
        {
            this.tokenManager = tokenManager;
            this.tracer = tracer;
        }

        public DebugTracer Tracer => tracer;
        public SPOTokenManager TokenManager => tokenManager;
    }

    public abstract class BaseChildLoader
    {
        public BaseChildLoader(BaseSharePointConnector parent)
        {
            Parent = parent;
        }

        public BaseSharePointConnector Parent { get; }
    }
}
