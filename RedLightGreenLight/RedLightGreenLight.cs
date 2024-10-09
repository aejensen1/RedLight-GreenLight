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
using System.Runtime.CompilerServices;
using System.Linq;

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
        public static RedLightGreenLight instance;

        public static float delay;
        public static float yellowDelay = 2f;
        public static bool isGreen;
        public static bool gameIsActive = false;
        public static bool hostIsStartingGame = false;
        public static bool clientIsStartingGame = false;
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
                if (instance == null)
                {
                    instance = FindObjectOfType<RedLightGreenLight>();
                    if (instance == null)
                    {
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
                harmony.Patch(original: AccessTools.Method(typeof(StartOfRound), "EndGameServerRpc"),
                    postfix: new HarmonyMethod(typeof(StartOfRoundPatch), "EndGameServerRpcPatch"));

                harmony.Patch(original: AccessTools.Method(typeof(StartMatchLever), "StartGame"),
                    postfix: new HarmonyMethod(typeof(StartMatchLeverPatch), "StartGamePatch"));

                harmony.Patch(original: AccessTools.Method(typeof(GameNetworkManager), "Disconnect"),
                    postfix: new HarmonyMethod(typeof(GameNetworkManagerPatch), "DisconnectPatch"));

                harmony.Patch(original: AccessTools.Method(typeof(QuickMenuManager), "LeaveGame"),
                    postfix: new HarmonyMethod(typeof(QuickMenuManagerPatch), "LeaveGamePatch"));

                mls.LogInfo("Red Light Green Light patches applied successfully!");
            }
            catch (Exception ex)
            {
                mls.LogError($"Failed to apply Harmony patches: {ex}");
                throw;
            }

            mls.LogInfo("Finished loading Red Light Green Light");
        }

        public void BeginGame()
        {
            // Check if the game is already starting to prevent re-entry
            if (hostIsStartingGame && NetworkManager.Singleton.IsHost)
            {
                Debug.Log("BeginGame is already in progress. Exiting to prevent infinite loop.");
                return;
            }
            else
                hostIsStartingGame = true; // Set the flag to indicate that the game is starting

            if (clientIsStartingGame && !NetworkManager.Singleton.IsHost)
            {
                Debug.Log("BeginGame is already in progress. Exiting to prevent infinite loop.");
                return;
            }
            else
                clientIsStartingGame = true; // Set the flag to indicate that the game is starting

            RedLightGreenLightNetworkManager.Instance.StartGameOnHost();

            ulong localClientId = NetworkManager.Singleton.LocalClientId; // Get the local client's ID

            // Assign the local player using the same logic as for the host
            player = RedLightGreenLightNetworkManager.Instance.FindLocalPlayer(localClientId);

            if (player != null)
            {
                mls.LogInfo("Local player found and assigned.");
            }
            else
            {
                mls.LogError("Local player not found!");
            }

            //Test logging to check for clients:
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                mls.LogInfo($"Connected client ID: {client.ClientId}");
            }

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
            hostIsStartingGame = false;
            clientIsStartingGame = false;
            mls.LogInfo("End Green Light/Red Light");
        }

        public IEnumerator GameCycles()
        {
            gameCyclesActive = true;

            while (gameIsActive)
            {
                delay = isGreen ? UnityEngine.Random.Range(7f, 12f) : UnityEngine.Random.Range(5f, 10f);
                mls.LogInfo($"Delay until next light change: {delay} seconds");

                yield return new WaitForSecondsRealtime(delay);
                if (!gameIsActive)
                {
                    waitForMovementActive = false;
                    gameCyclesActive = false;
                    break;
                }

                if (isGreen)
                {
                    mls.LogInfo("Yellow Light activated");
                    mls.LogInfo($"Delay until next light change: {yellowDelay} seconds");
                    yield return new WaitForSecondsRealtime(yellowDelay);
                    if (!gameIsActive)
                    {
                        waitForMovementActive = false;
                        gameCyclesActive = false;
                        break;
                    }
                }

                mls.LogInfo(!isGreen ? "Green Light activated" : "Red Light activated");
                isGreen = !isGreen;

                if (!waitForMovementActive && !isGreen)
                {
                    StartCoroutine(WaitForMovement());
                }
            }
            gameCyclesActive = false;
        }

        IEnumerator WaitForMovement()
        {
            if (player == null)
            {
                mls.LogError("Player is null in WaitForMovement.");
                yield break;
            }

            mls.LogInfo($"Player object: {player.gameObject.name}");

            waitForMovementActive = true; // Prevent multiple instances of this coroutine
            currentCameraRotation = player.gameplayCamera.transform.rotation; // Reset the base case for camera rotation
            previousCameraRotation = currentCameraRotation;

            while (!isGreen && !penaltyDealt && gameIsActive)
            {
                if (player != null) // Check if the local player exists and is not in the green state
                {
                    mls.LogInfo($"player.timeSincePlayerMoving: {player.timeSincePlayerMoving}");
                    if (player.timeSincePlayerMoving < 0.1f)
                    {
                        mls.LogInfo("Player moved. Damaging Player.");
                        DamagePlayer(); // Damage the local player
                    }

                    // Check for camera movement
                    if (player.gameplayCamera != null) // Ensure gameplayCamera exists
                    {
                        currentCameraRotation = player.gameplayCamera.transform.rotation;

                        // Define a rotation threshold
                        float rotationThreshold = 2.0f; // Adjust this as needed

                        // Check if the rotation has changed beyond the threshold
                        if (UnityEngine.Quaternion.Angle(currentCameraRotation, previousCameraRotation) > rotationThreshold)
                        {
                            mls.LogInfo("Camera moved. Damaging Player.");
                            DamagePlayer(); // Damage the local player
                        }
                    }
                }

                yield return new WaitForSecondsRealtime(0.1f); // Wait before checking again
                if (!gameIsActive)
                {
                    waitForMovementActive = false;
                    gameCyclesActive = false;
                    break;
                }
            }

            // Reset the flag after the coroutine ends
            penaltyDealt = false;
            waitForMovementActive = false;
        }


        public void DamagePlayer()
        {
            if (penaltyNum < damagePenalties.Count)
            {
                player.DamagePlayer(damagePenalties[penaltyNum], true, true, CauseOfDeath.Unknown, 0, false, default);
                mls.LogInfo($"Player took {damagePenalties[penaltyNum]} damage for moving during a red light.");
                penaltyNum++;
            }
            else
            {
                mls.LogInfo("Player took too much damage. Killing player.");
                UnityEngine.Vector3 bodyVelocity = new UnityEngine.Vector3(0, 5f, 0);
                player.KillPlayer(bodyVelocity, true, CauseOfDeath.Unknown, 0, default);
            }
            penaltyDealt = true;
        }

        // Method to synchronize light change and delay
        public void SyncLightChange(bool isCurrentlyGreen, float syncDelay)
        {
            isGreen = isCurrentlyGreen;
            delay = syncDelay;

            mls.LogInfo($"Light state changed on client side. Is Green: {isGreen}, Delay: {delay}");

            if (!isGreen && !waitForMovementActive)
            {
                StartCoroutine(WaitForMovement());
            }
        }
    }
}
