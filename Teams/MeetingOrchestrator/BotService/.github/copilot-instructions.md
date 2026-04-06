# Meeting Orchestrator Bot – Copilot Instructions

## Project Overview

This is a **Microsoft Teams Meeting Bot** (code-named *MeetingOrchestratorBot*) designed to **simulate lifelike meeting participants**. The bot joins a Teams call and follows a pre-configured **script** — speaking lines aloud via text-to-speech just like a real person would. Scripts can be configured, swapped, and updated at any time through the bot's API.

The primary goal is to **generate realistic meeting activity** so that Teams features such as **Copilot** and **Facilitator** can be demonstrated and tested in real time with genuine audio, dominant-speaker changes, and natural conversational flow — without needing a room full of live participants.

Under the hood the bot is built on the **Microsoft Graph Communications** platform and the **Teams Real-Time Media** SDK. It tracks dominant speakers in the call and streams synthesised audio into the meeting via Azure Speech.

## Solution Structure

| Project | Purpose |
|---|---|
| **Bot.Console** | Console entry-point. Inherits `AppHost`, boots the service, and starts the HTTP server. |
| **Bot.Services** | Core library – bot lifecycle, call handling, media streaming, speech synthesis, HTTP controllers, and Azure configuration. |
| **Bot.Model** | Shared models, constants, and contracts (e.g. `JoinCallBody`, `SpeechScript`, `Meeting`). |
| **Bot.Tests** | Tests using **xUnit** and `Xunit.SkippableFact`. |

## Target Framework & Language

- **.NET 10** (`net10.0` / `net10.0-windows`) – x64 only.
- **C# latest** (`<LangVersion>latest</LangVersion>`).
- Use modern C# features: file-scoped namespaces, primary constructors, pattern matching, nullable reference types where enabled.

## Key Dependencies

- **Microsoft.Graph.Communications.Calls / .Calls.Media** – Stateful call management and real-time media.
- **Microsoft.Skype.Bots.Media** – Low-level audio/video socket handling.
- **Microsoft.Graph** (v5) – Microsoft Graph SDK.
- **Microsoft.CognitiveServices.Speech** – Azure Speech SDK for TTS.
- **Azure.Identity** – `ClientSecretCredential` for Azure AD / Speech RBAC auth.
- **NAudio** – Audio format conversion.
- **ASP.NET Core** (via `FrameworkReference`) – HTTP controllers for call signaling.
- **Newtonsoft.Json** – Serialization (required by Graph Communications SDK).

## Architecture & Patterns

- **`BotService`** – Singleton managing the `ICommunicationsClient` lifecycle and a `ConcurrentDictionary<string, CallHandler>` of active calls.
- **`CallHandler`** – Per-call handler; tracks dominant speaker changes, manages TTS playback via `CallAudioHandler`.
- **`BotMediaStream` / `MediaStream`** – Handles raw audio/video buffers from the media platform.
- **Factory pattern** – `IMediaSessionFactory` / `ICallHandlerFactory` create media sessions and call handlers, aiding testability.
- **`AppHost`** – Bootstraps DI (`IServiceCollection`), Kestrel HTTP server, Graph logger, and Application Insights.
- **Controllers** – `JoinCallController`, `PlatformCallController`, `ScriptController`, `DemoController`, `ChangeScreenSharingRoleController`.

## Configuration

- Settings are loaded into `AzureSettings` (implements `IAzureSettings`) from environment variables / `.env` file (via `DotNetEnv`).
- Key settings: `AadAppId`, `AadAppSecret`, `AadTenantId`, `ServiceDnsName`, `BotName`, Speech resource config.

## Coding Conventions

- Prefer constructor injection via DI.
- Use `async`/`await` with `.ConfigureAwait(false)` in library code.
- XML doc comments on public APIs.
- Private fields use `_camelCase` prefix.
- Keep `Bot.Model` free of service-layer dependencies.

## Testing

- Framework: **xUnit** with `Microsoft.NET.Test.Sdk`.
- Use `Xunit.SkippableFact` (`Skip.IfNot(...)`) for integration tests that need live Azure credentials (user secrets).
- Test project references `Bot.Services`.
