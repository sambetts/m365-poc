using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Configuration;

public class ConnectionStrings : BaseConfig
{
    public ConnectionStrings(Microsoft.Extensions.Configuration.IConfigurationSection config) : base(config)
    {
    }

    [ConfigValue]
    public string SQLConnectionString { get; set; } = string.Empty;


}
