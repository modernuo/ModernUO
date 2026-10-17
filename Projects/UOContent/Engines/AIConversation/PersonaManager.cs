using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Logging;
using Server.Mobiles;
using Server.Prompts;
using Server.Targeting;

namespace Server.Engines.AIConversation;

/// <summary>
/// Resolves the identity used to role-play an NPC. Personas come from two
/// sources, in priority order:
///   1. Per-NPC personas set in game by staff ([SetPersona) — keyed to the
///      specific NPC and persisted through world saves.
///   2. Hand-authored templates in Data/ai-personas.json — matched by exact
///      NPC name, then by class name walking up the inheritance chain.
/// Only NPCs that resolve to a persona hold AI conversations.
/// </summary>
public class PersonaManager : GenericPersistence
{
    private const string _templatePath = "Data/ai-personas.json";

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PersonaManager));

    private static readonly Dictionary<Mobile, string> _customPersonas = new();
    private static readonly Dictionary<string, string> _nameTemplates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _typeTemplates = new(StringComparer.OrdinalIgnoreCase);

    public static PersonaManager Instance { get; private set; }

    public static int CustomCount => _customPersonas.Count;
    public static int TemplateCount => _nameTemplates.Count + _typeTemplates.Count;

    public static void Configure()
    {
        Instance = new PersonaManager();

        CommandSystem.Register("SetPersona", AccessLevel.GameMaster, SetPersona_OnCommand);
        CommandSystem.Register("RemovePersona", AccessLevel.GameMaster, RemovePersona_OnCommand);
        CommandSystem.Register("PersonaInfo", AccessLevel.GameMaster, PersonaInfo_OnCommand);

        LoadTemplates();
    }

    public PersonaManager() : base("AIPersonas", 100)
    {
    }

    /// <summary>
    /// Returns the persona text for a mobile, or null if it has none and
    /// therefore should not hold AI conversations.
    /// </summary>
    public static string GetPersona(Mobile npc)
    {
        if (npc == null)
        {
            return null;
        }

        if (_customPersonas.TryGetValue(npc, out var persona))
        {
            return persona;
        }

        if (!string.IsNullOrEmpty(npc.Name) && _nameTemplates.TryGetValue(npc.Name, out persona))
        {
            return persona;
        }

        var type = npc.GetType();

        while (type != null && type != typeof(object))
        {
            if (_typeTemplates.TryGetValue(type.Name, out persona))
            {
                return persona;
            }

            type = type.BaseType;
        }

        return null;
    }

    public static bool HasPersona(Mobile npc) => GetPersona(npc) != null;

    public static bool HasCustomPersona(Mobile npc) => _customPersonas.ContainsKey(npc);

    public static void SetCustomPersona(Mobile npc, string persona) => _customPersonas[npc] = persona;

    public static bool RemoveCustomPersona(Mobile npc) => _customPersonas.Remove(npc);

    public static void LoadTemplates()
    {
        _nameTemplates.Clear();
        _typeTemplates.Clear();

        var path = Path.Combine(Core.BaseDirectory, _templatePath);
        var templates = JsonConfig.Deserialize<PersonaTemplateEntry[]>(path);

        if (templates == null)
        {
            return;
        }

        foreach (var entry in templates)
        {
            var persona = entry.Persona?.Trim();

            if (string.IsNullOrEmpty(persona))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(entry.Name))
            {
                _nameTemplates[entry.Name] = persona;
            }
            else if (!string.IsNullOrEmpty(entry.Type))
            {
                _typeTemplates[entry.Type] = persona;
            }
        }

        logger.Information("Loaded {Count} persona template(s) from {Path}", TemplateCount, _templatePath);
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var count = 0;

        foreach (var (npc, _) in _customPersonas)
        {
            if (npc?.Deleted == false)
            {
                count++;
            }
        }

        writer.WriteEncodedInt(count);

        foreach (var (npc, persona) in _customPersonas)
        {
            if (npc?.Deleted == false)
            {
                writer.Write(npc);
                writer.Write(persona);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; ++i)
        {
            var npc = reader.ReadEntity<Mobile>();
            var persona = reader.ReadString();

            if (npc?.Deleted == false && !string.IsNullOrEmpty(persona))
            {
                _customPersonas[npc] = persona;
            }
        }
    }

    private static bool ValidateNpc(Mobile from, object targeted, out BaseCreature npc)
    {
        npc = targeted as BaseCreature;

        if (npc == null || npc.Player)
        {
            from.SendMessage("That is not an NPC.");
            return false;
        }

        if (npc.Controlled || npc.Summoned)
        {
            from.SendMessage("Pets and summons cannot be given personas.");
            return false;
        }

        return true;
    }

    [Usage("SetPersona")]
    [Description("Targets an NPC, then prompts for a persona description enabling AI conversation for that NPC.")]
    private static void SetPersona_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target the NPC to give a persona.");
        e.Mobile.Target = new SetPersonaTarget();
    }

    [Usage("RemovePersona")]
    [Description("Targets an NPC and removes its custom AI persona.")]
    private static void RemovePersona_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target the NPC to remove its persona.");
        e.Mobile.Target = new RemovePersonaTarget();
    }

    [Usage("PersonaInfo")]
    [Description("Targets an NPC and displays the AI persona that applies to it, if any.")]
    private static void PersonaInfo_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Target the NPC to inspect.");
        e.Mobile.Target = new PersonaInfoTarget();
    }

    private class SetPersonaTarget : Target
    {
        public SetPersonaTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!ValidateNpc(from, targeted, out var npc))
            {
                return;
            }

            from.SendMessage($"Enter the persona for {npc.Name} (who they are, how they speak, what they know):");
            from.Prompt = new SetPersonaPrompt(npc);
        }
    }

    private class SetPersonaPrompt : Prompt
    {
        private readonly BaseCreature _npc;

        public SetPersonaPrompt(BaseCreature npc) => _npc = npc;

        public override void OnResponse(Mobile from, string text)
        {
            if (_npc?.Deleted != false)
            {
                from.SendMessage("That NPC no longer exists.");
                return;
            }

            text = text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                from.SendMessage("Persona unchanged.");
                return;
            }

            SetCustomPersona(_npc, text);

            if (AIConversationSystem.Running)
            {
                from.SendMessage($"Persona set for {_npc.Name}. Players may now converse with them.");
            }
            else
            {
                from.SendMessage($"Persona set for {_npc.Name}. Players may converse with them once AI chat is enabled.");
            }
        }
    }

    private class RemovePersonaTarget : Target
    {
        public RemovePersonaTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!ValidateNpc(from, targeted, out var npc))
            {
                return;
            }

            if (RemoveCustomPersona(npc))
            {
                from.SendMessage($"Custom persona removed from {npc.Name}.");
            }
            else
            {
                from.SendMessage($"{npc.Name} has no custom persona.");
            }
        }
    }

    private class PersonaInfoTarget : Target
    {
        public PersonaInfoTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Mobile npc)
            {
                from.SendMessage("That is not a mobile.");
                return;
            }

            var persona = GetPersona(npc);

            if (persona == null)
            {
                from.SendMessage($"{npc.Name} has no AI persona.");
                return;
            }

            if (HasCustomPersona(npc))
            {
                from.SendMessage($"Persona for {npc.Name} (custom):");
            }
            else
            {
                from.SendMessage($"Persona for {npc.Name} (template):");
            }

            if (persona.Length > 200)
            {
                from.SendMessage($"{persona[..200]}...");
            }
            else
            {
                from.SendMessage(persona);
            }
        }
    }

    public record PersonaTemplateEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("type")]
        public string Type { get; init; }

        [JsonPropertyName("persona")]
        public string Persona { get; init; }
    }
}
