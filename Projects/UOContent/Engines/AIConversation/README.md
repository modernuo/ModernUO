# AI NPC Conversations

Gives NPCs unique identities and lets players talk to them in natural
language, backed by the Anthropic Messages API (default model:
`claude-haiku-4-5` — fast and inexpensive, well suited to short in-character
dialogue).

## Setup

1. Get an API key from https://platform.claude.com/ and export it on the
   server host:

   ```sh
   export ANTHROPIC_API_KEY=sk-ant-...
   ```

   The key is only ever read from the environment (variable name configurable
   via `aiConversation.apiKeyEnvVar`) — never put it in a config file.

2. In `Configuration/modernuo.json`, set `"aiConversation.enabled": "true"`.
   The system is **off by default**. All settings appear in the file with
   their defaults after the first boot:

   | Setting | Default | Description |
   |---|---|---|
   | `aiConversation.enabled` | `false` | Master switch |
   | `aiConversation.model` | `claude-haiku-4-5` | Anthropic model id |
   | `aiConversation.maxTokens` | `200` | Max tokens per reply |
   | `aiConversation.requestTimeout` | `00:00:20` | HTTP timeout per attempt |
   | `aiConversation.engageRange` | `6` | Tiles within which speech engages an NPC |
   | `aiConversation.sessionIdleTimeout` | `00:02:00` | Idle time before a conversation ends |
   | `aiConversation.maxHistoryMessages` | `20` | Bounded per-session history (oldest dropped) |
   | `aiConversation.maxPlayerMessageLength` | `240` | Player text truncated beyond this |
   | `aiConversation.maxResponseLength` | `600` | NPC reply truncated at a sentence boundary |
   | `aiConversation.playerCooldown` | `00:00:02` | Minimum delay between requests per player |
   | `aiConversation.playerRequestsPerMinute` | `8` | Per-player per-minute request cap |
   | `aiConversation.maxConcurrentRequests` | `4` | Server-wide in-flight request cap |
   | `aiConversation.extraInstructions` | *(empty)* | Extra text appended to the system prompt |
   | `aiConversation.apiKeyEnvVar` | `ANTHROPIC_API_KEY` | Environment variable holding the key |
   | `aiConversation.apiUrl` | `https://api.anthropic.com/v1/messages` | API endpoint |

   If the system is enabled but no key is present, a warning is logged at
   startup and the system stays disabled.

3. Give NPCs personas. Only NPCs with a persona will converse:
   - **Templates** — edit `Data/ai-personas.json` to match NPCs by class
     (`"type": "Banker"`, applies to subclasses too) or exact name
     (`"name": "Sage Elric"`). Ships with Banker, AnimalTrainer and
     TavernKeeper examples.
   - **In game** — a GameMaster uses `[SetPersona`, targets an NPC, and types
     a description. Per-NPC personas persist through world saves (module
     save `AIPersonas`) and take priority over templates.

4. Restart the server (or use `[AIChat reload` after editing the JSON).

## How players talk to NPCs

- Say **hello** (hi/hail/greetings/well met...) near a persona NPC, or say
  its **name**, to start a conversation.
- Keep talking normally — everything said nearby continues the conversation.
- Say **farewell** (bye/goodbye...), walk away, or go quiet for two minutes
  to end it. Farewells are answered with a canned line — no API call.
- Speaking another persona NPC's name switches the conversation to them.

Existing keyword behaviors (bank, guards, vendor buy, escort destinations,
pet commands) are untouched — the system observes `EventSink.Speech`
passively and never sets `Handled` or `Blocked`. Avoid giving personas to
NPCs whose keywords overlap with normal chat, or they may answer twice.

## Commands

| Command | Access | Description |
|---|---|---|
| `[SetPersona` | GameMaster | Target an NPC, then enter its persona text |
| `[RemovePersona` | GameMaster | Remove a custom persona |
| `[PersonaInfo` | GameMaster | Show the persona that applies to an NPC |
| `[AIChat status` | Administrator | Show state, sessions, in-flight requests and token usage |
| `[AIChat on/off` | Administrator | Toggle at runtime |
| `[AIChat reload` | Administrator | Reload persona templates and settings |
| `[AIChat end` | Administrator | End all active conversations |

## Design notes

- **Threading** — ModernUO game logic is single-threaded, and the game loop
  installs an `EventLoopContext` as the thread's `SynchronizationContext`.
  Requests are dispatched with `async`/`await`: the HTTP I/O runs on the
  thread pool, and the continuation resumes on the game thread before any
  game object is touched. The game loop never blocks on the network.
  (`AnthropicClient` uses `ConfigureAwait(false)` internally because it
  never touches game state; the top-level await in `AIConversationSystem`
  deliberately does not, so it marshals back.)
- **Session safety** — replies are dropped if the session ended, the NPC was
  deleted, or the player disconnected while the request was in flight.
  Speech from a player whose request is still in flight is ignored.
- **Cost control** — per-player cooldown and per-minute caps, a server-wide
  concurrent request cap, bounded history, bounded input/output lengths, and
  a cheap model by default. `[AIChat status` reports cumulative token usage
  (Haiku 4.5: $1 per million input tokens, $5 per million output tokens).
- **Failures** — one retry after ~1.5s on HTTP 429/5xx/timeout; other errors
  are final. On failure the error is logged and the NPC speaks a canned
  in-character fallback line.
- **Safety** — the system prompt instructs the model that the NPC cannot give
  items, gold or quests and can only talk; replies are sanitized (control
  characters stripped, whitespace collapsed, wrapping quotes removed) and
  length-limited before being spoken, then split into ~120-character chunks
  at sentence boundaries, spoken ~900 ms apart.
- **Persistence** — per-NPC personas are stored by a `GenericPersistence`
  module (`AIPersonas`), written as part of the normal world save. Deleted
  NPCs are skipped on save and null-guarded on load.
