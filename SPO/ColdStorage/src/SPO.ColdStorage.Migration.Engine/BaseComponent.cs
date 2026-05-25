using Microsoft.Extensions.Logging;
using SPO.ColdStorage.Entities.Configuration;

namespace SPO.ColdStorage.Migration.Engine;

public abstract class BaseComponent(Config config, ILogger logger)
{
    protected Config _config = config ?? throw new ArgumentNullException(nameof(config));
    protected ILogger _tracer = logger ?? throw new ArgumentNullException(nameof(logger));
}
