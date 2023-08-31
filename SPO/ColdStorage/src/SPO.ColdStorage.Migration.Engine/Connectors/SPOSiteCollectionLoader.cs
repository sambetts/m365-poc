using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine.Utils;
using SPO.ColdStorage.Models;

namespace SPO.ColdStorage.Migration.Engine.Connectors
{
    public class SPOSiteCollectionLoader : BaseSharePointConnector, ISiteCollectionLoader<ListItemCollectionPosition>
    {
        public SPOSiteCollectionLoader(Config config, string siteUrl, DebugTracer tracer) : base(new SPOTokenManager(config, siteUrl, tracer), tracer)
        {
        }

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



}
