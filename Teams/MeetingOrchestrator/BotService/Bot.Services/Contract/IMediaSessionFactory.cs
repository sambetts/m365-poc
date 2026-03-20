using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using System;

namespace Bot.Services.Contract;

/// <summary>
/// Factory for creating <see cref="ILocalMediaSession"/> instances,
/// decoupling media session construction from the bot service.
/// </summary>
public interface IMediaSessionFactory
{
    /// <summary>
    /// Creates a new local media session with standard audio, video, and VBSS sockets.
    /// </summary>
    /// <param name="client">The communications client used to create the session.</param>
    /// <param name="mediaSessionId">Optional media session identifier.</param>
    /// <returns>A configured <see cref="ILocalMediaSession"/>.</returns>
    ILocalMediaSession Create(ICommunicationsClient client, Guid mediaSessionId = default);
}
