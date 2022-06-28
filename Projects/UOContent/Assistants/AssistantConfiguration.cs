using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Assistants;

public static class AssistantConfiguration
{
    private const string _path = "Configuration/assistants.json";

    public static AssistantSettings Settings { get; private set; }

    private const string _defaultWarningMessage = "The server was unable to negotiate features with your assistant. "
                                                  + "You must download and run an updated version of <A HREF=\"https://uosteam.com\">UOSteam</A>"
                                                  + " or <A HREF=\"https://github.com/markdwags/Razor/releases/latest\">Razor</A>."
                                                  + "<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, "
                                                  + "once you have this box checked you may log in and play normally."
                                                  + "<BR><BR>You will be disconnected shortly.";

    public static void Configure()
    {
        var path = Path.Join(Core.BaseDirectory, _path);

        if (File.Exists(path))
        {
            Settings = JsonConfig.Deserialize<AssistantSettings>(path);
        }
        else
        {
            Settings = new AssistantSettings
            {
                WarnOnFailure = true,
                KickOnFailure = true,
                DisallowedFeatures = AssistantFeatures.None,
                DisconnectDelay = TimeSpan.FromSeconds(15.0),
                WarningMessage = _defaultWarningMessage
            };

            if (!DetectRazorFromCUO())
            {
                Settings.SupportedRazorVersion = "1.7.3.36";
            }

            Save(path);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisallowFeature(AssistantFeatures feature) => SetDisallowed(feature, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AllowFeature(AssistantFeatures feature) => SetDisallowed(feature, false);

    public static void SetDisallowed(AssistantFeatures feature, bool value)
    {
        if (value)
        {
            Settings.DisallowedFeatures |= feature;
        }
        else
        {
            Settings.DisallowedFeatures &= ~feature;
        }

        Save();
    }

    private static bool DetectRazorFromCUO()
    {
        var path = Core.FindDataFile("settings.json", false);

        if (File.Exists(path))
        {
            var settings = JsonConfig.Deserialize<UOClient.CUOSettings>(path);
            var rootDirectory = new FileInfo(path).Directory?.FullName;

            for (var i = 0; i < settings.Plugins.Length; i++)
            {
                var pluginPath = settings.Plugins[i];
                var pluginFilePath = PathUtility.GetFullPath(pluginPath, rootDirectory);
                if (File.Exists(pluginFilePath) && new FileInfo(pluginFilePath).Name.InsensitiveEquals("razor"))
                {
                    var assemblyVersion = AssemblyName.GetAssemblyName(pluginFilePath).Version;
                    if (assemblyVersion != null)
                    {
                        Settings.SupportedRazorVersion = assemblyVersion.ToString();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static void Save(string path = null)
    {
        path ??= Path.Join(Core.BaseDirectory, _path);
        JsonConfig.Serialize(path, Settings);
    }
}

public record AssistantSettings
{
    [JsonPropertyName("warnOnFailure")]
    public bool WarnOnFailure { get; set; }

    [JsonPropertyName("kickOnFailure")]
    public bool KickOnFailure { get; set; }

    [JsonPropertyName("disallowedFeatures")]
    [JsonConverter(typeof(FlagsConverter<AssistantFeatures>))]
    public AssistantFeatures DisallowedFeatures { get; set; }

    [JsonPropertyName("disconnectDelay")]
    public TimeSpan DisconnectDelay { get; set; }

    [JsonPropertyName("warningMessage")]
    public string WarningMessage { get; set; }

    [JsonPropertyName("supportedRazorVersion")]
    public string SupportedRazorVersion { get; set; }
}
