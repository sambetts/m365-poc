using CommonUtils.Config;
using Microsoft.Extensions.Configuration;
using SPOAzBlob.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Tests
{
    public class TestConfig : Config
    {
        public TestConfig(IConfiguration config) : base(config)
        {
        }

        [ConfigValue]
        public string AzureAdAppDisplayName { get; set; } = string.Empty;

        [ConfigValue]
        public string TestEmailAddress { get; set; } = string.Empty;
    }
}
