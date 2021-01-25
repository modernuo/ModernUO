namespace Server.Misc
{
    public static class TileConfigurations
    {
        public static void Configure()
        {
            // Using this requires the old mapDif files to be present. Only needed to support Clients < 6.0.0.0
            TileMatrixPatch.Enabled = ServerConfiguration.GetOrUpdateSetting("maps.enableTileMatrixPatches", !Core.SE);

            // OSI Client Patch 7.0.9.0
            MultiComponentList.PostHSFormat =
                ServerConfiguration.GetOrUpdateSetting(
                    "maps.enablePostHSMultiComponentFormat",
                    true
                );
        }
    }
}
