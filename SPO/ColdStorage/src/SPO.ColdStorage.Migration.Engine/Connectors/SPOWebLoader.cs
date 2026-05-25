using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Migration.Engine.Utils;
using SPO.ColdStorage.Models;

using Microsoft.Extensions.Logging;
namespace SPO.ColdStorage.Migration.Engine.Connectors;

public class SPOWebLoader(Web sPWeb, ClientContext clientContext, BaseSharePointConnector baseSharePointConnector) : BaseChildLoader(baseSharePointConnector), IWebLoader<ListItemCollectionPosition>
{
    private readonly ClientContext _clientContext = clientContext;

    public Web SPWeb { get; set; } = sPWeb;

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
                Parent.Tracer.LogInformation($"Got exception '{ex.Message}' loading data for list ID '{list.Id}' - not configured to analyse.");
            }

            if (listReadSuccess)
            {
                // Do not search through system or hidden lists
                if (!list.Hidden && !list.IsSystemList)
                {
                    Parent.Tracer.LogInformation($"Found '{list.Title}'...");
                    lists.Add(new SPOListLoader(list, Parent));
                }
                else
                {
                    Parent.Tracer.LogInformation($"Ignoring system/hidden list '{list.Title}'.");
                }
            }
        }

        return lists;
    }
}
