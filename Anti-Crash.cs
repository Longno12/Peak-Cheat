using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiCrash.Miscellaneous
{
    internal static class AntiCrash
    {
        public static bool Enabled = true;

        private static readonly Dictionary<string, float> rpcCooldowns = new Dictionary<string, float>
        {
            { "Dynamite.RPC_Explode", 0.5f },
            { "Constructable.CreatePrefabRPC", 0.5f },
            { "Campfire.SetFireWoodCount", 0.05f }
        };

        private static readonly Dictionary<string, float> lastCallTimes = new Dictionary<string, float>();

        public static bool Validate(string rpcName)
        {
            if (!Enabled) return true;

            if (!rpcCooldowns.TryGetValue(rpcName, out float cooldown)) return true;

            float now = Time.realtimeSinceStartup;

            if (lastCallTimes.TryGetValue(rpcName, out float lastTime))
            {
                if (now - lastTime < cooldown)
                {
                    UnityEngine.Debug.LogWarning($"[AntiCrash] Blocked RPC spam: {rpcName}");
                    return false;
                }
            }
            lastCallTimes[rpcName] = now;
            return true;
        }

        public static void ProtectRPC(string rpcName, float cooldown)
        {
            rpcCooldowns[rpcName] = cooldown;
            UnityEngine.Debug.Log($"[AntiCrash] Now protecting {rpcName} with {cooldown}s cooldown.");
        }
    }
}
