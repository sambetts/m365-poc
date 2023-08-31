using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Migration.Engine
{
    public abstract class BaseComponent
    {

        protected Config _config;
        protected DebugTracer _tracer;

        public BaseComponent(Config config, DebugTracer debugTracer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tracer = debugTracer;
        }
    }
}
