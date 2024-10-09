using HarmonyLib;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundPatch
    {
        [HarmonyPatch("EndGameServerRpc")]
        [HarmonyPostfix]
        public static void EndGameServerRpcPatch(int playerClientId)
        {
            RedLightGreenLight.mls.LogInfo("EndGameServerRpcPatch");
            RedLightGreenLight.Instance.EndGame();
        }
    }
}
