namespace Bot.Model.Models;

/// <summary>
/// Request body for starting a speech script on an active call.
/// </summary>
public class StartScriptBody
{
    /// <summary>
    /// Gets or sets the call leg ID of the active call.
    /// </summary>
    public string CallId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the bot instance to target.
    /// Must match the name used when joining the call.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the script to speak.
    /// </summary>
    public SpeechScript Script { get; set; }
}
