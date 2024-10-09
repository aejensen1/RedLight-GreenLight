using HarmonyLib;
using Unity.Netcode;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(StartMatchLever))]
    public class StartMatchLeverPatch
    {
        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        public static void StartGamePatch()
        {
            RedLightGreenLight.mls.LogInfo("StartGamePatch");
            RedLightGreenLight.Instance.BeginGame();
        }
    }
}
