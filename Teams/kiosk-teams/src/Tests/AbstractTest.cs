using Entities;
using Entities.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Tests;

public class AbstractTest
{

    protected const string FILE_CONTENTS = "En un lugar de la Mancha, de cuyo nombre no quiero acordarme, no ha mucho tiempo que vivía un hidalgo de los de lanza en astillero, adarga antigua, rocín flaco y galgo corredor";

    protected AppConfig? _config;

    [TestInitialize]
    public async Task Init()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", true);


        var config = builder.Build();
        _config = new AppConfig(config);

        // Init DB
        using (var db = new AppDbContext(_config!))
        {
            await DbInitializer.Init(db);
        }
    }
}
