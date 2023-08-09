using Entities;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Engine;

public class ClientNameResolver 
{
    private readonly ILocationIpRuleLoader _locationIpRuleLoader;

    public ClientNameResolver(ILocationIpRuleLoader locationIpRuleLoader)
    {
        _locationIpRuleLoader = locationIpRuleLoader;
    }

    public async Task<LocationInfo> GetClientTerminalName()
    {
        var rules = await _locationIpRuleLoader.LoadRules();
        var clientIpStr = _locationIpRuleLoader.GetIpAddress();


        var matchingRules = new List<LocationIpRule>();
        IPAddress? clientIp = null;
        if (IPAddress.TryParse(clientIpStr, out clientIp))
        {
            foreach (var rule in rules)
            {
                IPAddress? ruleClientIp = null, ruleSubnetIp = null;
                if (IPAddress.TryParse(rule.IpAddress, out ruleClientIp) && IPAddress.TryParse(rule.Subnet, out ruleSubnetIp))
                {
                    if (clientIp.IsInSameSubnet(ruleClientIp, ruleSubnetIp))
                    {
                        matchingRules.Add(rule);
                    }
                }
            }

            if (matchingRules.Count > 0)
            {
                var info = new LocationInfo
                {
                    Name = matchingRules.OrderBy(r => r.Order).First().Name,
                    Description = clientIpStr ?? "No IP Address"
                };
                return info;
            }
        }

        return new LocationInfo
        {
            Name = clientIpStr == "::1" ? "local" : "remote",
            Description = clientIpStr ?? "No IP Address"
        };
    }
}
