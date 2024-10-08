using System;
using HarmonyLib;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
//using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine.SceneManagement;


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
        public static RedLightGreenLight Instance
        {
            get
            {
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
            mls = Logger;
            harmony.PatchAll();
            mls.LogInfo($"RedLightGreenLight v{modVersion} loaded!");
        }
    }
}