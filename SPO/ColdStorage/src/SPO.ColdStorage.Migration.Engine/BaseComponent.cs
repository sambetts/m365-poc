using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using System.Text;

namespace SPO.ColdStorage.Migration.Engine;

public abstract class BaseComponent(Config config, DebugTracer debugTracer)
{

    protected Config _config = config ?? throw new ArgumentNullException(nameof(config));
    protected DebugTracer _tracer = debugTracer;
}
