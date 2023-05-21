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
        private string _exampleText;

        [CommandProperty(AccessLevel.GameMaster)]
        public string ExampleText
        {
            get => _exampleText;
            set
            {
                if (value != _exampleText)
                {
                    _exampleText = value;
                    this.MarkDirty();
                }
            }
        }

        [Constructible]
        public ExampleItem(string text) : base(0)
        {
            Example = text;
        }

        public ExampleItem(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); //Version
            writer.Write(_exampleText);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            // version 0
            _exampleText = reader.ReadString();
        }
    }
    ```

    Same class serialized with codegen
    ```cs
    [SerializationGenerator(0)]
    public partial class ExampleItem : Item
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _exampleText;

        [Constructible]
        public ExampleItem(string text) : base(0)
        {
            _exampleText = text;
        }
    }
    ```

    ### Step by step
    1. Add ```SerializationGenerator(versionNumber)``` to your class and make the class ```partial```
        ```cs
        [SerializationGenerator(0)]
        public partial class ExampleItem : Item
        ```
    1. Delete the constructor with ```Serial serial```
    1. Delete the ```Serialize``` and ```Deserialize``` methods.
    1. Add ```SerializableField(fieldOrder)``` attribute to all field you want to serialize.
      ```cs
      [SerializableField(0)]
      private string _exampleText;
      ```
    1. Run `publish.cmd`.

    ModernUO will create migration files for you. In this case "Server.Items.ExampleItem.v0.json" and "Server.Items.ExampleItem.Serialization.cs".
    These files contains all information and classes needed for ModernUO to serialize/deserialize your objects.

    ### Migrations
    When new field is added to the serialization, you need to increment `versionNumber` and run `publish.cmd` again to generate a migration file for the new version.
    Here is little example.

    New class code will look like this:
    ```cs
    [SerializationGenerator(1)]
    public partial class ExampleItem : Item
    {
        [SerializableField(0)]
        private string _exampleText;

        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _addedExampleTest;

        [Constructible]
        public ExampleItem(string text, string addedText) : base(0)
        {
          _exampleText = text;
          _addedExampleTest = addedText;
        }
    }
    ```

    Your IDE (Visual Studio, Rider, or VSCode), will show errors and the ModernUO will not compile.
    This happens because the code generator builds a migration from V0 to V1 and a new struct `V0Content` with all of the V0 fields is generated.
    ModernUO is expecting the developer to create a migration from the old version to the new.
    Now create a `MigrateFrom` method for each of the older versions, in this case V0, to the new version.

    ```cs
    private void MigrateFrom(V0Content content)
    {
        _exampleText = content.ExampleText;
    }
    ```

    !!! Tip
        Since the class is `partial`, you can create a standalone file for migrations to keep them organized. For example "ExampleItem.Migrations.cs"

    Your migration is now complete.

    ### Migrating from pre-codegen
    To migrate from pre-codegen serialization, change the old Deserialize method to this:
    ```cs
    private void Deserialize(IGenericReader reader, int version)
    ```

    This method will be automatically called when the serialization generator doesn't have a migration for for that older version.
    In this method you can make old fashioned deserializations that are mostly compatible with RunUO.

    ### After Deserialization
    To execute code after the deserialization, add the `AfterDeserialization()` attribute to a method.
    ```cs
    [AfterDeserialization]
    private void AfterDeserialization()
    {
      // Some code here
    }
    ```

    !!! Tip
        By default, `AfterDeserialization` is executed synchronously right after the actual deserialization. Passing `false` to the attribute will make it execute after all deserializations.

    ### Serializing Non-Entity Classes
    Sometimes you might need to serialize a nested object that is not an `ISerializable`. The syntax is largely the same except you must also mark the *parent* object for dirty tracking by using the `DirtyTrackingEntity` attribute.
    A good example of this is ["AquariumState"](https://github.com/modernuo/ModernUO/blob/main/Projects/UOContent/Items/Aquarium/AquariumState.cs).


    !!! Note
        Non-entity classes cannot be serialized by reference since that would require a reference Serial or ID. This means if the object is a serializable property on multiple objects, it will be serialized
        multiple times and will effectively get duplicated on world load. Consider either making it an `ISerializable` type and building world load/save mechanisms, or do not serialize the nested object directly.
        Instead opt for a global lookup, serialize with generic persistence, and then reattach to the objects using the `WorldLoad` event sink.
