using HarmonyLib;
using Unity.Netcode;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(NetworkSceneManager))]
    public class NetworkSceneManagerPatch
    {
        [HarmonyPatch("OnSceneLoaded")]
        [HarmonyPostfix]
        public static void OnSceneLoadedPatch()
        {
            RedLightGreenLight.mls.LogInfo("OnSceneLoadedPatch");
            RedLightGreenLight.Instance.BeginGame();
        }
    }
}
