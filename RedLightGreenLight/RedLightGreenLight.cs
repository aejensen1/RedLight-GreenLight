using System.Collections;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System.Collections.Generic;
using RedLightGreenLight.Patches;
using System;
using Unity.Netcode;
using GameNetcodeStuff;
using RedLightGreenLight.Assets;
using System.Numerics;



namespace RedLightGreenLight
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class RedLightGreenLight : BaseUnityPlugin
    {
        public const string modGUID = "ironthumb.RedLightGreenLight";
        public const string modName = "RedLightGreenLight";
        public const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;
        private static RedLightGreenLight instance;

        public static float delay;
        public static float yellowDelay = 2f;
        private static bool isGreen;
        public static bool gameIsActive = false;
        private static List<int> damagePenalties;
        private static int penaltyNum;
        private PlayerControllerB player;

        private bool gameCyclesActive = false;
        private bool waitForMovementActive = false;
        private bool penaltyDealt;

        private UnityEngine.Quaternion previousCameraRotation;
        private UnityEngine.Quaternion currentCameraRotation;




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
                harmony.Patch(original: AccessTools.Method(typeof(StartOfRound), "EndGameServerRpc"), postfix: new HarmonyMethod(typeof(StartOfRoundPatch), "EndGameServerRpcPatch"));
                //mls.LogInfo("StartOfRound patches applied successfully.");

                harmony.Patch(original: AccessTools.Method(typeof(NetworkSceneManager), "OnSceneLoaded"), postfix: new HarmonyMethod(typeof(NetworkSceneManagerPatch), "OnSceneLoadedPatch"));
                //mls.LogInfo("NetworkSceneManager patches applied successfully.");

                harmony.Patch(original: AccessTools.Method(typeof(GameNetworkManager), "Disconnect"), postfix: new HarmonyMethod(typeof(GameNetworkManagerPatch), "DisconnectPatch"));
                //mls.LogInfo("GameNetworkManager patches applied successfully.");

                harmony.Patch(original: AccessTools.Method(typeof(QuickMenuManager), "LeaveGame"), postfix: new HarmonyMethod(typeof(QuickMenuManagerPatch), "LeaveGamePatch"));
                //mls.LogInfo("QuickMenuManager patches applied successfully.");


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
            player = FindObjectOfType<PlayerControllerB>();
            penaltyDealt = false;
            isGreen = true;
            penaltyNum = 0;
            gameIsActive = true;
            mls.LogInfo("Begin Green Light/Red Light");
            if (!gameCyclesActive)
            {
                StartCoroutine(GameCycles());
            }
        }

        public void EndGame()
        {
            gameIsActive = false;
            mls.LogInfo("End Green Light/Red Light");
        }

        IEnumerator GameCycles()
        {
            gameCyclesActive = true;
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
                    mls.LogInfo($"Delay until next light change: {yellowDelay} seconds");
                    yield return new WaitForSecondsRealtime(yellowDelay);
                    mls.LogInfo($"Red Light activated");
                    penaltyDealt = false;
                }
                else
                    mls.LogInfo($"Green Light activated");

                isGreen = !isGreen; // Toggle light status
                if (!waitForMovementActive && !isGreen)
                {
                    StartCoroutine(WaitForMovement());
                }
            }
            gameCyclesActive = false;
        }

        IEnumerator WaitForMovement()
        {
            waitForMovementActive = true; // Prevent multiple instances of this coroutine
            currentCameraRotation = player.gameplayCamera.transform.rotation; // Reset the base case for camera rotation
            previousCameraRotation = currentCameraRotation;

            while (!isGreen && !penaltyDealt)
            {
                if (player != null) // Check if player exists and is not in the green state
                {
                    // Check if the player has moved physically
                    mls.LogInfo($"player.timeSincePlayerMoving: {player.timeSincePlayerMoving}");
                    if (player.timeSincePlayerMoving < 0.1f)
                    {
                        
                        //mls.LogInfo($"player.velocity: {player.velocity}"); // This is always 0,0,0
                        mls.LogInfo("Player moved. Damaging Player.");
                        DamagePlayer();
                    }

                    // Check for camera movement
                    if (player.gameplayCamera != null) // Ensure gameplayCamera exists
                    {
                        currentCameraRotation = player.gameplayCamera.transform.rotation;

                        // Define a rotation threshold
                        float rotationThreshold = 2.0f; // Adjust this as needed

                        mls.LogInfo($"currentCameraRotation: {currentCameraRotation}");
                        mls.LogInfo($"previousCameraRotation: {previousCameraRotation}");
                        // Check if the rotation has changed beyond the threshold
                        if (UnityEngine.Quaternion.Angle(currentCameraRotation, previousCameraRotation) > rotationThreshold)
                        {
                            mls.LogInfo("Camera moved. Damaging Player.");
                            DamagePlayer();
                        }
                    }

                }
                yield return new WaitForSecondsRealtime(0.1f); // Wait before checking again
            }
            waitForMovementActive = false;
        }


        public void DamagePlayer()
        {
            if (penaltyNum < damagePenalties.Count)
            {
                player.DamagePlayer(damagePenalties[penaltyNum], true, true, CauseOfDeath.Unknown, 0, false, default(UnityEngine.Vector3));
                mls.LogInfo($"Player took {damagePenalties[penaltyNum]} damage for moving during a red light.");
                penaltyNum++;
            }
            else
            {
                mls.LogInfo("Player took too much damage. Killing player.");
                UnityEngine.Vector3 bodyVelocity = new UnityEngine.Vector3(0, 5f, 0);
                player.KillPlayer(bodyVelocity, true, CauseOfDeath.Unknown, 0, default(UnityEngine.Vector3));
            }
            penaltyDealt = true;
        }
    }
}
