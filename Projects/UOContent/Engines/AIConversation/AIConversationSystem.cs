using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server.Collections;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.Engines.AIConversation;

/// <summary>
/// Lets players hold natural-language conversations with NPCs, backed by the
/// Anthropic Messages API. NPCs must have a persona (see PersonaManager) to
/// participate.
///
/// A player speaks near a persona NPC -> the system builds the conversation
/// history and awaits an API request. The HTTP I/O runs off-thread and the
/// continuation is marshaled back to the game loop by the EventLoopContext,
/// so the game thread never blocks on the network. Speech is observed
/// passively — existing keyword systems (bank, guards, vendors, pets) are
/// unaffected.
/// </summary>
public static class AIConversationSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AIConversationSystem));

    private const string _fallbackLine = "Hmm... forgive me, my thoughts wandered. What were we speaking of?";

    // Configuration (see README.md in this directory for details)
    private static bool _enabled;
    private static string _apiUrl;
    private static string _apiKeyEnvVar;
    private static string _model;
    private static int _maxTokens;
    private static TimeSpan _requestTimeout;
    private static int _engageRange;
    private static TimeSpan _sessionIdleTimeout;
    private static int _maxHistoryTurns;
    private static int _maxPlayerMessageLength;
    private static int _maxResponseLength;
    private static TimeSpan _playerCooldown;
    private static int _playerRequestsPerMinute;
    private static int _maxConcurrentRequests;
    private static string _extraInstructions;

    private static string _apiKey;
    private static AnthropicClient _client;
    private static bool _runtimeEnabled;

    private static readonly Dictionary<Mobile, ConversationSession> _sessions = new();
    private static TimerExecutionToken _sweepTimerToken;

    private static int _activeRequests;
    private static long _totalRequests;
    private static long _totalFailures;
    private static long _totalInputTokens;
    private static long _totalOutputTokens;

    public static bool Running => _runtimeEnabled;

    public static void Configure()
    {
        LoadSettings();

        CommandSystem.Register("AIChat", AccessLevel.Administrator, AIChat_OnCommand);

        EventSink.Speech += OnSpeech;
    }

    public static void Initialize()
    {
        if (!_enabled)
        {
            return;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.Warning(
                "Enabled in configuration but the {EnvVar} environment variable is not set. AI conversations are disabled.",
                _apiKeyEnvVar
            );
            return;
        }

        EnableRuntime();

        logger.Information("NPC conversations enabled (model: {Model})", _model);
    }

    private static void LoadSettings()
    {
        _enabled = ServerConfiguration.GetOrUpdateSetting("aiConversation.enabled", false);
        _apiUrl = ServerConfiguration.GetOrUpdateSetting("aiConversation.apiUrl", "https://api.anthropic.com/v1/messages");
        _apiKeyEnvVar = ServerConfiguration.GetOrUpdateSetting("aiConversation.apiKeyEnvVar", "ANTHROPIC_API_KEY");
        _model = ServerConfiguration.GetOrUpdateSetting("aiConversation.model", "claude-haiku-4-5");
        _maxTokens = ServerConfiguration.GetOrUpdateSetting("aiConversation.maxTokens", 200);
        _requestTimeout = ServerConfiguration.GetOrUpdateSetting("aiConversation.requestTimeout", TimeSpan.FromSeconds(20));
        _engageRange = ServerConfiguration.GetOrUpdateSetting("aiConversation.engageRange", 6);
        _sessionIdleTimeout = ServerConfiguration.GetOrUpdateSetting("aiConversation.sessionIdleTimeout", TimeSpan.FromMinutes(2));
        _maxHistoryTurns = ServerConfiguration.GetOrUpdateSetting("aiConversation.maxHistoryMessages", 20);
        _maxPlayerMessageLength = ServerConfiguration.GetOrUpdateSetting("aiConversation.maxPlayerMessageLength", 240);
        _maxResponseLength = ServerConfiguration.GetOrUpdateSetting("aiConversation.maxResponseLength", 600);
        _playerCooldown = ServerConfiguration.GetOrUpdateSetting("aiConversation.playerCooldown", TimeSpan.FromSeconds(2));
        _playerRequestsPerMinute = ServerConfiguration.GetOrUpdateSetting("aiConversation.playerRequestsPerMinute", 8);
        _maxConcurrentRequests = ServerConfiguration.GetOrUpdateSetting("aiConversation.maxConcurrentRequests", 4);
        _extraInstructions = ServerConfiguration.GetOrUpdateSetting("aiConversation.extraInstructions", "");

        _apiKey = string.IsNullOrEmpty(_apiKeyEnvVar) ? null : Environment.GetEnvironmentVariable(_apiKeyEnvVar);
    }

    private static void EnableRuntime()
    {
        _client = new AnthropicClient(_apiUrl, _apiKey, _requestTimeout);
        _runtimeEnabled = true;

        _sweepTimerToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), SweepSessions, out _sweepTimerToken);
    }

    private static void DisableRuntime()
    {
        _runtimeEnabled = false;
        _sweepTimerToken.Cancel();
        EndAllSessions();
    }

    private static void EndSession(ConversationSession session)
    {
        session.Ended = true;

        if (_sessions.TryGetValue(session.Player, out var current) && current == session)
        {
            _sessions.Remove(session.Player);
        }
    }

    private static int EndAllSessions()
    {
        var count = _sessions.Count;

        foreach (var session in _sessions.Values)
        {
            session.Ended = true;
        }

        _sessions.Clear();
        return count;
    }

    private static void SweepSessions()
    {
        if (_sessions.Count == 0)
        {
            return;
        }

        using var expired = PooledRefList<ConversationSession>.Create();
        var now = Core.Now;

        foreach (var session in _sessions.Values)
        {
            if (session.Busy)
            {
                continue; // resolved when the in-flight reply lands
            }

            var invalid = session.Npc.Deleted || session.Player.Deleted || session.Player.NetState == null ||
                          !IsInConversationRange(session.Player, session.Npc);

            if (invalid || now - session.LastActivity > _sessionIdleTimeout)
            {
                expired.Add(session);
            }
        }

        for (var i = 0; i < expired.Count; i++)
        {
            EndSession(expired[i]);
        }
    }

    private static bool IsInConversationRange(Mobile player, BaseCreature npc) =>
        npc.Map == player.Map && player.InRange(npc.Location, _engageRange + 4);

    private static void OnSpeech(SpeechEventArgs e)
    {
        if (!_runtimeEnabled || e.Handled || e.Blocked || e.Type != MessageType.Regular)
        {
            return;
        }

        var from = e.Mobile;

        if (from?.Player != true || !from.Alive || from.NetState == null)
        {
            return;
        }

        var said = e.Speech?.Trim();

        if (string.IsNullOrEmpty(said) || said.StartsWithOrdinal(CommandSystem.Prefix))
        {
            return;
        }

        _sessions.TryGetValue(from, out var session);

        // Addressing another persona NPC by name switches the conversation.
        var addressed = FindAddressedNpc(from, said);

        if (addressed != null && addressed != session?.Npc)
        {
            if (session != null)
            {
                EndSession(session);
            }

            session = new ConversationSession(from, addressed, _maxHistoryTurns);
            _sessions[from] = session;

            SendToNpc(session, said);
            return;
        }

        if (session != null)
        {
            if (session.Npc.Deleted || !IsInConversationRange(from, session.Npc))
            {
                EndSession(session);
                return;
            }

            if (ConversationText.IsFarewell(said))
            {
                session.Npc.Direction = session.Npc.GetDirectionTo(from);
                session.Npc.Say($"Farewell, {from.Name}.");
                EndSession(session);
                return;
            }

            SendToNpc(session, said);
            return;
        }

        // No session and no NPC addressed by name: a plain greeting engages
        // the nearest persona NPC.
        if (ConversationText.IsGreeting(said))
        {
            var nearest = FindNearestPersonaNpc(from);

            if (nearest != null)
            {
                session = new ConversationSession(from, nearest, _maxHistoryTurns);
                _sessions[from] = session;

                SendToNpc(session, said);
            }
        }
    }

    private static BaseCreature FindAddressedNpc(Mobile from, string said)
    {
        BaseCreature match = null;
        var bestDistance = double.MaxValue;

        foreach (var npc in from.GetMobilesInRange<BaseCreature>(_engageRange))
        {
            if (!IsEligibleNpc(from, npc) || !ConversationText.MentionsName(said, npc.Name))
            {
                continue;
            }

            var distance = from.GetDistanceToSqrt(npc);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                match = npc;
            }
        }

        return match;
    }

    private static BaseCreature FindNearestPersonaNpc(Mobile from)
    {
        BaseCreature match = null;
        var bestDistance = double.MaxValue;

        foreach (var npc in from.GetMobilesInRange<BaseCreature>(_engageRange))
        {
            if (!IsEligibleNpc(from, npc))
            {
                continue;
            }

            var distance = from.GetDistanceToSqrt(npc);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                match = npc;
            }
        }

        return match;
    }

    private static bool IsEligibleNpc(Mobile from, BaseCreature npc) =>
        !npc.Deleted && npc.Alive && !npc.Player && !npc.Controlled && !npc.Summoned &&
        from.CanSee(npc) && PersonaManager.HasPersona(npc);

    private static void SendToNpc(ConversationSession session, string said)
    {
        if (session.Busy)
        {
            return; // still waiting on the previous reply
        }

        var now = Core.Now;

        if (now - session.LastRequest < _playerCooldown)
        {
            return;
        }

        var recent = session.RecentRequests;

        while (recent.Count > 0 && now - recent.Peek() > TimeSpan.FromMinutes(1))
        {
            recent.Dequeue();
        }

        if (recent.Count >= _playerRequestsPerMinute)
        {
            session.Player.SendMessage($"{session.Npc.Name} seems overwhelmed; give them a moment.");
            return;
        }

        if (_activeRequests >= _maxConcurrentRequests)
        {
            session.Player.SendMessage($"{session.Npc.Name} seems distracted at the moment.");
            return;
        }

        if (said.Length > _maxPlayerMessageLength)
        {
            said = said[.._maxPlayerMessageLength];
        }

        session.History.Add(ChatRole.User, said);

        session.Busy = true;
        session.LastRequest = now;
        session.LastActivity = now;
        recent.Enqueue(now);

        session.Npc.Direction = session.Npc.GetDirectionTo(session.Player);

        var request = new AnthropicRequest
        {
            Model = _model,
            MaxTokens = _maxTokens,
            SystemPrompt = BuildSystemPrompt(session.Npc, session.Player),
            Messages = session.History.ToArray()
        };

        _activeRequests++;
        _totalRequests++;

        _ = ProcessRequestAsync(session, request);
    }

    private static async Task ProcessRequestAsync(ConversationSession session, AnthropicRequest request)
    {
        AnthropicResult result;

        try
        {
            // CompleteAsync runs its I/O on background threads; this
            // continuation resumes on the game thread via Core.LoopContext.
            result = await _client.CompleteAsync(request);
        }
        catch (Exception ex)
        {
            result = new AnthropicResult { Success = false, Error = ex.Message };
        }

        try
        {
            _activeRequests--;
            OnReply(session, result);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unhandled error while delivering an NPC reply");
        }
    }

    private static void OnReply(ConversationSession session, AnthropicResult result)
    {
        session.Busy = false;

        // The session may have ended (farewell, idle, [AIChat end), the NPC
        // may be gone, or the player may have disconnected: drop the reply.
        if (session.Ended || session.Npc.Deleted || session.Player.Deleted || session.Player.NetState == null)
        {
            return;
        }

        if (!result.Success)
        {
            _totalFailures++;

            logger.Warning("Request failed for {Npc}: {Error}", session.Npc.Name, result.Error);

            session.Npc.Say(_fallbackLine);
            return;
        }

        _totalInputTokens += result.InputTokens;
        _totalOutputTokens += result.OutputTokens;

        var text = ConversationText.Sanitize(result.Text, _maxResponseLength);

        if (text.Length == 0)
        {
            return;
        }

        session.History.Add(ChatRole.Assistant, text);
        session.LastActivity = Core.Now;

        DeliverSpeech(session.Npc, session.Player, text);
    }

    private static void DeliverSpeech(BaseCreature npc, Mobile player, string text)
    {
        var chunks = ConversationText.SplitIntoChunks(text, 120);

        npc.Direction = npc.GetDirectionTo(player);
        npc.Say(chunks[0]);

        for (var i = 1; i < chunks.Count; ++i)
        {
            Timer.DelayCall(TimeSpan.FromMilliseconds(900 * i), SayChunk, npc, chunks[i]);
        }
    }

    private static void SayChunk(BaseCreature npc, string chunk)
    {
        if (!npc.Deleted && npc.Map != null && npc.Map != Map.Internal)
        {
            npc.Say(chunk);
        }
    }

    private static string BuildSystemPrompt(BaseCreature npc, Mobile player)
    {
        using var sb = ValueStringBuilder.Create(1024);

        sb.Append("You are role-playing a character in the medieval fantasy world of Ultima Online (Britannia).\n\n");

        sb.Append($"Your character: {npc.Name ?? "an NPC"}");

        if (!string.IsNullOrEmpty(npc.Title))
        {
            sb.Append(' ');
            sb.Append(npc.Title);
        }

        sb.Append('\n');

        var persona = PersonaManager.GetPersona(npc);

        if (!string.IsNullOrEmpty(persona))
        {
            sb.Append($"Identity: {persona}\n");
        }

        var regionName = npc.Region?.Name;

        if (!string.IsNullOrEmpty(regionName))
        {
            sb.Append($"Current location: {regionName}");

            if (npc.Map != null)
            {
                sb.Append($", on the {npc.Map.Name} facet");
            }

            sb.Append('\n');
        }

        sb.Append($"You are speaking out loud, in person, with {player.Name ?? "a traveler"}");

        if (!string.IsNullOrEmpty(player.Title))
        {
            sb.Append(' ');
            sb.Append(player.Title);
        }

        sb.Append(", an adventurer.\n\n");

        sb.Append("Rules:\n");
        sb.Append("- Always stay in character. Never mention being an AI, a game, or anything outside Britannia.\n");
        sb.Append("- Keep replies short and conversational: one to three sentences of spoken dialogue.\n");
        sb.Append("- Reply with speech only. No stage directions, no asterisks, no quotation marks around your words.\n");
        sb.Append("- Use a medieval fantasy tone. No modern concepts, slang, or technology.\n");
        sb.Append("- You cannot give items, gold, quests, or services through this conversation, and you cannot change the world. You can only talk.\n");
        sb.Append("- Use plain text only: no markdown, no lists, no emoji.\n");

        if (!string.IsNullOrEmpty(_extraInstructions))
        {
            sb.Append(_extraInstructions);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    [Usage("AIChat <status|on|off|reload|end>")]
    [Description("Controls the AI NPC conversation system.")]
    private static void AIChat_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var arg = e.Length > 0 ? e.GetString(0) : "status";

        switch (arg.ToLowerInvariant())
        {
            case "on":
                {
                    if (string.IsNullOrEmpty(_apiKey))
                    {
                        from.SendMessage($"Cannot enable: the {_apiKeyEnvVar} environment variable is not set.");
                    }
                    else
                    {
                        EnableRuntime();
                        from.SendMessage("AI conversations enabled.");
                    }

                    break;
                }
            case "off":
                {
                    DisableRuntime();
                    from.SendMessage("AI conversations disabled.");
                    break;
                }
            case "reload":
                {
                    PersonaManager.LoadTemplates();
                    LoadSettings();

                    if (_runtimeEnabled)
                    {
                        EnableRuntime(); // rebuild the client with fresh settings
                    }

                    from.SendMessage(
                        $"Persona templates and settings reloaded. ({PersonaManager.TemplateCount} templates, {PersonaManager.CustomCount} custom personas)"
                    );
                    break;
                }
            case "end":
                {
                    var count = EndAllSessions();
                    from.SendMessage($"Ended {count} conversation(s).");
                    break;
                }
            default:
                {
                    if (_runtimeEnabled)
                    {
                        from.SendMessage($"AIChat status: running (model: {_model})");
                    }
                    else
                    {
                        from.SendMessage($"AIChat status: stopped (model: {_model})");
                    }

                    from.SendMessage($"Sessions: {_sessions.Count} active, {_activeRequests} request(s) in flight.");
                    from.SendMessage($"Personas: {PersonaManager.TemplateCount} templates, {PersonaManager.CustomCount} custom.");
                    from.SendMessage(
                        $"Usage: {_totalRequests} requests ({_totalFailures} failed), {_totalInputTokens} input / {_totalOutputTokens} output tokens."
                    );
                    break;
                }
        }
    }
}
