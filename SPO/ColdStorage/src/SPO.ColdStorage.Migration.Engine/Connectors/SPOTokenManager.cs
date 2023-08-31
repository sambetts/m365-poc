using Microsoft.Identity.Client;
using Microsoft.SharePoint.Client;
using SPO.ColdStorage.Entities.Configuration;
using SPO.ColdStorage.Migration.Engine.Utils;

namespace SPO.ColdStorage.Migration.Engine.Connectors
{
    public class SPOTokenManager
    {
        private readonly Config _config;
        private readonly string _siteUrl;
        private readonly DebugTracer _tracer;
        private AuthenticationResult? _contextAuthResult = null;
        protected ClientContext? _context = null;

        public SPOTokenManager(Config config, string siteUrl, DebugTracer tracer)
        {
            _config = config;
            _siteUrl = siteUrl;
            _tracer = tracer;
        }
        public async Task<ClientContext> GetOrRefreshContext()
        {
            return await GetOrRefreshContext(null)!;
        }
        public async Task<ClientContext> GetOrRefreshContext(Action? newTokenCallback)
        {
            if (_contextAuthResult == null || _contextAuthResult.ExpiresOn < DateTime.Now.AddMinutes(-5))
            {
                _tracer.TrackTrace($"Refreshing SPO access token...");
                _context = await AuthUtils.GetClientContext(_config, _siteUrl, _tracer, (AuthenticationResult auth) => _contextAuthResult = auth);
                await EnsureContextWebIsLoaded(_context);

                if (newTokenCallback != null)
                {
                    newTokenCallback();
                }
            }
            return _context!;
        }
        public async Task EnsureContextWebIsLoaded(ClientContext spClient)
        {
            var loaded = false;
            try
            {
                // Test if this will blow up
                var url = spClient.Web.Url;
                url = spClient.Site.Url;
                loaded = true;
            }
            catch (PropertyOrFieldNotInitializedException)
            {
                loaded = false;
            }

            if (!loaded)
            {
                spClient.Load(spClient.Web);
                spClient.Load(spClient.Site, s => s.Url);
                await spClient.ExecuteQueryAsyncWithThrottleRetries(_tracer);
            }
        }
    }
}
