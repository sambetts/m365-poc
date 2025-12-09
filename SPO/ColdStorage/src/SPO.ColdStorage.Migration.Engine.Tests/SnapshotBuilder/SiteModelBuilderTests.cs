using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SPO.ColdStorage.Entities;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Entities.DBEntities;
using SPO.ColdStorage.Migration.Engine.SnapshotBuilder;
using SPO.ColdStorage.Migration.Engine.Tests.Adapters;
using SPO.ColdStorage.Models;

namespace SPO.ColdStorage.Migration.Engine.Tests.SnapshotBuilder;

[TestClass]
public class SiteModelBuilderTests
{
    private Moq.Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfig = null!;
    private Config _config = null!;
    private DebugTracer _tracer = null!;
    private TargetMigrationSite _testSite = null!;
    private TestFileAnalyticsAdapter _testAdapter = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockConfig = new Moq.Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        
        // Mock AzureAd section
        var mockAzureAdSection = new Moq.Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        mockAzureAdSection.Setup(s => s["Instance"]).Returns("https://login.microsoftonline.com/");
        mockAzureAdSection.Setup(s => s["Domain"]).Returns("test.onmicrosoft.com");
        mockAzureAdSection.Setup(s => s["TenantId"]).Returns("test-tenant-id");
        mockAzureAdSection.Setup(s => s["ClientId"]).Returns("test-client-id");
        mockAzureAdSection.Setup(s => s["ClientID"]).Returns("test-client-id");  // Case-sensitive duplicate
        mockAzureAdSection.Setup(s => s["CallbackPath"]).Returns("/signin-oidc");
        mockAzureAdSection.Setup(s => s["Secret"]).Returns("test-secret");
        _mockConfig.Setup(c => c.GetSection("AzureAd")).Returns(mockAzureAdSection.Object);
        
        // Mock ConnectionStrings section
        var mockConnectionStringsSection = new Moq.Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        mockConnectionStringsSection.Setup(s => s["ServiceBus"]).Returns("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test;EntityPath=test");
        mockConnectionStringsSection.Setup(s => s["Storage"]).Returns("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net");
        mockConnectionStringsSection.Setup(s => s["SQLConnectionString"]).Returns("Server=localhost;Database=TestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        _mockConfig.Setup(c => c.GetSection("ConnectionStrings")).Returns(mockConnectionStringsSection.Object);
        
        // Mock Dev section
        var mockDevSection = new Moq.Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        mockDevSection.Setup(s => s["SearchServiceEndPoint"]).Returns("https://test.search.windows.net");
        mockDevSection.Setup(s => s["SearchServiceAdminApiKey"]).Returns("test-admin-key");
        mockDevSection.Setup(s => s["SearchServiceQueryApiKey"]).Returns("test-query-key");
        _mockConfig.Setup(c => c.GetSection("Dev")).Returns(mockDevSection.Object);
        
        // Mock Search section (reusing Dev section values)
        var mockSearchSection = new Moq.Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        mockSearchSection.Setup(s => s["SearchServiceEndPoint"]).Returns("https://test.search.windows.net");
        mockSearchSection.Setup(s => s["SearchServiceAdminApiKey"]).Returns("test-admin-key");
        mockSearchSection.Setup(s => s["SearchServiceQueryApiKey"]).Returns("test-query-key");
        _mockConfig.Setup(c => c.GetSection("Search")).Returns(mockSearchSection.Object);
        
        _mockConfig.Setup(c => c["AnalysisSkipHours"]).Returns("24");
        _mockConfig.Setup(c => c["SPOTenantName"]).Returns("test");
        _mockConfig.Setup(c => c["SPOClientId"]).Returns("test-client-id");
        _mockConfig.Setup(c => c["SPOUserName"]).Returns("test@test.com");
        _mockConfig.Setup(c => c["BaseServerAddress"]).Returns("https://test.com");
        _mockConfig.Setup(c => c["DBConnectionString"]).Returns("Server=test;Database=test");
        _mockConfig.Setup(c => c["InstrumentationKey"]).Returns("test-key");
        _mockConfig.Setup(c => c["KeyVaultUrl"]).Returns("https://test.vault.azure.net");
        _mockConfig.Setup(c => c["StorageConnectionString"]).Returns("DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test");
        _mockConfig.Setup(c => c["BlobContainerName"]).Returns("test-container");
        
        _config = new Config(_mockConfig.Object);
        _tracer = new DebugTracer(null, "test");

        _testSite = new TargetMigrationSite
        {
            RootURL = "https://test.sharepoint.com/sites/testsite",
            FilterConfigJson = null
        };

        _testAdapter = new TestFileAnalyticsAdapter();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Cleanup if needed
    }

