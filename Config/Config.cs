using BepInEx.Configuration;

namespace BulkEngineUpdateMod.Config
{
    public static class ModConfig
    {
        public static ConfigEntry<int> EnginePrice;
        public static ConfigEntry<int> EngineProfitShare;

        public static void Init(ConfigFile config)
        {
            EnginePrice = config.Bind(
                "Engine", 
                "EnginePrice", 
                3000,
                new ConfigDescription(
                    "Définir le prix de base des moteurs (1 à 100,000).",
                    new AcceptableValueRange<int>(1, 100000)
                )
            );
            EngineProfitShare = config.Bind(
                "Engine", 
                "EngineProfitShare", 
                3,
                new ConfigDescription(
                    "Définir la part de bénéfice pour les moteurs (1 à 50).",
                    new AcceptableValueRange<int>(1, 50)
                )
            );
        }
    }
}