using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace RedLightGreenLight
{
    public class RedLightGreenLightNetworkManager : NetworkBehaviour
    {
        private static RedLightGreenLightNetworkManager _instance;
        private PlayerControllerB player;

        public static RedLightGreenLightNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("RedLightGreenLightNetworkManager");
                    _instance = obj.AddComponent<RedLightGreenLightNetworkManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        // This runs on all clients when the game begins
        [ClientRpc]
        public void BeginGameClientRpc()
        {
            RedLightGreenLight.mls.LogInfo("Game started on client side!");
            //RedLightGreenLight.isGreen = true;
            //RedLightGreenLight.gameIsActive = true;

            // Optionally, start game cycles or other relevant logic for clients here.
            RedLightGreenLight.Instance.BeginGame();
        }

        // Synchronize the light state and delay across clients
        [ClientRpc]
        public void SyncLightChangeClientRpc(bool isCurrentlyGreen, float syncDelay)
        {
            RedLightGreenLight.mls.LogInfo($"Received light sync on client. Light is green: {isCurrentlyGreen}, Delay: {syncDelay}");
            
            RedLightGreenLight.isGreen = isCurrentlyGreen;
            RedLightGreenLight.Instance.SyncLightChange(isCurrentlyGreen, syncDelay);
        }

        // Method to be called from host to start the game
        public void StartGameOnHost()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                RedLightGreenLight.mls.LogInfo("Starting game on host!");
                /*ulong localClientId = NetworkManager.Singleton.LocalClientId; // Get the local client's ID

                // Assign the host's player by finding the player with the local client ID
                player = FindLocalPlayer(localClientId);

                if (player == null)
                {
                    RedLightGreenLight.mls.LogError("Host player not found!");
                    return;
                }

                RedLightGreenLight.isGreen = true;
                RedLightGreenLight.gameIsActive = true;*/

                BeginGameClientRpc(); // Inform clients to start the game
                //RedLightGreenLight.instance.StartCoroutine(RedLightGreenLight.instance.GameCycles());
            }
        }

        public PlayerControllerB FindLocalPlayer(ulong localClientId)
        {
            PlayerControllerB[] allPlayers = FindObjectsOfType<PlayerControllerB>();

            foreach (PlayerControllerB playerController in allPlayers)
            {
                if (playerController.actualClientId == localClientId) // Compare the local client's ID
                {
                    return playerController;
                }
            }

            RedLightGreenLight.mls.LogError("Local player not found!");
            return null; // Handle cases where no local player is found
        }
    }
}