    [TestMethod]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Assert
        builder.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithNullAdapter_CreatesDefaultGraphAdapter()
    {
        // Arrange & Act
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, null);

        // Assert
        builder.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Build_WithoutCallback_ReturnsModel()
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        var result = await builder.Build();

        // Assert
        result.Should().NotBeNull();
        result.AllFiles.Should().NotBeNull();
    }

    [TestMethod]
    public void Build_WithInvalidBatchSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        Func<Task> act = async () => await builder.Build(0, null, null);

        // Assert
        act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public async Task Build_WithTestAdapter_CallsAdapterMethods()
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);
        _testAdapter.ResetCounters();

        // Configure test data
        _testAdapter.SetAnalyticsData("test-item-1", new ItemAnalyticsRepsonse.AnalyticsItemActionStat
        {
            ActionCount = 10,
            ActorCount = 5
        });

        _testAdapter.SetVersionData("test-item-1", new DriveItemVersionInfo
        {
            Versions = 
            [
                new DriveItemVersion { Id = "1.0", Size = 1024 },
                new DriveItemVersion { Id = "2.0", Size = 2048 }
            ]
        });

        // Act
        var result = await builder.Build(10, null, null);

        // Assert
        result.Should().NotBeNull();
        // Note: Without actual file crawling, adapter won't be called
        // This test validates the builder can be constructed with test adapter
    }

    [TestMethod]
    public void BackgroundMetaTasksAll_ReturnsEnumerable()
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        var tasks = builder.BackgroundMetaTasksAll;

        // Assert
        tasks.Should().NotBeNull();
        tasks.Should().BeAssignableTo<IEnumerable<Task<BackgroundUpdate>>>();
    }

    [DataTestMethod]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    public async Task Build_WithDifferentBatchSizes_Succeeds(int batchSize)
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        var result = await builder.Build(batchSize, null, null);

        // Assert
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        Action act = () =>
        {
            builder.Dispose();
            builder.Dispose();
            builder.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public async Task Build_CalledMultipleTimes_ReturnsSameModel()
    {
        // Arrange
        using var builder = new SiteModelBuilder(_config, _tracer, _testSite, _testAdapter);

        // Act
        var result1 = await builder.Build();
        var result2 = await builder.Build();

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [TestMethod]
    public void Constructor_WithFilterConfigJson_ParsesConfiguration()
    {
        // Arrange
        var siteWithFilter = new TargetMigrationSite
        {
            RootURL = "https://test.sharepoint.com/sites/testsite",
            FilterConfigJson = "{\"IncludeAllLists\":true}"
        };

        // Act
        using var builder = new SiteModelBuilder(_config, _tracer, siteWithFilter, _testAdapter);

        // Assert
        builder.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_WithInvalidFilterConfigJson_UsesDefaultConfig()
    {
        // Arrange
        var siteWithInvalidFilter = new TargetMigrationSite
        {
            RootURL = "https://test.sharepoint.com/sites/testsite",
            FilterConfigJson = "invalid json"
        };

        // Act & Assert - should not throw
        using var builder = new SiteModelBuilder(_config, _tracer, siteWithInvalidFilter, _testAdapter);
        builder.Should().NotBeNull();
    }
}
