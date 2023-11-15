using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.HighPerformance;
using Server.Collections;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Engines.Spawners;
using Server.Items;
using Server.Network;
using Server.Utilities;
using MemoryExtensions = System.MemoryExtensions;

namespace Server.Commands
{
    public static class Decorate
    {
        private static Mobile m_Mobile;
        private static int m_Count;

        public static void Initialize()
        {
            CommandSystem.Register("Decorate", AccessLevel.Administrator, Decorate_OnCommand);
        }

        [Usage("Decorate")]
        [Description("Generates world decoration.")]
        private static void Decorate_OnCommand(CommandEventArgs e)
        {
            m_Mobile = e.Mobile;
            m_Count = 0;

            m_Mobile.SendMessage("Generating world decoration, please wait.");

            NetState.FlushAll();

            Generate("Data/Decoration/Britannia", Map.Trammel, Map.Felucca);
            Generate("Data/Decoration/Trammel", Map.Trammel);
            Generate("Data/Decoration/Felucca", Map.Felucca);
            Generate("Data/Decoration/Ilshenar", Map.Ilshenar);
            Generate("Data/Decoration/Malas", Map.Malas);
            Generate("Data/Decoration/Tokuno", Map.Tokuno);

            m_Mobile.SendMessage($"World generating complete. {m_Count} items were generated.");
        }

        public static void Generate(string folder, params Map[] maps)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            var files = Directory.GetFiles(folder, "*.cfg");

