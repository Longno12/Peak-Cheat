using HarmonyLib;
using MyCoolMod;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static Character;

namespace ClassLibrary1
{
    internal class Patches
    {
        public static bool NoS;
        public static bool NoP;
        public static bool NoR;
        public static bool NoD;
        public static bool NoSL;
        public static bool NoDstry;
        public static bool LowGravityEnabled;
        public static bool TimeScaleEnabled;

        [HarmonyPatch(typeof(CharacterMovement), "GetMovementForce")]
        [HarmonyPostfix]
        public static void SpeedHackPatch(ref float __result)
        {
            if (Plugin.SpeedMultiplier > 1.0f)
            {
                __result *= Plugin.SpeedMultiplier;
                Plugin.Log.LogInfo($"Applied speed multiplier: {Plugin.SpeedMultiplier}");
            }
        }

        [HarmonyPatch(typeof(CharacterMovement), "JumpRpc")]
        [HarmonyPrefix]
        public static void JumpHackPatch(CharacterMovement __instance)
        {
            if (Plugin.JumpMultiplier > 1.0f)
            {
                float originalJump = __instance.jumpImpulse;
                __instance.jumpImpulse *= Plugin.JumpMultiplier;
                __instance.StartCoroutine(ResetJumpImpulse(__instance, originalJump));
                Plugin.Log.LogInfo($"Applied jump multiplier: {Plugin.JumpMultiplier}");
            }
        }

        private static IEnumerator ResetJumpImpulse(CharacterMovement movementInstance, float originalJump)
        {
            yield return new WaitForEndOfFrame();
            movementInstance.jumpImpulse = originalJump;
        }

