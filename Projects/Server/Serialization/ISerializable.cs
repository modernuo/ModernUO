using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
  public interface ISerializable
  {
    int TypeReference { get; }
    uint SerialIdentity { get; }
    void Serialize(IGenericWriter writer);
  }
}
