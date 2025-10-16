using CommonUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace OfficeNotifications.Engine
{
    /// <summary>
    /// Handle Graph User looksup
    /// </summary>
    public class GraphUserManager : AbstractGraphManager
    {
        public GraphUserManager(Config config, ILogger trace) :base (config, trace)
        {
        }

        public async Task<User> GetUserByEmail(string email)
        {
            var searchResults = await _client.Users[email].GetAsync();
            return searchResults;
        }

        public static string GetUserName(User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.DisplayName;
        }
    }
}
