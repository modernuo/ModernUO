#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Server;

public class StringList
{
    private static readonly char[] m_Tab = { '\t' };

    //C# argument support
    public static readonly Regex FormatExpression = new( @"~(\d)+_.*?~", RegexOptions.IgnoreCase );

    static StringList() => Localization = new StringList();

    public StringList()
        : this( "enu" )
    {
    }

    public StringList( string language )
        : this( language, true )
    {
    }

    public StringList( string language, bool format )
    {
        Language = language;

        var path = Core.FindDataFile( $"Cliloc.{language}" );

        if ( path == null )
        {
            Console.WriteLine( $"Cliloc.{language} not found" );
            return;
        }

        using ( var bin = new BinaryReader( new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.Read ) ) )
        {
            var buffer = new byte[1024];

            bin.ReadInt32();
            bin.ReadInt16();

            while ( bin.BaseStream.Length != bin.BaseStream.Position )
            {
                var number = bin.ReadInt32();

                bin.ReadByte(); // flag

                int length = bin.ReadInt16();

                if ( length > buffer.Length )
                {
                    buffer = new byte[( length + 1023 ) & ~1023];
                }

                bin.Read( buffer, 0, length );

                var text = Encoding.UTF8.GetString( buffer, 0, length );

                var se = new StringEntry( number, text );

                EntryTable[number] = se;
                StringTable[number] = text;
            }
        }
    }

    public static StringList Localization { get; }

    public Dictionary<int, StringEntry> EntryTable { get; } = new();

    public Dictionary<int, string> StringTable { get; } = new();

    public string Language { get; private set; }

    public string this[ int number ] => GetString( number );

    public static string MatchComparison( Match m ) => $"{{{Utility.ToInt32( m.Groups[1].Value ) - 1}}}";

    public static string FormatArguments( string entry ) => FormatExpression.Replace( entry, MatchComparison );

    public static string CombineArguments( string str, string args )
    {
        if ( !string.IsNullOrEmpty( args ) )
        {
            return CombineArguments( str, args.Split( m_Tab ) );
        }

        return str;
    }

    public static string CombineArguments( string str, params object[] args ) => string.Format( str, args );

    public static string CombineArguments( int number, string args ) => CombineArguments( number, args.Split( m_Tab ) );

    public static string CombineArguments( int number, params object[] args ) => string.Format( Localization[number], args );

    public StringEntry GetEntry( int number )
    {
        if ( EntryTable.TryGetValue( number, out var entry ) )
        {
            return entry;
        }

        return null;
    }

    public string GetString( int number )
    {
        if ( StringTable.TryGetValue( number, out var entry ) )
        {
            return entry;
        }

        return null;
    }
}

public class StringEntry
{
    private static readonly Regex m_RegEx = new(
        @"~(\d+)[_\w]+~",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant
    );

    private static readonly object[] m_Args = { "", "", "", "", "", "", "", "", "", "", "" };

    private string m_FmtTxt;

    public StringEntry( int number, string text )
    {
        Number = number;
        Text = text;
    }

    public int Number { get; private set; }
    public string Text { get; }

    public string Format( params object[] args )
    {
        if ( m_FmtTxt == null )
        {
            m_FmtTxt = m_RegEx.Replace( Text, @"{$1}" );
        }

        for ( var i = 0; i < args.Length && i < 10; i++ )
        {
            m_Args[i + 1] = args[i];
        }

        return string.Format( m_FmtTxt, m_Args );
    }
}
