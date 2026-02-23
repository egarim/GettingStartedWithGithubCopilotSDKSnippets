# Getting Started With GitHub Copilot SDK — Snippets

Companion repo for [GettingStartedWithGithubCopilotSDK](https://github.com/egarim/GettingStartedWithGithubCopilotSDK).

**Purpose:** Empty project shells + [HotkeyTyper](https://github.com/jamesmontemagno/app-hotkeytyper) snippets for type-free demo recording.

## How to Use

1. **Install HotkeyTyper** from [app-hotkeytyper releases](https://github.com/jamesmontemagno/app-hotkeytyper/releases)
2. **Copy** `snippets/settings.json` to `%LocalAppData%\HotkeyTyper\settings.json`
3. **Open this solution** in Visual Studio
4. **Select the demo set** in HotkeyTyper (e.g. "03-Tools")
5. **Record your demo** — use `CTRL+SHIFT+1` through `CTRL+SHIFT+9` to paste code blocks

## Snippet Sets

| Set | Demo | Hotkeys | Topics |
|-----|------|---------|--------|
| 01-Client | Client Lifecycle | 7 | Create, Start, Ping, Status, Auth, Models, Stop |
| 02-Session | Sessions & Multi-turn | 6 | Create, SendAndWait, Events, Resume, SystemMsg, Streaming |
| 03-Tools | Custom Tools | 6 | Define, Register, Multiple, Complex Types, Weather, JsonContext |
| 04-Hooks | Pre/Post Hooks | 4 | Allow, PostUse, Deny, Both |
| 05-Permissions | Permission Handling | 3 | Approve, Deny, Async |
| 06-AskUser | User Input | 2 | Choice handler, Freeform |
| 07-Compaction | Infinite Sessions | 2 | Config, Events |
| 08-Skills | Skills | 3 | SKILL.md, Load, Disable |
| 09-McpAgents | MCP & Agents | 4 | MCP Config, Agent, Agent+Tools, Agent+MCP |
| 10-BlazorChat | Blazor Demo | 3 | NuGet, Options, Registration |

## Workflow

1. Open the empty `Program.cs` for the demo you're recording
2. Switch HotkeyTyper to that demo's set
3. Hit record
4. Use hotkeys to "type" each code block naturally during your explanation
5. Run and show the output

Each snippet is configured with `hasCode: true` and a natural typing speed.
