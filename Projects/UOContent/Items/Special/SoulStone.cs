using System;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(4)]
public partial class SoulStone : Item, ISecurable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _lastUserName;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _account;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SkillName _skill;

    [Constructible]
    public SoulStone(string account = null, int inactiveItemID = 0x2A93, int activeItemID = 0x2A94) : base(
        inactiveItemID
    )
    {
        Light = LightType.Circle300;
        LootType = LootType.Blessed;

        _inactiveItemID = inactiveItemID;
        _activeItemID = activeItemID;

        _account = account;
    }

    public override int LabelNumber => 1030899; // soulstone

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int ActiveItemID
    {
        get => _activeItemID;
        set
        {
            _activeItemID = value;

            if (!IsEmpty)
            {
                ItemID = _activeItemID;
            }

            this.MarkDirty();
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public virtual int InactiveItemID
    {
        get => _inactiveItemID;
        set
        {
            _inactiveItemID = value;

            if (IsEmpty)
            {
                ItemID = _inactiveItemID;
            }

            this.MarkDirty();
        }
    }

    [SerializableProperty(6)]
    [CommandProperty(AccessLevel.GameMaster)]
    public double SkillValue
    {
        get => _skillValue;
        set
        {
            _skillValue = value;

            ItemID = IsEmpty ? _inactiveItemID : _activeItemID;

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsEmpty => _skillValue <= 0.0;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!IsEmpty)
        {
            // Skill stored: ~1_skillname~ ~2_skillamount~
            list.Add(1070721, $"{AosSkillBonuses.GetLabel(Skill):#}\t{SkillValue:F1}");
        }

        if (LastUserName != null)
        {
            list.Add(1041602, LastUserName); // Owner: ~1_val~
        }
        else
        {
            list.AddLocalized(1041602, 1074235); // Owner: ~1_val~
        }
    }

    private static bool CheckCombat(Mobile m, TimeSpan time)
    {
        for (var i = 0; i < m.Aggressed.Count; ++i)
        {
            var info = m.Aggressed[i];

            if (Core.Now - info.LastCombatTime < time)
            {
                return true;
            }
        }

        return false;
    }

    protected virtual bool CheckUse(Mobile from)
    {
        if (Deleted || !IsAccessibleTo(from))
        {
            return false;
        }

        if (from.Map != Map || !from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return false;
        }

        if (Account != null && (from.Account is not Accounting.Account || from.Account.Username != Account))
        {
            // This is an Account Bound Soulstone, and your character is not bound to it.  You cannot use this Soulstone.
            from.SendLocalizedMessage(1070714);
            return false;
        }

        if (CheckCombat(from, TimeSpan.FromMinutes(2.0)))
        {
            // You must wait two minutes after engaging in combat before you can use a Soulstone.
            from.SendLocalizedMessage(1070727);
            return false;
        }

        if (from.Criminal)
        {
            // You must wait two minutes after committing a criminal act before you can use a Soulstone.
            from.SendLocalizedMessage(1070728);
            return false;
        }

        if (from.Region.GetLogoutDelay(from) > TimeSpan.Zero)
        {
            // In order to use your Soulstone, you must be in a safe log-out location.
            from.SendLocalizedMessage(1070729);
            return false;
        }

        if (!from.Alive)
        {
            from.SendLocalizedMessage(1070730); // You may not use a Soulstone while your character is dead.
            return false;
        }

        if (Sigil.ExistsOn(from))
        {
            // You may not use a Soulstone while your character has a faction town sigil.
            from.SendLocalizedMessage(1070731);
            return false;
        }

        if (from.Spell?.IsCasting == true)
        {
            from.SendLocalizedMessage(1070733); // You may not use a Soulstone while your character is casting a spell.
            return false;
        }

        if (from.Poisoned)
        {
            from.SendLocalizedMessage(1070734); // You may not use a Soulstone while your character is poisoned.
            return false;
        }

        if (from.Paralyzed)
        {
            from.SendLocalizedMessage(1070735); // You may not use a Soulstone while your character is paralyzed.
            return false;
        }

        if ((from as PlayerMobile)?.AcceleratedStart > Core.Now)
        {
            // You may not use a soulstone while your character is under the effects of a Scroll of Alacrity.
            from.SendLocalizedMessage(1078115);
            return false;
        }

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!CheckUse(from))
        {
            return;
        }

        from.CloseGump<SelectSkillGump>();
        from.CloseGump<ConfirmSkillGump>();
        from.CloseGump<ConfirmTransferGump>();
        from.CloseGump<ConfirmRemovalGump>();
        from.CloseGump<ErrorGump>();

        if (IsEmpty)
        {
            from.SendGump(new SelectSkillGump(this, from));
        }
        else
        {
            from.SendGump(new ConfirmTransferGump(this, from));
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _lastUserName = reader.ReadString();
        Level = (SecureLevel)reader.ReadInt();
        _activeItemID = reader.ReadInt();
        _inactiveItemID = reader.ReadInt();
        Account = reader.ReadString();
        reader.ReadDateTime(); // m_NextUse - Not used

        _skill = (SkillName)reader.ReadEncodedInt();
        _skillValue = reader.ReadDouble();
    }

    private class SelectSkillGump : Gump
    {
        private readonly SoulStone _stone;

        public SelectSkillGump(SoulStone stone, Mobile from) : base(50, 50)
        {
            _stone = stone;

            AddPage(0);

            AddBackground(0, 0, 520, 440, 0x13BE);

            AddImageTiled(10, 10, 500, 20, 0xA40);
            AddImageTiled(10, 40, 500, 360, 0xA40);
            AddImageTiled(10, 410, 500, 20, 0xA40);

            AddAlphaRegion(10, 10, 500, 420);

            AddHtmlLocalized(10, 12, 500, 20, 1061087, 0x7FFF); // Which skill do you wish to transfer to the Soulstone?

            AddButton(10, 410, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 412, 450, 20, 1060051, 0x7FFF); // CANCEL

            for (int i = 0, n = 0; i < from.Skills.Length; i++)
            {
                var skill = from.Skills[i];

                if (skill.Base > 0.0)
                {
                    var p = n % 30;

                    if (p == 0)
                    {
                        var page = n / 30;

                        if (page > 0)
                        {
                            AddButton(260, 380, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);
                            AddHtmlLocalized(305, 382, 200, 20, 1011066, 0x7FFF); // Next page
                        }

                        AddPage(page + 1);

                        if (page > 0)
                        {
                            AddButton(10, 380, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page);
                            AddHtmlLocalized(55, 382, 200, 20, 1011067, 0x7FFF); // Previous page
                        }
                    }

                    var x = p % 2 == 0 ? 10 : 260;
                    var y = p / 2 * 20 + 40;

                    AddButton(x, y, 0xFA5, 0xFA6, i + 1);
                    AddHtmlLocalized(x + 45, y + 2, 200, 20, AosSkillBonuses.GetLabel(skill.SkillName), 0x7FFF);

                    n++;
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0 || !_stone.IsEmpty)
            {
                return;
            }

            var from = sender.Mobile;

            var iSkill = info.ButtonID - 1;
            if (iSkill < 0 || iSkill >= from.Skills.Length)
            {
                return;
            }

            var skill = from.Skills[iSkill];
            if (skill.Base <= 0.0)
            {
                return;
            }

            if (!_stone.CheckUse(from))
            {
                return;
            }

            from.SendGump(new ConfirmSkillGump(_stone, skill));
        }
    }

    private class ConfirmSkillGump : Gump
    {
        private readonly Skill _skill;
        private readonly SoulStone _stone;

        public ConfirmSkillGump(SoulStone stone, Skill skill) : base(50, 50)
        {
            _stone = stone;
            _skill = skill;

            AddBackground(0, 0, 520, 440, 0x13BE);

            AddImageTiled(10, 10, 500, 20, 0xA40);
            AddImageTiled(10, 40, 500, 360, 0xA40);
            AddImageTiled(10, 410, 500, 20, 0xA40);

            AddAlphaRegion(10, 10, 500, 420);

            AddHtmlLocalized(10, 12, 500, 20, 1070709, 0x7FFF); // <CENTER>Confirm Soulstone Transfer</CENTER>

            /* <CENTER>Soulstone</CENTER><BR>
             * You are using a Soulstone.  This powerful artifact allows you to remove skill points
             * from your character and store them in the stone for later retrieval.  In order to use
             * the stone, you must make sure your Skill Lock for the indicated skill is pointed downward.
             * Click the "Skills" button on your Paperdoll to access the Skill List, and double-check
             * your skill lock.<BR><BR>
             *
             * Once you activate the stone, all skill points in the indicated skill will be removed from
             * your character.  These skill points can later be retrieved.  IMPORTANT: When retrieving
             * skill points from a Soulstone, the Soulstone WILL REPLACE any existing skill points
             * already on your character!<BR><BR>
             *
             * This is an Account Bound Soulstone.  Skill pointsstored inside can be retrieved by any
             * character on the same account as the character who placed them into the stone.
             */
            AddHtmlLocalized(10, 42, 500, 110, 1061067, 0x7FFF, false, true);

            AddHtmlLocalized(10, 200, 390, 20, 1062297, 0x7FFF); // Skill Chosen:
            AddHtmlLocalized(210, 200, 390, 20, AosSkillBonuses.GetLabel(skill.SkillName), 0x7FFF);

            AddHtmlLocalized(10, 220, 390, 20, 1062298, 0x7FFF); // Current Value:
            AddLabel(210, 220, 0x481, skill.Base.ToString("F1"));

            AddHtmlLocalized(10, 240, 390, 20, 1062299, 0x7FFF); // Current Cap:
            AddLabel(210, 240, 0x481, skill.Cap.ToString("F1"));

            AddHtmlLocalized(10, 260, 390, 20, 1062300, 0x7FFF); // New Value:
            AddLabel(210, 260, 0x481, "0.0");

            AddButton(10, 360, 0xFA5, 0xFA6, 2);

            // Activate the stone.  I am ready to transfer the skill points to it.
            AddHtmlLocalized(45,362,450,20,1070720,0x7FFF);

            AddButton(10, 380, 0xFA5, 0xFA6, 1);
            AddHtmlLocalized(45, 382, 450, 20, 1062279, 0x7FFF); // No, let me make another selection.

            AddButton(10, 410, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 412, 450, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0 || !_stone.IsEmpty)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_stone.CheckUse(from))
            {
                return;
            }

            if (info.ButtonID == 1) // Is asking for another selection
            {
                from.SendGump(new SelectSkillGump(_stone, from));
                return;
            }

            if (_skill.Base <= 0.0)
            {
                return;
            }

            if (_skill.Lock != SkillLock.Down)
            {
                // <CENTER>Unable to Transfer Selected Skill to Soulstone</CENTER>

                /* You cannot transfer the selected skill to the Soulstone at this time. The selected
                 * skill may be locked or set to raise in your skill menu. Click on "Skills" in your
                 * paperdoll menu to check your raise/locked/lower settings and your total skills.
                 * Make any needed adjustments, then click "Continue". If you do not wish to transfer
                 * the selected skill at this time, click "Cancel".
                 */

                from.SendGump(new ErrorGump(_stone, 1070710, 1070711));
                return;
            }

            _stone.Skill = _skill.SkillName;
            _stone.SkillValue = _skill.Base;

            _skill.Base = 0.0;

            // You have successfully transferred your skill points into the Soulstone.
            from.SendLocalizedMessage(1070712);

            _stone.LastUserName = from.RawName;

            Effects.SendLocationParticles(
                EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                0,
                0,
                0,
                0,
                0,
                5060,
                0
            );
            Effects.PlaySound(from.Location, from.Map, 0x243);

            Effects.SendMovingParticles(
                new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 6, from.Z + 15), from.Map),
                from,
                0x36D4,
                7,
                0,
                false,
                true,
                0x497,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );

            Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
        }
    }

    private class ConfirmTransferGump : Gump
    {
        private readonly SoulStone _stone;

        public ConfirmTransferGump(SoulStone stone, Mobile from) : base(50, 50)
        {
            _stone = stone;

            AddBackground(0, 0, 520, 440, 0x13BE);

            AddImageTiled(10, 10, 500, 20, 0xA40);
            AddImageTiled(10, 40, 500, 360, 0xA40);
            AddImageTiled(10, 410, 500, 20, 0xA40);

            AddAlphaRegion(10, 10, 500, 420);

            AddHtmlLocalized(10, 12, 500, 20, 1070709, 0x7FFF); // <CENTER>Confirm Soulstone Transfer</CENTER>

            /* <CENTER>Soulstone</CENTER><BR>
             * You are using a Soulstone.  This powerful artifact allows you to remove skill points
             * from your character and store them in the stone for later retrieval.  In order to use
             * the stone, you must make sure your Skill Lock for the indicated skill is pointed downward.
             * Click the "Skills" button on your Paperdoll to access the Skill List, and double-check
             * your skill lock.<BR><BR>
             *
             * Once you activate the stone, all skill points in the indicated skill will be removed from
             * your character.  These skill points can later be retrieved.  IMPORTANT: When retrieving
             * skill points from a Soulstone, the Soulstone WILL REPLACE any existing skill points
             * already on your character!<BR><BR>
             *
             * This is an Account Bound Soulstone.  Skill pointsstored inside can be retrieved by any
             * character on the same account as the character who placed them into the stone.
             */
            AddHtmlLocalized(10, 42, 500, 110, 1061067, 0x7FFF, false, true);

            AddHtmlLocalized(10, 200, 390, 20, 1070718, 0x7FFF); // Skill Stored:
            AddHtmlLocalized(210, 200, 390, 20, AosSkillBonuses.GetLabel(stone.Skill), 0x7FFF);

            var fromSkill = from.Skills[stone.Skill];

            AddHtmlLocalized(10, 220, 390, 20, 1062298, 0x7FFF); // Current Value:
            AddLabel(210, 220, 0x481, fromSkill.Base.ToString("F1"));

            AddHtmlLocalized(10, 240, 390, 20, 1062299, 0x7FFF); // Current Cap:
            AddLabel(210, 240, 0x481, fromSkill.Cap.ToString("F1"));

            AddHtmlLocalized(10, 260, 390, 20, 1062300, 0x7FFF); // New Value:
            AddLabel(210, 260, 0x481, stone.SkillValue.ToString("F1"));

            AddButton(10, 360, 0xFA5, 0xFA6, 2);

            // Activate the stone.  I am ready to retrieve the skill points from it.
            AddHtmlLocalized(45,362,450,20,1070719,0x7FFF);

            AddButton(10, 380, 0xFA5, 0xFA6, 1);

            // Remove all skill points from this stone and DO NOT absorb them.
            AddHtmlLocalized(45,382,450,20,1070723,0x7FFF);

            AddButton(10, 410, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 412, 450, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0 || _stone.IsEmpty)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_stone.CheckUse(from))
            {
                return;
            }

            if (info.ButtonID == 1) // Remove skill points
            {
                from.SendGump(new ConfirmRemovalGump(_stone));
                return;
            }

            var skillValue = _stone.SkillValue;
            var fromSkill = from.Skills[_stone.Skill];

            /* If we have, say, 88.4 in our skill and the stone holds 100, we need
             * 11.6 free points. Also, if we're below our skillcap by, say, 8.2 points,
             * we only need 11.6 - 8.2 = 3.4 points.
             */
            var requiredAmount = (int)(skillValue * 10) - fromSkill.BaseFixedPoint - (from.SkillsCap - from.SkillsTotal);

            var cannotAbsorb = false;

            if (fromSkill.Lock != SkillLock.Up)
            {
                cannotAbsorb = true;
            }
            else if (requiredAmount > 0)
            {
                var available = 0;

                for (var i = 0; i < from.Skills.Length; ++i)
                {
                    if (from.Skills[i].Lock != SkillLock.Down)
                    {
                        continue;
                    }

                    available += from.Skills[i].BaseFixedPoint;
                }

                if (requiredAmount > available)
                {
                    cannotAbsorb = true;
                }
            }

            if (cannotAbsorb)
            {
                // <CENTER>Unable to Absorb Selected Skill from Soulstone</CENTER>

                /* You cannot absorb the selected skill from the Soulstone at this time. The selected
                 * skill may be locked or set to lower in your skill menu. You may also be at your
                 * total skill cap.  Click on "Skills" in your paperdoll menu to check your
                 * raise/locked/lower settings and your total skills.  Make any needed adjustments,
                 * then click "Continue". If you do not wish to transfer the selected skill at this
                 * time, click "Cancel".
                 */

                from.SendGump(new ErrorGump(_stone, 1070717, 1070716));
                return;
            }

            if (skillValue > fromSkill.Cap)
            {
                // <CENTER>Unable to Absorb Selected Skill from Soulstone</CENTER>

                /* The amount of skill stored in this stone exceeds your individual skill cap for
                 * that skill.  In order to retrieve the skill points stored in this stone, you must
                 * obtain a Power Scroll of the appropriate type and level in order to increase your
                 * skill cap.  You cannot currently retrieve the skill points stored in this stone.
                 */

                from.SendGump(new ErrorGump(_stone, 1070717, 1070715));
                return;
            }

            if (fromSkill.Base >= skillValue)
            {
                // <CENTER>Unable to Absorb Selected Skill from Soulstone</CENTER>

                /* You cannot transfer the selected skill to the Soulstone at this time. The selected
                 * skill has a skill level higher than what is stored in the Soulstone.
                 */

                // Wrong message?!

                from.SendGump(new ErrorGump(_stone, 1070717, 1070802));
                return;
            }

            if ((from as PlayerMobile)?.AcceleratedStart > Core.Now)
            {
                // <CENTER>Unable to Absorb Selected Skill from Soulstone</CENTER>

                /*You may not use a soulstone while your character is under the effects of a Scroll of Alacrity.*/

                // Wrong message?!

                from.SendGump(new ErrorGump(_stone, 1070717, 1078115));
                return;
            }

            if (requiredAmount > 0)
            {
                for (var i = 0; i < from.Skills.Length; ++i)
                {
                    if (from.Skills[i].Lock != SkillLock.Down)
                    {
                        continue;
                    }

                    if (requiredAmount >= from.Skills[i].BaseFixedPoint)
                    {
                        requiredAmount -= from.Skills[i].BaseFixedPoint;
                        from.Skills[i].Base = 0.0;
                    }
                    else
                    {
                        from.Skills[i].BaseFixedPoint -= requiredAmount;
                        break;
                    }
                }
            }

            fromSkill.Base = skillValue;
            _stone.SkillValue = 0.0;

            from.SendLocalizedMessage(1070713); // You have successfully absorbed the Soulstone's skill points.

            _stone.LastUserName = from.RawName;

            Effects.SendLocationParticles(
                EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                0,
                0,
                0,
                0,
                0,
                5060,
                0
            );
            Effects.PlaySound(from.Location, from.Map, 0x243);

            Effects.SendMovingParticles(
                new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 6, from.Z + 15), from.Map),
                from,
                0x36D4,
                7,
                0,
                false,
                true,
                0x497,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );

            Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

            if (_stone is SoulstoneFragment frag)
            {
                if (--frag.UsesRemaining <= 0)
                {
                    from.SendLocalizedMessage(1070974); // You have used up your soulstone fragment.
                }
            }
        }
    }

    private class ConfirmRemovalGump : Gump
    {
        private readonly SoulStone _stone;

        public ConfirmRemovalGump(SoulStone stone) : base(50, 50)
        {
            _stone = stone;

            AddBackground(0, 0, 520, 440, 0x13BE);

            AddImageTiled(10, 10, 500, 20, 0xA40);
            AddImageTiled(10, 40, 500, 360, 0xA40);
            AddImageTiled(10, 410, 500, 20, 0xA40);

            AddAlphaRegion(10, 10, 500, 420);

            AddHtmlLocalized(10, 12, 500, 20, 1070725, 0x7FFF); // <CENTER>Confirm Soulstone Skill Removal</CENTER>

            /* WARNING!<BR><BR>
             *
             * You are about to permanently remove all skill points stored in this Soulstone.
             * You WILL NOT absorb these skill points.  They will be DELETED.<BR><BR>
             *
             * Are you sure you wish to do this?  If not, press the Cancel button.
             */
            AddHtmlLocalized(10, 42, 500, 110, 1070724, 0x7FFF, false, true);

            AddButton(10, 380, 0xFA5, 0xFA6, 1);
            AddHtmlLocalized(45, 382, 450, 20, 1052072, 0x7FFF); // Continue

            AddButton(10, 410, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 412, 450, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0 || _stone.IsEmpty)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_stone.CheckUse(from))
            {
                return;
            }

            _stone.SkillValue = 0.0;
            from.SendLocalizedMessage(1070726); // You have successfully deleted the Soulstone's skill points.
        }
    }

    private class ErrorGump : Gump
    {
        private readonly SoulStone _stone;

        public ErrorGump(SoulStone stone, int title, int message) : base(50, 50)
        {
            _stone = stone;

            AddBackground(0, 0, 520, 440, 0x13BE);

            AddImageTiled(10, 10, 500, 20, 0xA40);
            AddImageTiled(10, 40, 500, 360, 0xA40);
            AddImageTiled(10, 410, 500, 20, 0xA40);

            AddAlphaRegion(10, 10, 500, 420);

            AddHtmlLocalized(10, 12, 500, 20, title, 0x7FFF);

            AddHtmlLocalized(10, 42, 500, 110, message, 0x7FFF, false, true);

            AddButton(10, 380, 0xFA5, 0xFA6, 1);
            AddHtmlLocalized(45, 382, 450, 20, 1052072, 0x7FFF); // Continue

            AddButton(10, 410, 0xFB1, 0xFB2, 0);
            AddHtmlLocalized(45, 412, 450, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0)
            {
                return;
            }

            var from = sender.Mobile;

            if (!_stone.CheckUse(from))
            {
                return;
            }

            if (_stone.IsEmpty)
            {
                from.SendGump(new SelectSkillGump(_stone, from));
            }
            else
            {
                from.SendGump(new ConfirmTransferGump(_stone, from));
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class SoulstoneFragment : SoulStone, IUsesRemaining
{
    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [Constructible]
    public SoulstoneFragment(string account) : this(5, account)
    {
    }

    [Constructible]
    public SoulstoneFragment(int usesRemaining = 5, string account = null) : base(account, Utility.Random(0x2AA1, 9)) =>
        _usesRemaining = usesRemaining;

    public override int LabelNumber => 1071000; // soulstone fragment

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    protected override bool CheckUse(Mobile from)
    {
        var canUse = base.CheckUse(from);

        if (canUse)
        {
            if (_usesRemaining <= 0)
            {
                from.SendLocalizedMessage(1070975); // That soulstone fragment has no more uses.
                return false;
            }
        }

        return canUse;
    }
}

[Flippable]
[SerializationGenerator(0, false)]
public partial class BlueSoulstone : SoulStone
{
    [Constructible]
    public BlueSoulstone(string account = null) : base(account, 0x2ADC, 0x2ADD)
    {
    }

    public void Flip()
    {
        ItemID = ItemID switch
        {
            0x2ADC => 0x2AEC,
            0x2ADD => 0x2AED,
            0x2AEC => 0x2ADC,
            0x2AED => 0x2ADD,
            _      => ItemID
        };
    }
}

[SerializationGenerator(0, false)]
public partial class RedSoulstone : SoulStone, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public RedSoulstone(string account = null): base(account, 0x32F3, 0x32F4)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076217); // 1st Year Veteran Reward
        }
    }
}
