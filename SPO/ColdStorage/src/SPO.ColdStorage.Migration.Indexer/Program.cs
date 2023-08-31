

using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Migration.Engine;
using SPO.ColdStorage.Migration.Engine.Utils;

Console.WriteLine("SPO Cold Storage - SharePoint Indexer");

var config = ConsoleUtils.GetConfigurationWithDefaultBuilder();
ConsoleUtils.PrintCommonStartupDetails();

// Send to application insights or just the stdout?
DebugTracer tracer;
if (config.HaveAppInsightsConfigured)
{
    tracer = new DebugTracer(config.AppInsightsInstrumentationKey, "Indexer");
}
else
    tracer = DebugTracer.ConsoleOnlyTracer();


// Init DB
using (var db = new SPOColdStorageDbContext(config))
{
    await DbInitializer.Init(db, config.DevConfig);
}

// Start discovery
var discovery = new SharePointContentIndexer(config, tracer);
await discovery.StartMigrateAllSites();


Console.WriteLine("\nAll sites scanned. Finished indexing.");
