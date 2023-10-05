using System;
using ModernUO.Serialization;
using Server.Engines.Stealables;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BasePigmentsOfTokuno : Item, IUsesRemaining
{
    private static readonly Type[] m_Glasses =
    {
        typeof(MaritimeGlasses),
        typeof(WizardsGlasses),
        typeof(TradeGlasses),
        typeof(LyricalGlasses),
        typeof(NecromanticGlasses),
        typeof(LightOfWayGlasses),
        typeof(FoldedSteelGlasses),
        typeof(PoisonedGlasses),
        typeof(TreasureTrinketGlasses),
        typeof(MaceShieldGlasses),
        typeof(ArtsGlasses),
        typeof(AnthropomorphistGlasses)
    };

    private static readonly Type[] m_Replicas =
    {
        typeof(ANecromancerShroud),
        typeof(BraveKnightOfTheBritannia),
        typeof(CaptainJohnsHat),
        typeof(DetectiveBoots),
        typeof(DjinnisRing),
        typeof(EmbroideredOakLeafCloak),
        typeof(GuantletsOfAnger),
        typeof(LieutenantOfTheBritannianRoyalGuard),
        typeof(OblivionsNeedle),
        typeof(RoyalGuardSurvivalKnife),
        typeof(SamaritanRobe),
        typeof(TheMostKnowledgePerson),
        typeof(TheRobeOfBritanniaAri),
        typeof(AcidProofRobe),
        typeof(Calm),
        typeof(CrownOfTalKeesh),
        typeof(FangOfRactus),
        typeof(GladiatorsCollar),
        typeof(OrcChieftainHelm),
        typeof(Pacify),
        typeof(Quell),
        typeof(ShroudOfDeciet),
        typeof(Subdue)
    };

    private static readonly Type[] m_DyableHeritageItems =
    {
        typeof(ChargerOfTheFallen),
        typeof(SamuraiHelm),
        typeof(HolySword),
        typeof(LeggingsOfEmbers),
        typeof(ShaminoCrossbow)
    };

    private TextDefinition m_Label;

    [EncodedInt]
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    public BasePigmentsOfTokuno() : base(0xEFF)
    {
        Weight = 1.0;
        _usesRemaining = 1;
    }

    public BasePigmentsOfTokuno(int uses) : base(0xEFF)
    {
        Weight = 1.0;
        _usesRemaining = uses;
    }

    public BasePigmentsOfTokuno(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1070933; // Pigments of Tokuno

    protected TextDefinition Label
    {
        get => m_Label;
        set
        {
            m_Label = value;
            InvalidateProperties();
        }
    }

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        m_Label.AddTo(list);
        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsAccessibleTo(from) && from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(1070929); // Select the artifact or enhanced magic item to dye.
            from.BeginTarget(3, false, TargetFlags.None, InternalCallback);
        }
        else
        {
            from.SendLocalizedMessage(502436); // That is not accessible.
        }
    }

    private void InternalCallback(Mobile from, object targeted)
    {
        if (Deleted || UsesRemaining <= 0 || !from.InRange(GetWorldLocation(), 3) ||
            !IsAccessibleTo(from))
        {
            return;
        }

        if (targeted is not Item i)
        {
            from.SendLocalizedMessage(1070931); // You can only dye artifacts and enhanced magic items with this tub.
        }
        else if (!from.InRange(i.GetWorldLocation(), 3) || !IsAccessibleTo(from))
        {
            from.SendLocalizedMessage(502436); // That is not accessible.
        }
        else if (from.Items.Contains(i))
        {
            from.SendLocalizedMessage(1070930); // Can't dye artifacts or enhanced magic items that are being worn.
        }
        else if (i.IsLockedDown)
        {
            // You may not dye artifacts and enhanced magic items which are locked down.
            from.SendLocalizedMessage(1070932);
        }
        else if (i.QuestItem)
        {
            from.SendLocalizedMessage(1151836); // You may not dye toggled quest items.
        }
        else if (i is MetalPigmentsOfTokuno)
        {
            from.SendLocalizedMessage(1042417); // You cannot dye that.
        }
        else if (i is LesserPigmentsOfTokuno)
        {
            from.SendLocalizedMessage(1042417); // You cannot dye that.
        }
        else if (i is PigmentsOfTokuno)
        {
            from.SendLocalizedMessage(1042417); // You cannot dye that.
        }
        else if (!IsValidItem(i))
        {
            // You can only dye artifacts and enhanced magic items with this tub.	//Yes, it says tub on OSI.  Don't ask me why ;p
            from.SendLocalizedMessage(1070931);
        }
        else
        {
            // Notes: on OSI there IS no hue check to see if it's already hued.  and no messages on successful hue either
            i.Hue = Hue;

            if (--UsesRemaining <= 0)
            {
                Delete();
            }

            from.PlaySound(0x23E); // As per OSI TC1
        }
    }

    public static bool IsValidItem(Item i)
    {
        if (i is BasePigmentsOfTokuno)
        {
            return false;
        }

        var t = i.GetType();

        var resource = CraftResource.None;

        if (i is BaseWeapon weapon)
        {
            resource = weapon.Resource;
        }
        else if (i is BaseArmor armor)
        {
            resource = armor.Resource;
        }
        else if (i is BaseClothing clothing)
        {
            resource = clothing.Resource;
        }

        if (!CraftResources.IsStandard(resource))
        {
            return true;
        }

        return t.InTypeList(TreasuresOfTokuno.TokunoDyable)
               || t.InTypeList(TreasuresOfTokuno.LesserArtifactsTotal)
               || t.InTypeList(TreasuresOfTokuno.GreaterArtifacts)
               || t.InTypeList(DemonKnight.ArtifactRarity10)
               || t.InTypeList(DemonKnight.ArtifactRarity11)
               || t.InTypeList(MondainsLegacy.Artifacts)
               || t.InTypeList(StealableArtifacts.TypesOfEntries)
               || t.InTypeList(Paragon.Artifacts)
               || t.InTypeList(Leviathan.Artifacts)
               || t.InTypeList(TreasureMapChest.Artifacts)
               || t.InTypeList(m_Replicas)
               || t.InTypeList(m_DyableHeritageItems)
               || t.InTypeList(m_Glasses);
    }
}
