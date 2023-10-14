using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class KronusScroll : QuestItem
{
    private static readonly Rectangle2D m_WellOfTearsArea = new(2080, 1346, 10, 10);
    private static readonly Map m_WellOfTearsMap = Map.Malas;

    [Constructible]
    public KronusScroll() : base(0x227A)
    {
        Weight = 1.0;
        Hue = 0x44E;
    }

    public override int LabelNumber => 1060149; // Calling of Kronus

    public override bool CanDrop(PlayerMobile player) => player.Quest is not DarkTidesQuest;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from))
        {
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        var qs = pm.Quest;

        if (qs is DarkTidesQuest)
        {
            if (qs.IsObjectiveInProgress(typeof(FindMardothAboutKronusObjective)))
            {
                // You read the scroll, but decide against performing the calling until you are instructed to do so by Mardoth.
                pm.SendLocalizedMessage(1060151, "", 0x41);
            }
            else if (qs.IsObjectiveInProgress(typeof(FindWellOfTearsObjective)))
            {
                // You must be at the Well of Tears in the city of Necromancers to use this scroll.
                pm.SendLocalizedMessage(1060152, "", 0x41);
            }
            else if (qs.IsObjectiveInProgress(typeof(UseCallingScrollObjective)))
            {
                if (pm.Map == m_WellOfTearsMap && m_WellOfTearsArea.Contains(pm.Location))
                {
                    QuestObjective obj = qs.FindObjective<UseCallingScrollObjective>();

                    if (obj?.Completed == false)
                    {
                        obj.Complete();
                    }

                    Delete();
                    new CallingTimer(pm).Start();
                }
                else
                {
                    // You must be at the Well of Tears in the city of Necromancers to use this scroll.
                    pm.SendLocalizedMessage(1060152, "", 0x41);
                }
            }
            else
            {
                // A strange terror grips your heart as you attempt to read the scroll.  You decide it would be a bad idea to read it out loud.
                pm.SendLocalizedMessage(1060150, "", 0x41);
            }
        }
    }

    private class CallingTimer : Timer
    {
        private readonly PlayerMobile _player;
        private int m_Step;

        public CallingTimer(PlayerMobile player) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), 6)
        {

            _player = player;
            m_Step = 0;
        }

        protected override void OnTick()
        {
            if (_player.Deleted)
            {
                Stop();
                return;
            }

            if (!_player.Mounted)
            {
                _player.Animate(Utility.RandomBool() ? 16 : 17, 7, 1, true, false, 0);
            }

            if (m_Step == 4)
            {
                var baseX = m_WellOfTearsArea.X;
                var baseY = m_WellOfTearsArea.Y;
                var width = m_WellOfTearsArea.Width;
                var height = m_WellOfTearsArea.Height;
                var map = m_WellOfTearsMap;

                Effects.SendLocationParticles(
                    EffectItem.Create(_player.Location, _player.Map, TimeSpan.FromSeconds(1.0)),
                    0,
                    0,
                    0,
                    0x13C4
                );
                Effects.PlaySound(_player.Location, _player.Map, 0x243);

                for (var i = 0; i < 15; i++)
                {
                    var x = baseX + Utility.Random(width);
                    var y = baseY + Utility.Random(height);
                    var z = map.GetAverageZ(x, y);

                    var from = new Point3D(x, y, z + Utility.RandomMinMax(5, 20));
                    var to = new Point3D(x, y, z);

                    var hue = Utility.RandomList(0x481, 0x482, 0x489, 0x497, 0x66D);

                    Effects.SendMovingEffect(map, 0x36D4, from, to,0, 0, false, true, hue);
                }
            }

            if (m_Step < 5)
            {
                _player.Frozen = true;
            }
            else // Cast completed
            {
                _player.Frozen = false;

                SummonedPaladin.BeginSummon(_player);
            }

            m_Step++;
        }
    }
}
