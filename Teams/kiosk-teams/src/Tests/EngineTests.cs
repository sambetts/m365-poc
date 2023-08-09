using Engine;
using Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests;

[TestClass]
public class EngineTests : AbstractTest
{

    [TestMethod]
    public async Task ClientNameResolverTests()
    {
        var rules = new List<LocationIpRule>()
        {
            new LocationIpRule() { IpAddress = "127.0.0.1", Order = 0, Subnet = "255.255.255.255", Name = "Local Exact Subnet" },
            new LocationIpRule() { IpAddress = "127.0.0.1", Order = 1, Subnet = "255.255.255.0", Name = "Local" },
            new LocationIpRule() { IpAddress = "192.168.0.1", Subnet = "255.255.0.0", Name = "Network" }
        };

        var fakeIpRulesLoader = new FakeLocationIpRuleLoader("127.0.0.1", rules);

        var resolverFakeIps = new ClientNameResolver(fakeIpRulesLoader);
        var r = await resolverFakeIps.GetClientTerminalName();

        Assert.IsTrue(r.Name == "Local Exact Subnet");


        var resolverNoIps = new ClientNameResolver(new FakeLocationIpRuleLoader("", new List<LocationIpRule>()));

        var defaultResult = await resolverNoIps.GetClientTerminalName();
        Assert.IsTrue(defaultResult.Name == _config!.DefaultLocationName);
    }
}

public class FakeLocationIpRuleLoader : ILocationIpRuleLoader
{
    private string _fakeIp;
    private IEnumerable<LocationIpRule> _fakeRules;

    public FakeLocationIpRuleLoader(string fakeIp, IEnumerable<LocationIpRule> fakeRules)
    {
        _fakeIp = fakeIp;
        _fakeRules = fakeRules;
    }

    public void SetIpAddress(string fakeIp)
    {
        _fakeIp = fakeIp;
    }
    public void SetRules(IEnumerable<LocationIpRule> fakeRules)
    {
        _fakeRules = fakeRules;
    }

    public string? GetIpAddress()
    {
        return _fakeIp;
    }

    public Task<IEnumerable<LocationIpRule>> LoadRules()
    {
        return Task.FromResult(_fakeRules);
    }
}
