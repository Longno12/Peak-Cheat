using HarmonyLib;
using MyCoolMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Character;

namespace ClassLibrary1
{
    internal class Patches
    {
        [HarmonyPatch(typeof(CharacterMovement), "GetMovementForce")]
        [HarmonyPostfix]
        public static void SpeedHackPatch(ref float __result)
        {
            if (Plugin.SpeedMultiplier > 1.0f)
            {
                __result *= Plugin.SpeedMultiplier;
            }
        }

        [HarmonyPatch(typeof(CharacterMovement), "JumpRpc")]
        [HarmonyPrefix]
        public static void JumpHackPatch(CharacterMovement __instance)
        {
            if (Plugin.JumpMultiplier > 1.0f)
            {
                __instance.jumpImpulse *= Plugin.JumpMultiplier;
                __instance.StartCoroutine(ResetJumpImpulse(__instance));
            }
        }

        private static System.Collections.IEnumerator ResetJumpImpulse(CharacterMovement movementInstance)
        {
            yield return new WaitForEndOfFrame();
            if (Plugin.JumpMultiplier > 1.0f)
            {
                movementInstance.jumpImpulse /= Plugin.JumpMultiplier;
            }
        }

        [HarmonyPatch(typeof(CharacterMovement), "CheckFallDamage")]
        [HarmonyPrefix]
        public static bool NoFallDamagePatch()
        {
            if (Plugin.NoFallDamageEnabled)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), "UseStamina")]
        [HarmonyPrefix]
        public static bool AlwaysSprintPatch(Character __instance, float usage)
        {
            if (Plugin.AlwaysSprintEnabled && __instance.data.isSprinting)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch]
        public static class SprintPatches
        {
            private static MethodInfo checkSprintMethod;

            [HarmonyPrepare]
            public static bool Prepare()
            {
                checkSprintMethod = AccessTools.Method(typeof(Character), "CheckSprint");
                if (checkSprintMethod == null)
                {
                    Plugin.Log.LogError("Could not find internal method Character.CheckSprint(). The Always Sprint patch will be disabled.");
                    return false;
                }

                Plugin.Log.LogInfo("Successfully found internal method Character.CheckSprint() for sprint patch.");
                return true;
            }

            [HarmonyPatch(typeof(Character), "FixedUpdate")]
            [HarmonyPostfix]
            public static void ForceSprintPatch(Character __instance)
            {
                if (Plugin.AlwaysSprintEnabled && __instance.IsLocal)
                {
                    bool canSprint = (bool)checkSprintMethod.Invoke(__instance, null);
                    if (canSprint)
                    {
                        __instance.data.isSprinting = true;
                        __instance.data.sinceUseStamina = 10f;
                    }
                }
            }

            [HarmonyPatch(typeof(Character), "UseStamina")]
            [HarmonyPrefix]
            public static bool NoStaminaForSprintPatch(Character __instance)
            {
                if (Plugin.AlwaysSprintEnabled && __instance.data.isSprinting)
                {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(CharacterItems), "DropAllItems")]
        public static class KeepItemsPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (Plugin.KeepItemsEnabled)
                {
                    Plugin.Log.LogInfo("KeepItems is enabled, preventing items from dropping on death.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameOverHandler), "BeginIslandLoadRPC")]
        public class SoftLockPatch
        {
            private static bool Prefix()
            {
                return !NoSL;
            }
        }

        [HarmonyPatch(typeof(Character), "RPCA_PassOut")]
        public class PassOutPatch
        {
            private static bool Prefix()
            {
                return !NoP;
            }
        }

        [HarmonyPatch(typeof(Character), "PassOut")]
        public class PassOutPatch1
        {
            private static bool Prefix()
            {
                return !NoP;
            }
        }

        [HarmonyPatch(typeof(Character), "RPCA_Die")]
        public class DiePatch1
        {
            private static bool Prefix()
            {
                return !NoD;
            }
        }

        [HarmonyPatch(typeof(Character), "Die")]
        public class DiePatch2
        {
            private static bool Prefix()
            {
                return !NoD;
            }
        }

        [HarmonyPatch(typeof(Character), "StartPassedOutOnTheBeach")]
        public static class PreventBeachPassout
        {
            [HarmonyPostfix]
            static void WakeUp()
            {
                var localPlayer = Character.localCharacter;
                if (localPlayer != null && localPlayer.data is CharacterData characterData)
                {
                    characterData.passedOut = false;
                    characterData.fullyPassedOut = false;
                    characterData.passOutValue = 0f;
                    characterData.passedOutOnTheBeach = 0f;
                    characterData.lastPassedOut = float.MinValue;
                    localPlayer.RPCA_UnFall();
                }
            }
        }

        public static bool NoS;
        public static bool NoP;
        public static bool NoR;
        public static bool NoD;
        public static bool NoSL;
        public static bool NoDstry;
    }
}
