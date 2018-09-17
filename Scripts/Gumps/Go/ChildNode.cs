using System.Xml;

namespace Server.Gumps
{
  public class ChildNode
  {
    public ChildNode(XmlTextReader xml, ParentNode parent)
    {
      Parent = parent;

      Parse(xml);
    }

    public ParentNode Parent{ get; }

    public string Name{ get; private set; }

    public Point3D Location{ get; private set; }

    private void Parse(XmlTextReader xml)
    {
      if (xml.MoveToAttribute("name"))
        Name = xml.Value;
      else
        Name = "empty";

      int x = 0, y = 0, z = 0;

      if (xml.MoveToAttribute("x"))
        x = Utility.ToInt32(xml.Value);

      if (xml.MoveToAttribute("y"))
        y = Utility.ToInt32(xml.Value);

      if (xml.MoveToAttribute("z"))
        z = Utility.ToInt32(xml.Value);

      Location = new Point3D(x, y, z);
    }
  }
}