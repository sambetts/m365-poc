using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;

namespace SPO.ColdStorage.Tests;

public class AbstractTest
{

    protected const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";

    protected Config? _config;
    protected ILogger _tracer = NullLogger.Instance;

    [TestInitialize]
    public async Task Init()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", true);

        var config = builder.Build();
        _config = new Config(config);

        // Init DB
        using var db = new SPOColdStorageDbContext(_config!);
        await DbInitializer.Init(db, _config.DevConfig!);
    }
}