        [HarmonyPatch(typeof(CharacterMovement), "CheckFallDamage")]
        [HarmonyPrefix]
        public static bool NoFallDamagePatch()
        {
            if (Plugin.NoFallDamageEnabled)
            {
                Plugin.Log.LogInfo("No fall damage applied.");
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), "UseStamina")]
        [HarmonyPrefix]
        public static bool AlwaysSprintPatch(Character __instance, float usage)
        {
            if ((Plugin.AlwaysSprintEnabled || NoS) && __instance.data.isSprinting)
            {
                Plugin.Log.LogInfo("Prevented stamina usage for sprint.");
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
                if ((Plugin.AlwaysSprintEnabled || NoS) && __instance.IsLocal)
                {
                    bool canSprint = (bool)checkSprintMethod.Invoke(__instance, null);
                    if (canSprint)
                    {
                        __instance.data.isSprinting = true;
                        __instance.data.sinceUseStamina = 10f;
                    }
                }
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
        public static class SoftLockPatch
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (NoSL)
                {
                    Plugin.Log.LogInfo("Prevented soft lock via BeginIslandLoadRPC.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "RPCA_PassOut")]
        public static class PassOutPatch
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (NoP)
                {
                    Plugin.Log.LogInfo("Prevented pass out via RPCA_PassOut.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "PassOut")]
        public static class PassOutPatch1
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (NoP)
                {
                    Plugin.Log.LogInfo("Prevented pass out via PassOut.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "RPCA_Die")]
        public static class DiePatch1
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (NoD)
                {
                    Plugin.Log.LogInfo("Prevented death via RPCA_Die.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "Die")]
        public static class DiePatch2
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                if (NoD)
                {
                    Plugin.Log.LogInfo("Prevented death via Die.");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Character), "StartPassedOutOnTheBeach")]
        public static class PreventBeachPassout
        {
            [HarmonyPostfix]
            static void WakeUp(Character __instance)
            {
                if (__instance.IsLocal)
                {
                    __instance.data.passedOut = false;
                    __instance.data.fullyPassedOut = false;
                    __instance.data.passOutValue = 0f;
                    __instance.data.passedOutOnTheBeach = 0f;
                    __instance.data.lastPassedOut = float.MinValue;
                    __instance.photonView.RPC("RPCA_UnFall", RpcTarget.All);
                    Plugin.Log.LogInfo("Prevented beach passout and cleared fall status.");
                }
            }
        }

        [HarmonyPatch(typeof(CharacterAfflictions), "AddStatus")]
        [HarmonyPrefix]
        public static bool StatusEffectPatch(CharacterAfflictions __instance, CharacterAfflictions.STATUSTYPE statusType, float amount, bool sync)
        {
            if (Plugin.StatusLockEnabled)
            {
                Plugin.Log.LogInfo($"Blocked status effect {statusType} due to StatusLockEnabled.");
                return false;
            }
            if (sync)
            {
                __instance.GetComponent<PhotonView>().RPC("SyncStatusesRPC", RpcTarget.All, statusType, amount, true);
            }
            return true;
        }

        [HarmonyPatch(typeof(Character), "FixedUpdate")]
        [HarmonyPostfix]
        public static void GravityPatch(Character __instance)
        {
            if (LowGravityEnabled || NoR)
            {
                float gravityMultiplier = LowGravityEnabled ? 0.2f : (NoR ? -1f : 1f);
                Vector3 gravityForce = Vector3.up * (Physics.gravity.y * (1f - gravityMultiplier));
                __instance.photonView.RPC("RPCA_ApplyGravityForce", RpcTarget.All, gravityForce);
            }
        }

        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        public static void InvisibilityPatch(Character __instance)
        {
            if (NoDstry && __instance.IsLocal)
            {
                __instance.photonView.RPC("RPCA_SetVisibility", RpcTarget.All, false);
                Plugin.Log.LogInfo($"Set {__instance.characterName} to invisible.");
            }
        }

        [HarmonyPatch(typeof(Character), "Update")]
        [HarmonyPostfix]
        public static void TimeScalePatch(Character __instance)
        {
            if (TimeScaleEnabled && PhotonNetwork.IsMasterClient && __instance.IsLocal)
            {
                float targetTimeScale = Plugin.TimeScaleValue;
                __instance.photonView.RPC("RPCA_SetTimeScale", RpcTarget.All, targetTimeScale);
            }
        }

        [HarmonyPatch(typeof(Character), "WarpPlayer")]
        [HarmonyPostfix]
        public static void CustomPoofPatch(Character __instance, Vector3 position, bool poof)
        {
            if (poof && Plugin.CustomPoofEnabled)
            {
                __instance.photonView.RPC("RPCA_CustomPoofVFX", RpcTarget.All, Plugin.CustomPoofColor, Plugin.CustomPoofScale);
                Plugin.Log.LogInfo($"Applied custom poof VFX for {__instance.characterName}.");
            }
        }

        [HarmonyPatch(typeof(CharacterItems), "EquipSlotRpc")]
        [HarmonyPostfix]
        public static void ItemSpawnPatch(CharacterItems __instance, byte slot, int viewID)
        {
            if (Plugin.ItemSpawnEnabled)
            {
                PhotonView itemView = PhotonView.Find(viewID);
                if (itemView != null)
                {
                    Plugin.Log.LogInfo($"Equipped item with ViewID {viewID} to slot {slot}.");
                }
            }
        }

        [HarmonyPatch(typeof(Item), nameof(Item.CarryWeight), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CarryWeight_Getter(ref int __result)
        {
            __result = 0;
            return false;
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("Update", MethodType.Normal)]
        [HarmonyPrefix]
        public static void Update_Prefix(Item __instance)
        {
            FieldInfo destroyField = typeof(Item).GetField("destroyTick", BindingFlags.Instance | BindingFlags.NonPublic);
            Type itemStateType = __instance.GetType().Assembly.GetType("ItemState") ?? throw new Exception("ItemState enum not found");
            object groundState = Enum.Parse(itemStateType, "Ground");
            if (__instance.itemState.Equals(groundState))
            {
                destroyField.SetValue(__instance, 0f);
            }
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("ContinueUsePrimary", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool ContinueUsePrimary_Prefix(Item __instance)
        {
            if (__instance.isUsingPrimary)
            {
                MethodInfo finishMethod = typeof(Item).GetMethod("FinishCastPrimary", BindingFlags.Instance | BindingFlags.NonPublic);
                finishMethod.Invoke(__instance, null);
            }
            return false;
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("CanUsePrimary", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool CanUsePrimary_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("CanUseSecondary", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool CanUseSecondary_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("RemoveFromActiveList", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool RemoveFromActiveList_Prefix()
        {
            return false;
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch("ConsumeDelayed", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool ConsumeDelayed_Prefix(Item __instance)
        {
            __instance.Consume(__instance.holderCharacter != null ? __instance.holderCharacter.photonView.ViewID : -1);
            return false;
        }

        [HarmonyPatch(typeof(Item), nameof(Item.IsInteractible))]
        [HarmonyPrefix]
        public static bool IsInteractible_Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Item), nameof(Item.Interact))]
        [HarmonyPrefix]
        public static void Interact_Prefix(Item __instance, Character interactor)
        {
            __instance.Consume(interactor.photonView.ViewID);
        }
    }
}