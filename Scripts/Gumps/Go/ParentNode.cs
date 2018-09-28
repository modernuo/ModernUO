using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace Server.Gumps
{
  public interface IGoNode
  {
    ParentNode Parent{ get; }
    string Name{ get; }
  }
  
  public class ParentNode : IGoNode
  {
    public ParentNode(XmlTextReader xml, ParentNode parent)
    {
      Parent = parent;
      
      Parse(xml);
    }

    public ParentNode Parent{ get; }

    public IGoNode[] Children{ get; private set; }

    public string Name{ get; private set; }

    private void Parse(XmlTextReader xml)
    {
      Name = xml.MoveToAttribute("name") ? xml.Value : "empty";

      if (xml.IsEmptyElement)
        Children = new IGoNode[0];
      else
      {
        List<IGoNode> children = new List<IGoNode>();

        while (xml.Read() && (xml.NodeType == XmlNodeType.Element || xml.NodeType == XmlNodeType.Comment))
        {
          if (xml.NodeType == XmlNodeType.Comment)
            continue;

          if (xml.Name == "child")
            children.Add(new ChildNode(xml, this));
          else
            children.Add(new ParentNode(xml, this));
        }

        Children = children.ToArray();
      }
    }
  }
}