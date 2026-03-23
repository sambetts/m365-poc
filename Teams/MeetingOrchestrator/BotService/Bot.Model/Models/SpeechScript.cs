using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bot.Model.Models;

/// <summary>
/// A structured speech script composed of paragraphs, each with optional
/// language, pause-before, and pause-after configuration.
/// </summary>
public class SpeechScript
{
    /// <summary>
    /// Default spoken language applied when a paragraph does not specify one.
    /// </summary>
    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    /// Ordered list of paragraphs to speak.
    /// </summary>
    [JsonPropertyName("paragraphs")]
    public List<SpeechScriptParagraph> Paragraphs { get; set; } = [];

    /// <summary>
    /// Parses script content. Accepts JSON with paragraph metadata, or falls
    /// back to treating blank-line-separated blocks as individual paragraphs.
    /// </summary>
    public static SpeechScript Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Try structured JSON first.
        try
        {
            var script = JsonSerializer.Deserialize<SpeechScript>(content);
            if (script?.Paragraphs is { Count: > 0 })
                return script;
        }
        catch (JsonException) { }

        // Fallback: plain text split by blank lines.
        var paragraphs = new List<SpeechScriptParagraph>();
        foreach (var block in content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = block.Trim();
            if (trimmed.Length > 0)
                paragraphs.Add(new SpeechScriptParagraph { Text = trimmed });
        }

        if (paragraphs.Count == 0 && content.Trim().Length > 0)
            paragraphs.Add(new SpeechScriptParagraph { Text = content.Trim() });

        return new SpeechScript { Paragraphs = paragraphs };
    }
}

/// <summary>
/// A single paragraph within a <see cref="SpeechScript"/>.
/// </summary>
public class SpeechScriptParagraph
{
    /// <summary>The text to speak.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// <summary>
    /// BCP-47 language tag (e.g. "en-US", "fr-FR"). When <c>null</c>, the
    /// script's <see cref="SpeechScript.DefaultLanguage"/> is used.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>Seconds of silence to insert before speaking this paragraph.</summary>
    [JsonPropertyName("pauseBeforeSeconds")]
    public double PauseBeforeSeconds { get; set; }

    /// <summary>Seconds of silence to insert after speaking this paragraph.</summary>
    [JsonPropertyName("pauseAfterSeconds")]
    public double PauseAfterSeconds { get; set; }
}
