using BepInEx.Configuration;

namespace BulkEngineUpdateMod.Config
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> EnableFeature;

        public static void Init(ConfigFile config)
        {
            EnableFeature = config.Bind(
                "General",
                "EnableFeature",
                true,
                "Activer ou désactiver le mod."
            );
        }
    }
}