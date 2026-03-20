using Bot.Services.Contract;
using Microsoft.Graph.Communications.Calls;

namespace Bot.Services.Bot;

/// <summary>
/// Default implementation of <see cref="ICallHandlerFactory"/>.
/// </summary>
public class DefaultCallHandlerFactory : ICallHandlerFactory
{
    private readonly IAzureSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCallHandlerFactory"/> class.
    /// </summary>
    /// <param name="settings">The Azure settings.</param>
    public DefaultCallHandlerFactory(IAzureSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    public CallHandler Create(ICall call) => new(call, _settings);
}
