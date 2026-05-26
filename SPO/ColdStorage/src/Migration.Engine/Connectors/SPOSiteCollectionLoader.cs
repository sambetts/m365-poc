using Microsoft.SharePoint.Client;
using Entities.Configuration;
using Migration.Engine.Utils;
using Models;

using Microsoft.Extensions.Logging;
namespace Migration.Engine.Connectors;

public class SPOSiteCollectionLoader(Config config, string siteUrl, ILogger tracer) : BaseSharePointConnector(new SPOTokenManager(config, siteUrl, tracer), tracer), ISiteCollectionLoader<ListItemCollectionPosition>
{
    public async Task<List<IWebLoader<ListItemCollectionPosition>>> GetWebs()
    {
        var webs = new List<IWebLoader<ListItemCollectionPosition>>();

        var spClient = await TokenManager.GetOrRefreshContext();
        var rootWeb = spClient.Web;
        await TokenManager.EnsureContextWebIsLoaded(spClient);
        spClient.Load(rootWeb.Webs);
        await spClient.ExecuteQueryAsyncWithThrottleRetries(Tracer);

        webs.Add(new SPOWebLoader(spClient.Web, spClient, this));

        foreach (var subSweb in rootWeb.Webs)
        {
            webs.Add(new SPOWebLoader(subSweb, spClient, this));
        }

        return webs;
    }
}

