---
title: Serialization
---

# Serialization / Savings

=== "Generic persistence"
    ```Persistence.Serialize``` and ```Persistence.Deserialize``` is replaced with GenericPersistence class

    Example of how to persist a custom system.

    ```cs
    namespace Server.ExampleSystem
    {
        public static class ExampleSerialization
        {
            public static void Configure()
            {
                GenericPersistence.Register("ExampleSystem", Serialize, Deserialize);
            }

            public static void Serialize(IGenericWriter writer)
            {
                // Do serialization here
                writer.WriteEncodedInt(0); // version
            }

            public static void Deserialize(IGenericReader reader)
            {
                // Do deserialization here
                var version = reader.ReadEncodedInt();
            }
        }
    }
    ```

=== "Codegen"
    ### Basic info
    ModernUO can programatically generate migrations. This feature is based on internal C# Source generators [More info](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/)

    Old way of serializing objects:
    ```cs
    public class ExampleItem : Item
    {
      public string ExampleText { get; set; }

      [Constructible]
      public ExampleItem() : base(0)
      {

      }
      public ExampleItem(Serial serial) : base(serial)
      {
      }

      public override void Serialize(IGenericWriter writer)
      {
          base.Serialize(writer);

          writer.WriteEncodedInt(0); //Version
          writer.Write(ExampleText);
      }

      public override void Deserialize(IGenericReader reader)
      {
          base.Deserialize(reader);

          var version = reader.ReadEncodedInt();
          ExampleText = reader.ReadString();
      }
    }
    ```

    Same class serialized with codegen
    ```cs
    [Serializable(0)]
    public partial class ExampleItem : Item
    {
      [SerializableField(0)]
      public string ExampleText { get; set; }

      [Constructible]
      public ExampleItem() : base(0)
      {

      }
    }
    ```

    ### Step by step
    1. Add ```SerializableAttribute(versionNumber)``` to your class and make it ```partial```
        ```cs
        [Serializable(0)]
        public partial class ExampleItem : Item
        ```
    1. Delete constructors with ```Serial serial```, ```Serialize``` and ```Deserialize``` methods.
    1. Add ```SerializableField(fieldOrder)``` attribute to all field you want to serialize.
      ```cs
      [SerializableField(0)]
      public string ExampleText { get; set; }
      ```
    1. Build project "Run Schema Migrations". ModernUO will create migration files for you. In this case "Server.Items.ExampleItem.v0.json" and "Server.Items.ExampleItem.Serialization.cs"
      These files contains all information and classes needed for MUO to serialize/deserialize your objects.

    ### Migrations
    When new field is added to serialization, you need to increment versionNumber and make migration files. Here is little example.

    New class code will look like this:
    ```cs
    [Serializable(1)]
    public partial class ExampleItem : Item
    {
      [SerializableField(0)]
      public string ExampleText { get; set; }

      [SerializableField(1)]
      public string AddedExampleTest { get; set; }

      [Constructible]
      public ExampleItem() : base(0)
      {
      }
    }
    ```

    After building "Run Schema Migrations" project, MUO will generate V0Content in serialization class.

    This Content contains all fields from V0.
    Now create MigrateFrom for each version you make, in this case V0.

    !!! Tip
        When you have more versions, create standalone file for migrations only. For example "ExampleItem.Migrations.cs"

    ```cs
    private void MigrateFrom(V0Content content)
    {
        ExampleText = content.ExampleText;
    }
    ```

    Your migration is now completed.

    ### Migrating from pre-codegen
    For migration from pre-codegen code, use method
    ```cs
    private void Deserialize(IGenericReader reader, int version)
    ```

    this method is called when codegen doesnt have VXContent for deserialized object or version of Content is lower than deserialized.
    In this method you can make old fashioned deserialization as before codegen.

    ### After deserialization
    For some code changes after world load, you can use AfterDeserializationAttribute.
    ```cs
    [AfterDeserialization]
    private void AfterDeserialization()
    {
      // Some code here
    }
    ```

    ### Embedded serialization
    Sometimes you need to serialize object inside object. For this you should use "EmbeddedSerializableAttribute". Nice example to understand it is ["AquariumState"](https://github.com/modernuo/ModernUO/blob/main/Projects/UOContent/Items/Aquarium/AquariumState.cs)