            for (var i = 0; i < files.Length; ++i)
            {
                var list = DecorationList.ReadAll(files[i]);

                for (var j = 0; j < list.Count; ++j)
                {
                    m_Count += list[j].Generate(maps);
                }
            }
        }
    }

    public class DecorationList
    {
        private static readonly Type typeofStatic = typeof(Static);
        private static readonly Type typeofLocalizedStatic = typeof(LocalizedStatic);
        private static readonly Type typeofBaseDoor = typeof(BaseDoor);
        private static readonly Type typeofAnkhWest = typeof(AnkhWest);
        private static readonly Type typeofAnkhNorth = typeof(AnkhNorth);
        private static readonly Type typeofBeverage = typeof(BaseBeverage);
        private static readonly Type typeofLocalizedSign = typeof(LocalizedSign);
        private static readonly Type typeofMarkContainer = typeof(MarkContainer);
        private static readonly Type typeofWarningItem = typeof(WarningItem);
        private static readonly Type typeofHintItem = typeof(HintItem);
        private static readonly Type typeofCannon = typeof(Cannon);
        private static readonly Type typeofSerpentPillar = typeof(SerpentPillar);

        private static readonly string[] m_EmptyParams = Array.Empty<string>();
        private List<DecorationEntry> m_Entries;
        private int m_ItemID;
        private string[] m_Params;
        private Type m_Type;

        public Item Construct()
        {
            if (m_Type == null)
            {
                return null;
            }

            Item item;

            try
            {
                if (m_Type == typeofStatic)
                {
                    item = new Static(m_ItemID);
                }
                else if (m_Type == typeofLocalizedStatic)
                {
                    var labelNumber = 0;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("LabelNumber"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                                break;
                            }
                        }
                    }

                    item = new LocalizedStatic(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofLocalizedSign)
                {
                    var labelNumber = 0;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("LabelNumber"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                                break;
                            }
                        }
                    }

                    item = new LocalizedSign(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofAnkhWest || m_Type == typeofAnkhNorth)
                {
                    var bloodied = false;

                    for (var i = 0; !bloodied && i < m_Params.Length; ++i)
                    {
                        bloodied = m_Params[i] == "Bloodied";
                    }

                    if (m_Type == typeofAnkhWest)
                    {
                        item = new AnkhWest(bloodied);
                    }
                    else
                    {
                        item = new AnkhNorth(bloodied);
                    }
                }
                else if (m_Type == typeofMarkContainer)
                {
                    var bone = false;
                    var locked = false;
                    var map = Map.Malas;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i] == "Bone")
                        {
                            bone = true;
                        }
                        else if (m_Params[i] == "Locked")
                        {
                            locked = true;
                        }
                        else if (m_Params[i].StartsWithOrdinal("TargetMap"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                map = Map.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                    }

                    var mc = new MarkContainer(bone, locked);

                    mc.TargetMap = map;
                    mc.Description = "strange location";

                    item = mc;
                }
                else if (m_Type == typeofHintItem)
                {
                    var range = 0;
                    var messageNumber = 0;
                    string messageString = null;
                    var hintNumber = 0;
                    string hintString = null;
                    var resetDelay = TimeSpan.Zero;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("Range"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                range = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("WarningString"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                messageString = m_Params[i][++indexOf..];
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("WarningNumber"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                messageNumber = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("HintString"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                hintString = m_Params[i][++indexOf..];
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("HintNumber"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                hintNumber = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("ResetDelay"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                resetDelay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                    }

                    var hi = new HintItem(m_ItemID, range, messageNumber, hintNumber);

                    hi.WarningMessage = messageString;
                    hi.HintMessage = hintString;
                    hi.ResetDelay = resetDelay;

                    item = hi;
                }
                else if (m_Type == typeofWarningItem)
                {
                    var range = 0;
                    var messageNumber = 0;
                    string messageString = null;
                    var resetDelay = TimeSpan.Zero;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("Range"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                range = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("WarningString"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                messageString = m_Params[i][++indexOf..];
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("WarningNumber"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                messageNumber = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("ResetDelay"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                resetDelay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                    }

                    var wi = new WarningItem(m_ItemID, range, messageNumber);

                    wi.WarningMessage = messageString;
                    wi.ResetDelay = resetDelay;

                    item = wi;
                }
                else if (m_Type == typeofCannon)
                {
                    var direction = CannonDirection.North;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("CannonDirection"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                direction = (CannonDirection)Enum.Parse(
                                    typeof(CannonDirection),
                                    m_Params[i][++indexOf..],
                                    true
                                );
                            }
                        }
                    }

                    item = new Cannon(direction);
                }
                else if (m_Type == typeofSerpentPillar)
                {
                    string word = null;
                    var destination = new Rectangle2D();

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("Word"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                word = m_Params[i][++indexOf..];
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("DestStart"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                destination.Start = Point2D.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                        else if (m_Params[i].StartsWithOrdinal("DestEnd"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                destination.End = Point2D.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                    }

                    item = new SerpentPillar(word, destination);
                }
                else if (m_Type.IsSubclassOf(typeofBeverage))
                {
                    var content = BeverageType.Liquor;
                    var fill = false;

                    for (var i = 0; !fill && i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("Content"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                content = (BeverageType)Enum.Parse(
                                    typeof(BeverageType),
                                    m_Params[i][++indexOf..],
                                    true
                                );
                                fill = true;
                            }
                        }
                    }

                    if (fill)
                    {
                        item = m_Type.CreateInstance<Item>(content);
                    }
                    else
                    {
                        item = m_Type.CreateInstance<Item>();
                    }
                }
                else if (m_Type.IsSubclassOf(typeofBaseDoor) && m_Params.Length > 0)
                {
                    var facing = DoorFacing.WestCW;

                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("Facing"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                facing = (DoorFacing)Enum.Parse(typeof(DoorFacing), m_Params[i][++indexOf..], true);
                                break;
                            }
                        }
                    }

                    item = m_Type.CreateInstance<Item>(facing);
                }
                else
                {
                    item = m_Type.CreateInstance<Item>();
                }
            }
            catch (Exception e)
            {
                throw new TypeInitializationException(m_Type.ToString(), e);
            }

            if (item is BaseAddon addon)
            {
                if (addon is MaabusCoffin coffin)
                {
                    for (var i = 0; i < m_Params.Length; ++i)
                    {
                        if (m_Params[i].StartsWithOrdinal("SpawnLocation"))
                        {
                            var indexOf = m_Params[i].IndexOfOrdinal('=');

                            if (indexOf >= 0)
                            {
                                coffin.SpawnLocation = Point3D.Parse(m_Params[i][++indexOf..]);
                            }
                        }
                    }
                }
                else if (m_ItemID > 0)
                {
                    var comps = addon.Components;

                    for (var i = 0; i < comps.Count; ++i)
                    {
                        var comp = comps[i];

                        if (comp.Offset == Point3D.Zero)
                        {
                            comp.ItemID = m_ItemID;
                        }
                    }
                }
            }
            else if (item is BaseLight light)
            {
                bool unlit = false, unprotected = false;

                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (!unlit && m_Params[i] == "Unlit")
                    {
                        unlit = true;
                    }
                    else if (!unprotected && m_Params[i] == "Unprotected")
                    {
                        unprotected = true;
                    }

                    if (unlit && unprotected)
                    {
                        break;
                    }
                }

                // Make light never run out of fuel.
                light.Duration = TimeSpan.Zero;

                if (!unlit)
                {
                    light.Ignite();
                }

                if (!unprotected)
                {
                    light.Protected = true;
                }

                if (m_ItemID > 0)
                {
                    light.ItemID = m_ItemID;
                }
            }
            else if (item is BaseSpawner sp)
            {
                sp.NextSpawn = TimeSpan.Zero;

                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("Spawn"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.AddEntry(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MinDelay"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.MinDelay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MaxDelay"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.MaxDelay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("NextSpawn"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.NextSpawn = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Count"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.Count = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                            for (var se = 0; se < sp.Entries.Count; se++)
                            {
                                sp.Entries[se].SpawnedMaxCount = sp.Count;
                            }
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Team"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.Team = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("HomeRange"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.HomeRange = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Running"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.Running = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Group"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            sp.Group = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                }
            }
            else if (item is RecallRune rune)
            {
                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("Description"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            rune.Description = m_Params[i][++indexOf..];
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Marked"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            rune.Marked = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("TargetMap"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            rune.TargetMap = Map.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Target"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            rune.Target = Point3D.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                }
            }
            else if (item is SkillTeleporter st)
            {
                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("Skill"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Skill = (SkillName)Enum.Parse(typeof(SkillName), m_Params[i][++indexOf..], true);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("RequiredFixedPoint"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Required = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]) * 0.1;
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Required"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Required = Utility.ToDouble(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MessageString"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Message = m_Params[i][++indexOf..];
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MessageNumber"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Message = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.PointDest = Point3D.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.MapDest = Map.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Creatures = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.SourceEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.DestEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.SoundID = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            st.Delay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                }

                if (m_ItemID > 0)
                {
                    st.ItemID = m_ItemID;
                }
            }
            else if (item is KeywordTeleporter kt)
            {
                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("Substring"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.Substring = m_Params[i][++indexOf..];
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Keyword"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.Keyword = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Range"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.Range = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.PointDest = Point3D.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.MapDest = Map.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.Creatures = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.SourceEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.DestEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.SoundID = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            kt.Delay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                }

                if (m_ItemID > 0)
                {
                    kt.ItemID = m_ItemID;
                }
            }
            else if (item is Teleporter tp)
            {
                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.PointDest = Point3D.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.MapDest = Map.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.Creatures = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.DestEffect = Utility.ToBoolean(m_Params[i][++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.SoundID = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        }
                    }
                    else if (m_Params[i].StartsWithOrdinal("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            tp.Delay = TimeSpan.Parse(m_Params[i][++indexOf..]);
                        }
                    }
                }

                if (m_ItemID > 0)
                {
                    tp.ItemID = m_ItemID;
                }
            }
            else if (item is FillableContainer cont)
            {
                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (m_Params[i].StartsWithOrdinal("ContentType"))
                    {
                        var indexOf = m_Params[i].IndexOfOrdinal('=');

                        if (indexOf >= 0)
                        {
                            cont.ContentType = (FillableContentType)Enum.Parse(
                                typeof(FillableContentType),
                                m_Params[i][++indexOf..],
                                true
                            );
                        }
                    }
                }

                if (m_ItemID > 0)
                {
                    cont.ItemID = m_ItemID;
                }
            }
            else if (m_ItemID > 0)
            {
                item.ItemID = m_ItemID;
            }

            item.Movable = false;

            for (var i = 0; i < m_Params.Length; ++i)
            {
                if (m_Params[i].StartsWithOrdinal("Light"))
                {
                    var indexOf = m_Params[i].IndexOfOrdinal('=');

                    if (indexOf >= 0)
                    {
                        item.Light = (LightType)Enum.Parse(typeof(LightType), m_Params[i][++indexOf..], true);
                    }
                }
                else if (m_Params[i].StartsWithOrdinal("Hue"))
                {
                    var indexOf = m_Params[i].IndexOfOrdinal('=');

                    if (indexOf >= 0)
                    {
                        var hue = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);

                        if (item is DyeTub tub)
                        {
                            tub.DyedHue = hue;
                        }
                        else
                        {
                            item.Hue = hue;
                        }
                    }
                }
                else if (m_Params[i].StartsWithOrdinal("Name"))
                {
                    var indexOf = m_Params[i].IndexOfOrdinal('=');

                    if (indexOf >= 0)
                    {
                        item.Name = m_Params[i][++indexOf..];
                    }
                }
                else if (m_Params[i].StartsWithOrdinal("Amount"))
                {
                    var indexOf = m_Params[i].IndexOfOrdinal('=');

                    if (indexOf >= 0)
                    {
                        // Must supress stackable warnings

                        var wasStackable = item.Stackable;

                        item.Stackable = true;
                        item.Amount = Utility.ToInt32(m_Params[i].AsSpan()[++indexOf..]);
                        item.Stackable = wasStackable;
                    }
                }
            }

            return item;
        }

        private static bool FindItem(int x, int y, int z, Map map, Item srcItem)
        {
            var itemID = srcItem.ItemID;
            var lt = srcItem.Light;
            var srcName = srcItem.ItemData.Name;
            var type = srcItem.GetType();

            var res = false;

            using var queue = PooledRefQueue<Item>.Create();

            foreach (var item in map.GetItemsInRange(new Point3D(x, y, z), 1))
            {
                if (srcItem is BaseDoor)
                {
                    if (!(item is BaseDoor))
                    {
                        continue;
                    }

                    var bd = (BaseDoor)item;
                    Point3D p;
                    int bdItemID;

                    if (bd.Open)
                    {
                        p = new Point3D(bd.X - bd.Offset.X, bd.Y - bd.Offset.Y, bd.Z - bd.Offset.Z);
                        bdItemID = bd.ClosedId;
                    }
                    else
                    {
                        p = bd.Location;
                        bdItemID = bd.ItemID;
                    }

                    if (p.X != x || p.Y != y)
                    {
                        continue;
                    }

                    if (item.Z == z && bdItemID == itemID)
                    {
                        res = true;
                    }
                    else if ((item.Z - z).Abs() < 8)
                    {
                        queue.Enqueue(item);
                    }
                }
                else if (TileData.ItemTable[itemID & TileData.MaxItemValue].LightSource)
                {
                    if (item.Z == z)
                    {
                        if (item.ItemID == itemID)
                        {
                            if (item.Light != lt)
                            {
                                queue.Enqueue(item);
                            }
                            else
                            {
                                res = true;
                            }
                        }
                        else if (item.ItemData.LightSource && item.ItemData.Name == srcName)
                        {
                            queue.Enqueue(item);
                        }
                    }
                }
                else if (srcItem is Teleporter or FillableContainer or BaseBook)
                {
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        if (item.GetType() != type)
                        {
                            queue.Enqueue(item);
                        }
                        else
                        {
                            res = true;
                        }
                    }
                }
                else
                {
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        return true;
                    }
                }
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().Delete();
            }

            return res;
        }

        public int Generate(Map[] maps)
        {
            var count = 0;

            Item item = null;

            for (var i = 0; i < m_Entries.Count; ++i)
            {
                var entry = m_Entries[i];
                var loc = entry.Location;
                var extra = entry.Extra;

                for (var j = 0; j < maps.Length; ++j)
                {
                    try
                    {
                        item ??= Construct();
                    }
                    catch (TypeInitializationException e)
                    {
                        Console.WriteLine(
                            $"{nameof(Generate)}() failed to load type: {e.TypeName}: {e.InnerException?.Message}"
                        );
                        continue;
                    }

                    if (item == null)
                    {
                        continue;
                    }

                    if (FindItem(loc.X, loc.Y, loc.Z, maps[j], item))
                    {
                    }
                    else
                    {
                        item.MoveToWorld(loc, maps[j]);
                        ++count;

                        if (item is BaseDoor door)
                        {
                            var itemType = door.GetType();
                            foreach (var link in maps[j].GetItemsInRange<BaseDoor>(loc, 1))
                            {
                                if (link != item && link.Z == door.Z && link.GetType() == itemType)
                                {
                                    door.Link = link;
                                    link.Link = door;
                                    break;
                                }
                            }
                        }
                        else if (item is MarkContainer markCont)
                        {
                            try
                            {
                                markCont.Target = Point3D.Parse(extra);
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        item = null;
                    }
                }
            }

            item?.Delete();

            return count;
        }

        public static List<DecorationList> ReadAll(string path)
        {
            using var ip = new StreamReader(path);
            var list = new List<DecorationList>();
            DecorationList v;

            while ((v = Read(ip)) != null)
            {
                list.Add(v);
            }

            return list;
        }

        public static DecorationList Read(StreamReader ip)
        {
            string line;

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length > 0 && !line.StartsWithOrdinal("#"))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var list = new DecorationList();

            var indexOf = line.IndexOfOrdinal(' ');

            list.m_Type = AssemblyHandler.FindTypeByName(indexOf > -1 ? line[..indexOf] : line);
            indexOf++;

            if (list.m_Type == null)
            {
                throw new ArgumentException($"Type not found for header: '{line}'");
            }

            var span = line.AsSpan(indexOf, line.Length - indexOf);

            var argsStart = span.IndexOfOrdinal('(');

            if (argsStart > -1)
            {
                var parms = span[(argsStart + 1)..^(line.EndsWithOrdinal(")") ? 1 : 0)];
                if (parms.Length == 0)
                {
                    list.m_Params = Array.Empty<string>();
                }
                else
                {
                    list.m_Params = new string[MemoryExtensions.Count(parms, ';') + 1];

                    indexOf = 0;
                    foreach (var part in parms.Tokenize(';'))
                    {
                        list.m_Params[indexOf++] = part.Trim().ToString();
                    }
                }
            }
            else
            {
                list.m_Params = m_EmptyParams;
            }

            list.m_ItemID = Utility.ToInt32(argsStart > -1 ? span[..argsStart] : span);
            list.m_Entries = new List<DecorationEntry>();

            while ((line = ip.ReadLine()) != null)
            {
                span = line.AsSpan().Trim();

                if (span.Length == 0)
                {
                    break;
                }

                if (span.StartsWithOrdinal("#"))
                {
                    continue;
                }

                list.m_Entries.Add(new DecorationEntry(span));
            }

            return list;
        }
    }

    public class DecorationEntry
    {
        public DecorationEntry(ReadOnlySpan<char> line)
        {
            Pop(out var x, ref line);
            Pop(out var y, ref line);
            Pop(out var z, ref line);

            Location = new Point3D(Utility.ToInt32(x), Utility.ToInt32(y), Utility.ToInt32(z));
            Extra = line.ToString();
        }

        public Point3D Location { get; }

        public string Extra { get; }

        public static void Pop(out ReadOnlySpan<char> v, ref ReadOnlySpan<char> line)
        {
            var space = line.IndexOfOrdinal(' ');

            if (space >= 0)
            {
                v = line[..space++];
                line = line[space..];
            }
            else
            {
                v = line;
                line = default;
            }
        }
    }
}
