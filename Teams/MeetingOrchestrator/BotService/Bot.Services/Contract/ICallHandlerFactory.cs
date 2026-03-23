using Bot.Services.Bot;
using Microsoft.Graph.Communications.Calls;

namespace Bot.Services.Contract;

/// <summary>
/// Factory for creating <see cref="CallHandler"/> instances,
/// decoupling handler construction from the bot service.
/// </summary>
public interface ICallHandlerFactory
{
    /// <summary>
    /// Creates a new <see cref="CallHandler"/> for the specified call.
    /// </summary>
    /// <param name="call">The stateful call.</param>
    /// <param name="displayName">The display name the bot used when joining.</param>
    /// <returns>A new <see cref="CallHandler"/> instance.</returns>
    CallHandler Create(ICall call, string displayName);
}
