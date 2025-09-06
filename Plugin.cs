using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI.Extensions;
using MyCoolMod.Patches;

namespace MyCoolMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private bool _rpcReceiverInjected = false;
        public static bool GodModeEnabled { get; set; } = false;
        public static float DamageMultiplier { get; set; } = 1.0f;
        public static bool EspEnabled { get; set; } = false;
        public static bool BoxEspEnabled { get; set; } = false;
        public static bool TracersEnabled { get; set; } = false;
        public static bool InfiniteStaminaEnabled { get; set; } = false;
        public static bool StatusImmunityEnabled { get; set; } = false;
        public static float SpeedMultiplier { get; set; } = 1.0f;
        public static float JumpMultiplier { get; set; } = 1.0f;

        public static ManualLogSource Log;
        internal static bool IsGuiVisible = true;

        private void Update()
        {
            if (!_rpcReceiverInjected && Character.localCharacter != null)
            {
                Character.localCharacter.gameObject.AddComponent<RpcReceiver>();
                _rpcReceiverInjected = true;
                Log.LogInfo("RPC Receiver injected successfully!");
            }
        }

        private void Awake()
        {
            Log = Logger;
            HP.ApplyHarmonyPatches();
            Log.LogInfo($"Harmony patches applied via Menu class!");
            var guiObject = new GameObject("MyCoolModGUI");
            DontDestroyOnLoad(guiObject);
            guiObject.AddComponent<ModGUI>();
            Log.LogInfo($"GUI Initialized!");
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void OnDestroy()
        {
            HP.RemoveHarmonyPatches();
            Log.LogInfo($"Harmony patches removed.");
        }
    }
}