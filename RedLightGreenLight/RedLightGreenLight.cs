using System.Collections;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using RedLightGreenLight.Patches;
using System;
using Unity.Netcode;


namespace RedLightGreenLight
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class RedLightGreenLight : BaseUnityPlugin
    {
        public const string modGUID = "ironthumb.RedLightGreenLight";
        public const string modName = "RedLightGreenLight";
        public const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource? mls;
        private static RedLightGreenLight? instance;
        public static float delay;
        public static float yellowDelay = 2f;
        private static bool isGreen;
        public static bool gameIsActive = false;
        private static List<int>? damagePenalties;
        private static int penaltyNum;

        public static RedLightGreenLight Instance
        {
            get
            {
                //mls.LogInfo("Instance");
                if (instance == null)
                {
                    // Find existing instances
                    instance = FindObjectOfType<RedLightGreenLight>();

                    if (instance == null)
                    {
                        // Create a new instance if none found
                        var gameObject = new GameObject("RedLightGreenLight");
                        DontDestroyOnLoad(gameObject);
                        instance = gameObject.AddComponent<RedLightGreenLight>();
                    }
                }
                return instance;
            }
        }

        public void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);

            mls = base.Logger;

            damagePenalties = new List<int> { 5, 10, 20, 40, 80, 160 };

            try
            {
                harmony.Patch(original: AccessTools.Method(typeof(StartOfRound), "Disconnect"), postfix: new HarmonyMethod(typeof(StartOfRoundPatch), "DisconnectPatch"));
                //mls.LogInfo("StartOfRound patches applied successfully.");

                harmony.Patch(original: AccessTools.Method(typeof(NetworkSceneManager), "OnSceneLoaded"), postfix: new HarmonyMethod(typeof(NetworkSceneManagerPatch), "OnSceneLoadedPatch"));
                //mls.LogInfo("NetworkSceneManager patches applied successfully.");

                harmony.Patch(original: AccessTools.Method(typeof(GameNetworkManager), "Disconnect"), postfix: new HarmonyMethod(typeof(GameNetworkManagerPatch), "DisconnectPatch"));
                //mls.LogInfo("GameNetworkManager patches applied successfully.");

                mls.LogInfo("Red Light Green Light patches applied successfully!");
            }
            catch (Exception ex)
            {
                mls.LogError($"Failed to apply Harmony patches: {ex}");
                throw; // Rethrow the exception to indicate initialization failure
            }

            mls.LogInfo("Finished loading Red Light Green Light");
        }

        public void BeginGame()
        {
            isGreen = true;
            penaltyNum = 0;
            gameIsActive = true;
            mls.LogInfo("Begin Green Light/Red Light");
            GameCycles();
        }

        public void EndGame()
        {
            gameIsActive = false;
            mls.LogInfo("End Green Light/Red Light");
        }

        IEnumerator GameCycles()
        {
            if (gameIsActive)
            {
                WaitForMovement();
            }
            while (gameIsActive)
            {
                if (isGreen) // Longer delay for green light
                {
                    delay = UnityEngine.Random.Range(7f, 12f);
                    mls.LogInfo($"Delay until next light change: {delay} seconds");
                }
                else // Shorter delay for red light
                {
                    delay = UnityEngine.Random.Range(5f, 10f);
                    mls.LogInfo($"Delay until next light change: {delay} seconds");
                }

                yield return new WaitForSecondsRealtime(delay);
                
                if (isGreen) // yellow warning light (grace period)
                {
                    mls.LogInfo($"Yellow Light activated");
                    yield return new WaitForSecondsRealtime(yellowDelay);
                }

                isGreen = !isGreen; // Toggle light status
            }
        }

        IEnumerator WaitForMovement()
        {
            while (gameIsActive)
            {
                if (Player.m_localPlayer != null && isGreen == false)
                {
                    if (Player.m_localPlayer.m_moveDir != Vector3.zero)
                    {
                        mls.LogInfo("Player moved. Damaging Player.");
                        DamagePlayer();
                    }
                }
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        public void DamagePlayer()
        {
            if (penaltyNum < damagePenalties.Count)
            {
                Player.m_localPlayer.TakeDamage(damagePenalties[penaltyNum], true, null, true);
                mls.LogInfo($"Player took {damagePenalties[penaltyNum]} damagefor moving during a red light.");
                penaltyNum++;
            }
            else
            {
                mls.LogInfo("Player took too much damage. Killing player.");
                Player.m_localPlayer.KillMe();
            }
        }
    }
}
