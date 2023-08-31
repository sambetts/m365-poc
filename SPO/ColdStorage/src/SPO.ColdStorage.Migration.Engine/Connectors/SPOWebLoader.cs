using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Migration.Engine.Utils;
using SPO.ColdStorage.Models;

namespace SPO.ColdStorage.Migration.Engine.Connectors
{

    public class SPOWebLoader : BaseChildLoader, IWebLoader<ListItemCollectionPosition>
    {
        private readonly ClientContext _clientContext;

        public SPOWebLoader(Web sPWeb, ClientContext clientContext, BaseSharePointConnector baseSharePointConnector) : base(baseSharePointConnector)
        {
            SPWeb = sPWeb;
            _clientContext = clientContext;
        }
        public Web SPWeb { get; set; } = null!;


        public async Task<List<IListLoader<ListItemCollectionPosition>>> GetLists()
        {
            var lists = new List<IListLoader<ListItemCollectionPosition>>();

            _clientContext.Load(SPWeb.Lists);
            await _clientContext.ExecuteQueryAsyncWithThrottleRetries(Parent.Tracer);

            foreach (var list in SPWeb.Lists)
            {
                var listReadSuccess = false;
                try
                {
                    _clientContext.Load(list, l => l.IsSystemList);
                    await _clientContext.ExecuteQueryAsyncWithThrottleRetries(Parent.Tracer);
                    listReadSuccess = true;
                }
                catch (System.Net.WebException ex)
                {
                    Parent.Tracer.TrackTrace($"Got exception '{ex.Message}' loading data for list ID '{list.Id}' - not configured to analyse.");
                }

                if (listReadSuccess)
                {
                    // Do not search through system or hidden lists
                    if (!list.Hidden && !list.IsSystemList)
                    {
                        Parent.Tracer.TrackTrace($"Found '{list.Title}'...");
                        lists.Add(new SPOListLoader(list, Parent));
                    }
                    else
                    {
                        Parent.Tracer.TrackTrace($"Ignoring system/hidden list '{list.Title}'.");
                    }
                }
            }

            return lists;
        }
    }
}
