using Bot.Model.Models;
using Bot.Services.Bot;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bot.Services.Contract
{
    /// <summary>
    /// Interface IBotService
    /// Implements the <see cref="Bot.Model.Contracts.IInitializable" />
    /// </summary>
    /// <seealso cref="Bot.Model.Contracts.IInitializable" />
    public interface IBotService : Model.Contracts.IInitializable
    {
        Task ChangeSharingRoleAsync(string callLegId, ScreenSharingRole role);

        /// <summary>
        /// Gets the collection of call handlers.
        /// </summary>
        /// <value>The call handlers.</value>
        ConcurrentDictionary<string, CallHandler> CallHandlers { get; }

        /// <summary>
        /// Gets the entry point for stateful bot.
        /// </summary>
        /// <value>The client.</value>
        ICommunicationsClient Client { get; }

        /// <summary>
        /// End a particular call.
        /// </summary>
        /// <param name="callLegId">The call leg id.</param>
        /// <returns>The <see cref="Task" />.</returns>
        Task EndCallByCallLegIdAsync(string callLegId);

        /// <summary>
        /// Joins the call asynchronously.
        /// </summary>
        /// <param name="joinCallBody">The join call body.</param>
        /// <returns>The <see cref="ICall" /> that was requested to join.</returns>
        Task<ICall> JoinCallAsync(JoinCallBody joinCallBody);
    }
}
