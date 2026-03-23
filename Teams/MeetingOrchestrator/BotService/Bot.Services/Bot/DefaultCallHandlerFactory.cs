using Bot.Services.Contract;
using Microsoft.Graph.Communications.Calls;

namespace Bot.Services.Bot;

/// <summary>
/// Default implementation of <see cref="ICallHandlerFactory"/>.
/// </summary>
public class DefaultCallHandlerFactory : ICallHandlerFactory
{
    private readonly ITextToSpeechService _ttsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCallHandlerFactory"/> class.
    /// </summary>
    /// <param name="ttsService">The TTS service for speech playback.</param>
    public DefaultCallHandlerFactory(ITextToSpeechService ttsService)
    {
        _ttsService = ttsService;
    }

    /// <inheritdoc />
    public CallHandler Create(ICall call, string displayName) => new(call, _ttsService, displayName);
}
