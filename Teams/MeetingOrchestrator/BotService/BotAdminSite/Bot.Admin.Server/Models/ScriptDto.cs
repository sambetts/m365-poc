namespace Bot.Admin.Models;

/// <summary>
/// Represents a saved speech script.
/// </summary>
public class ScriptDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string DefaultLanguage { get; set; } = "en-US";
    public List<ParagraphDto> Paragraphs { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A single paragraph within a speech script.
/// </summary>
public class ParagraphDto
{
    public string Text { get; set; } = "";
    public string? Language { get; set; }
    public double PauseBeforeSeconds { get; set; }
    public double PauseAfterSeconds { get; set; }
}

/// <summary>
/// Request body for joining a bot to a Teams meeting.
/// </summary>
public class JoinCallRequest
{
    public string JoinUrl { get; set; } = "";
    public string? DisplayName { get; set; }
}

/// <summary>
/// Response returned after a bot joins a call.
/// </summary>
public class JoinCallResponse
{
    public string? CallId { get; set; }
    public Guid ScenarioId { get; set; }
    public string? CallUri { get; set; }
}

/// <summary>
/// Request body for starting a script on an active bot.
/// </summary>
public class StartScriptRequest
{
    public string CallId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ScriptId { get; set; } = "";
}
