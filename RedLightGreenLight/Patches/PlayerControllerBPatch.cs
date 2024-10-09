using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace RedLightGreenLight.Patches
{
    
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPostfix]
        public static void DamagePlayerPatch()
        {
            
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayerPatch()
        {

        }
    }
}
