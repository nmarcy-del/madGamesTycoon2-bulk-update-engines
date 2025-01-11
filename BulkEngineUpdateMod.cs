using BepInEx;
using HarmonyLib;
using BulkEngineUpdateMod.Config;

namespace BulkEngineUpdateMod
{
    [BepInPlugin("com.del001.updateallengines", "Update All Engines Mod", "1.0.0")]
    [BepInProcess("Mad Games Tycoon 2.exe")]
    public class BulkEngineUpdateMod : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("com.del001.updateallengines");
            harmony.PatchAll();
            
            ModConfig.Init(Config);
        }
    }
}