using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[Flippable(0x234E, 0x234F)]
[SerializationGenerator(1)]
public partial class TapestryOfSosaria : Item, ISecurable
{
    [SerializedIgnoreDupe]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public TapestryOfSosaria() : base(0x234E)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1062917; // The Tapestry of Sosaria

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            TapestryOfSosariaGump.DisplayTo(from);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        Level = (SecureLevel)reader.ReadEncodedInt();
    }

    private class TapestryOfSosariaGump : StaticGump<TapestryOfSosariaGump>
    {
        public override bool Singleton => true;

        private TapestryOfSosariaGump() : base(50, 50)
        {
        }

        public static void DisplayTo(Mobile from)
        {
            if (from?.NetState == null)
            {
                return;
            }

            from.SendGump(new TapestryOfSosariaGump());
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddImage(0, 0, 0x2C95);
        }
    }
}
