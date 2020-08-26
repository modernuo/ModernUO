using System;

namespace Server.Items
{
  public class SlayerEntry
  {
    private static readonly int[] m_AosTitles =
    {
      1060479, // undead slayer
      1060470, // orc slayer
      1060480, // troll slayer
      1060468, // ogre slayer
      1060472, // repond slayer
      1060462, // dragon slayer
      1060478, // terathan slayer
      1060475, // snake slayer
      1060467, // lizardman slayer
      1060473, // reptile slayer
      1060460, // demon slayer
      1060466, // gargoyle slayer
      1017396, // Balron Damnation
      1060461, // demon slayer
      1060469, // ophidian slayer
      1060477, // spider slayer
      1060474, // scorpion slayer
      1060458, // arachnid slayer
      1060465, // fire elemental slayer
      1060481, // water elemental slayer
      1060457, // air elemental slayer
      1060471, // poison elemental slayer
      1060463, // earth elemental slayer
      1060459, // blood elemental slayer
      1060476, // snow elemental slayer
      1060464, // elemental slayer
      1070855 // fey slayer
    };

    private static readonly int[] m_OldTitles =
    {
      1017384, // Silver
      1017385, // Orc Slaying
      1017386, // Troll Slaughter
      1017387, // Ogre Thrashing
      1017388, // Repond
      1017389, // Dragon Slaying
      1017390, // Terathan
      1017391, // Snake's Bane
      1017392, // Lizardman Slaughter
      1017393, // Reptilian Death
      1017394, // Daemon Dismissal
      1017395, // Gargoyle's Foe
      1017396, // Balron Damnation
      1017397, // Exorcism
      1017398, // Ophidian
      1017399, // Spider's Death
      1017400, // Scorpion's Bane
      1017401, // Arachnid Doom
      1017402, // Flame Dousing
      1017403, // Water Dissipation
      1017404, // Vacuum
      1017405, // Elemental Health
      1017406, // Earth Shatter
      1017407, // Blood Drinking
      1017408, // Summer Wind
      1017409, // Elemental Ban
      1070855 // fey slayer
    };

    public SlayerEntry(SlayerName name, params Type[] types)
    {
      Name = name;
      Types = types;
    }

    public SlayerGroup Group { get; set; }

    public SlayerName Name { get; }

    public Type[] Types { get; }

    public int Title
    {
      get
      {
        int[] titles = Core.AOS ? m_AosTitles : m_OldTitles;

        return titles[(int)Name - 1];
      }
    }

    public bool Slays(Mobile m)
    {
      Type t = m.GetType();

      for (int i = 0; i < Types.Length; ++i)
        if (Types[i].IsAssignableFrom(t))
          return true;

      return false;
    }
  }
}