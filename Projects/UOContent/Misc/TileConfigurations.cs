namespace Server.Misc
{
    public static class TileConfigurations
    {
        public static void Configure()
        {
            // OSI Client Patch 7.0.9.0
            MultiComponentList.PostHSFormat =
                ServerConfiguration.GetOrUpdateSetting(
                    "maps.enablePostHSMultiComponentFormat",
                    true
                );
        }
    }
}
