using HarmonyLib;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(QuickMenuManager))]
    public class QuickMenuManagerPatch
    {
        [HarmonyPatch("LeaveGame")]
        [HarmonyPostfix]
        public static void LeaveGamePatch()
        {
            RedLightGreenLight.Instance.EndGame();
        }
    }
}
