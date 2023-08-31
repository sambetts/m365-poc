using CommonUtils;
using SPOAzBlob.Engine.Models;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Manager for Graph drive delta tokens
    /// </summary>
    public class DriveDeltaTokenManager : AbstractGraphManager
    {
        private readonly AzureStorageManager _azureStorageManager;
        private string propNameForSite;
        public DriveDeltaTokenManager(Config config, DebugTracer trace, AzureStorageManager azureStorageManager) : base(config, trace)
        {
            propNameForSite = $"Delta:{_config.SharePointSiteId}";
            this._azureStorageManager = azureStorageManager;
        }

        public async Task SetToken(string token)
        {
            await _azureStorageManager.SetPropertyValue(propNameForSite, token);
        }

        public async Task<DriveDelta?> GetToken()
        {
            var propVal = await _azureStorageManager.GetPropertyValue(propNameForSite);
            if (propVal?.Value != null)
            {
                return new DriveDelta(propVal!);
            }
            else
            {
                return null;
            }
        }


        public async Task DeleteToken()
        {
            await _azureStorageManager.ClearPropertyValue(propNameForSite);
        }
    }
}
