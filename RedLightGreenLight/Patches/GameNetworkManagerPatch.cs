using HarmonyLib;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(GameNetworkManager))]
    public class GameNetworkManagerPatch
    {
        [HarmonyPatch("Disconnect")]
        [HarmonyPostfix]
        public static void DisconnectPatch()
        {
            RedLightGreenLight.Instance.EndGame();
        }
    }
}
