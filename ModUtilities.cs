using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using pworld.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyCoolMod
{
    public static class ModUtilities
    {
        private static float _originalMovementForce = -1f;
        private static float _originalJumpImpulse = -1f;
        public static bool GetItem(this Character player, out Item item)
        {
            var currentItem = player.data?.currentItem;

            if (currentItem != null)
            {
                item = currentItem;
                return true;
            }

            item = null;
            return false;
        }
        public static void DeleteItem(this Character player)
        {

            if (!player.GetItem(out var item))
            {
                Plugin.Log.Log(BepInEx.Logging.LogLevel.Warning, $"Couldnt force consume Item (Instance is null)");
                return;
            }

            if (item == null)
            {
                Plugin.Log.Log(BepInEx.Logging.LogLevel.Warning, $"Couldnt force consume Item (GetItem = true, Instance is null)");
                return;
            }

            item.photonView.RPC("Consume", RpcTarget.All, -1);
        }
        public static void DisableItems()
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;

            foreach (var player in allPlayers)
            {
                if (player != null && player.GetItem(out _))
                {
                    player.DeleteItem();
                }
            }
        }

        #region Basic player utilities
        public static void KillPlayer(Character target)
        {
            if (!ValidateTarget(target, "KillPlayer")) return;
            try
            {
                Vector3 itemSpawnPoint = target.Center + Vector3.up * 0.2f + Vector3.forward * 0.1f;
                PerformActionOnPlayer(target, "RPCA_Die", itemSpawnPoint);
                ModGUI.ShowNotification("Player Action", $"Killed {target.characterName}", ModGUI.NotificationType.Warning);
            }
            catch (Exception e) { Plugin.Log.LogError($"KillPlayer error: {e.Message}"); }
        }

        public static void TripPlayer(Character target)
        {
            if (!ValidateTarget(target, "TripPlayer")) return;
            try
            {
                float fallDuration = 3f;
                PerformActionOnPlayer(target, "RPCA_Fall", fallDuration);
                ModGUI.ShowNotification("Player Action", $"Tripped {target.characterName}");
            }
            catch (Exception e) { Plugin.Log.LogError($"TripPlayer error: {e.Message}"); }
        }

        public static void PassOutPlayer(Character target)
        {
            if (!ValidateTarget(target, "PassOutPlayer")) return;
            try
            {
                if (target.photonView != null)
                {
                    target.photonView.RPC("RPCA_PassOut", RpcTarget.All);
                    ModGUI.ShowNotification("Player Action", $"Passed out {target.characterName}");
                }
                else Plugin.Log.LogWarning($"PassOutPlayer: {target.characterName} has no PhotonView.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"PassOutPlayer error: {ex.Message}");
            }
        }

        public static void WakeUpPlayer(Character target)
        {
            PerformActionOnPlayer(target, "RPCA_UnPassOut");
            ModGUI.ShowNotification("Player Action", $"Woke up {target.characterName}", ModGUI.NotificationType.Success);
        }

        public static void ForcePushPlayer(Character target)
        {
            PerformActionOnPlayer(target, "RPCA_AddForceToBodyPart", 0, Vector3.up * 3000f);
            ModGUI.ShowNotification("Player Action", $"Force-pushed {target.characterName}");
        }

        public static void RevivePlayer(Character target)
        {
            if (!ValidateTarget(target, "RevivePlayer")) return;
            try
            {
                Vector3 revivePos = target.Ghost != null ? target.Ghost.transform.position : target.Head;
                PerformActionOnPlayer(target, "RPCA_ReviveAtPosition", revivePos + Vector3.up, false);
                ModGUI.ShowNotification("Player Action", $"Revived {target.characterName}", ModGUI.NotificationType.Success);
            }
            catch (Exception e) { Plugin.Log.LogError($"RevivePlayer error: {e.Message}"); }
        }

        public static void ReviveSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) RevivePlayer(p); }
        public static void KillSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) KillPlayer(p); }
        public static void PassOutSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) PassOutPlayer(p); }
        public static void TripSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) TripPlayer(p); }
        #endregion

        #region Teleport / Position
        public static void TeleportAllPlayersToMe()
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer == null)
            {
                ModGUI.ShowNotification("Teleport", "Local player not found", ModGUI.NotificationType.Error);
                return;
            }

            Vector3 myPos = localPlayer.Head + Vector3.up * 1f;
            foreach (var character in PlayerManager.GetAllCharacters())
            {
                if (character != null && !character.IsLocal && character.photonView != null)
                {
                    character.photonView.RPC("WarpPlayerRPC", RpcTarget.All, myPos, true);
                }
            }
            ModGUI.ShowNotification("Teleport", "Summoned all players");
        }

        public static void TeleportPlayerToMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null && target != null && target != localPlayer)
            {
                PerformActionOnPlayer(target, "WarpPlayerRPC", localPlayer.transform.position);
                ModGUI.ShowNotification("Teleport", $"Brought {target.characterName} to you");
            }
        }
        #endregion

        #region Toggles (flags stored on Plugin)
        public static void ToggleAlwaysSprint()
        {
            Plugin.AlwaysSprintEnabled = !Plugin.AlwaysSprintEnabled;
            ModGUI.ShowNotification("Toggle", $"Always Sprint {(Plugin.AlwaysSprintEnabled ? "Enabled" : "Disabled")}");
        }
        public static void ToggleNoFallDamage()
        {
            Plugin.NoFallDamageEnabled = !Plugin.NoFallDamageEnabled;
            ModGUI.ShowNotification("Toggle", $"No Fall Damage {(Plugin.NoFallDamageEnabled ? "Enabled" : "Disabled")}");
        }
        public static void ToggleKeepItemsOnDeath()
        {
            Plugin.KeepItemsEnabled = !Plugin.KeepItemsEnabled;
            ModGUI.ShowNotification("Toggle", $"Keep Items {(Plugin.KeepItemsEnabled ? "Enabled" : "Disabled")}");
        }
        #endregion

        #region Mass operations
        public static void ReviveAllPlayers()
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            foreach (var player in allPlayers)
            {
                if (player != null && (player.data.dead || player.data.fullyPassedOut))
                {
                    RevivePlayer(player);
                }
            }
            ModGUI.ShowNotification("Global Action", "Revived all dead players", ModGUI.NotificationType.Success);
        }

        public static void KillAllPlayers()
        {
            ActionOnAllPlayers(KillPlayer);
            ModGUI.ShowNotification("Global Action", "Killed all other players", ModGUI.NotificationType.Warning);
        }

        public static void PassOutAll()
        {
            ActionOnAllPlayers(PassOutPlayer);
            ModGUI.ShowNotification("Global Action", "Passed out all other players", ModGUI.NotificationType.Warning);
        }
        #endregion

        #region Carry mechanics
        public static void ForcePlayerToCarryMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (target == null || localPlayer == null || target == localPlayer) return;
            PerformActionOnPlayer(target, "RPCA_StartCarry", localPlayer.photonView.ViewID);
            ModGUI.ShowNotification("Carry", $"Forcing {target.characterName} to carry you");
        }

        public static void ForceCarryChain()
        {
            var allPlayers = PlayerManager.GetAllCharacters()?.Where(p => p != null).ToList();
            if (allPlayers == null || allPlayers.Count < 2) return;
            BreakAllCarries();
            for (int i = 0; i < allPlayers.Count - 1; i++)
            {
                ForcePlayerToCarry(allPlayers[i], allPlayers[i + 1]);
            }
            ModGUI.ShowNotification("Carry", "Created a player carry chain!");
        }

        public static void ForceCarryCircle()
        {
            var allPlayers = PlayerManager.GetAllCharacters()?.Where(p => p != null).ToList();
            if (allPlayers == null || allPlayers.Count < 2) return;
            BreakAllCarries();
            for (int i = 0; i < allPlayers.Count; i++)
            {
                Character carrier = allPlayers[i];
                Character target = allPlayers[(i + 1) % allPlayers.Count];
                ForcePlayerToCarry(carrier, target);
            }
            ModGUI.ShowNotification("Carry", "Created a player carry circle!");
        }

        public static void BreakAllCarries()
        {
            foreach (var id in forcedCarryPairs.Keys.ToList())
            {
                var pv = PhotonView.Find(id);
                if (pv != null) pv.RPC("RPCA_Drop", RpcTarget.All);
            }
            forcedCarryPairs.Clear();
            ModGUI.ShowNotification("Carry", "Broke all forced carries");
        }
        #endregion

        #region Misc / Items / Forces
        public static void GiveItemToPlayer(Character target, string itemName)
        {
            if (!ValidateTarget(target, "GiveItemToPlayer") || string.IsNullOrEmpty(itemName)) return;
            try
            {
                int viewId = target.photonView != null ? target.photonView.ViewID : -1;
                if (viewId != -1)
                {
                    PhotonNetwork.RaiseEvent(
                        0,
                        new object[] { viewId, itemName },
                        new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
                        SendOptions.SendReliable
                    );
                    ModGUI.ShowNotification("Item", $"Requested {itemName} for {target.characterName}", ModGUI.NotificationType.Success);
                }
            }
            catch (Exception e)
            {
                ModGUI.ShowNotification("Item", "Failed to send request", ModGUI.NotificationType.Error);
            }
        }

        public static void DisarmPlayer(Character target)
        {
            if (target == null || target.refs?.items?.currentSelectedSlot == null) return;
            if (target.refs.items.currentSelectedSlot.IsSome)
            {
                try
                {
                    byte equippedSlotId = target.refs.items.currentSelectedSlot.Value;
                    Vector3 dropPosition = target.Center;
                    PerformActionOnPlayer(target, "DropItemFromSlotRPC", equippedSlotId, dropPosition);
                    ModGUI.ShowNotification("Action", $"Disarmed {target.characterName}");
                }
                catch (Exception e) { Plugin.Log.LogError($"DisarmPlayer error: {e.Message}"); }
            }
        }
        public static void MeteorStrikePlayer(Character target)
        {
            if (!ValidateTarget(target, "MeteorStrikePlayer")) return;
            try
            {
                Vector3 currentPos = target.transform.position;
                Vector3 skyPos = new Vector3(currentPos.x, currentPos.y + 200f, currentPos.z);
                PerformActionOnPlayer(target, "WarpPlayerRPC", skyPos, false);
                Vector3 downwardForce = Vector3.down * 50000f;
                PerformActionOnPlayer(target, "RPCA_AddForceToBodyPart", 0, downwardForce);
            }
            catch (Exception e) { Plugin.Log.LogError($"MeteorStrikePlayer error: {e.Message}"); }
        }
        public static void FreezePlayer(Character target)
        {
            if (!ValidateTarget(target, "FreezePlayer")) return;
            PerformActionOnPlayer(target, "RPCA_Stick", (int)BodypartType.Hip, target.transform.position, target.transform.position, (int)CharacterAfflictions.STATUSTYPE.Cold, 0.1f);
            ModGUI.ShowNotification("Action", $"Froze {target.characterName} in place");
        }

        public static void UnfreezePlayer(Character target)
        {
            if (!ValidateTarget(target, "UnfreezePlayer")) return;
            PerformActionOnPlayer(target, "RPCA_Unstick");
            ModGUI.ShowNotification("Action", $"Unfroze {target.characterName}");
        }
        #endregion

        #region Rendering / Visuals
        public static void RenderPlayerDead(Character target)
        {
            if (target == null) return;
            try
            {
                var customization = target.refs?.customization;
                if (customization != null)
                {
                    var pv = customization.GetComponent<PhotonView>();
                    if (pv != null) pv.RPC("CharacterDied", RpcTarget.All);
                }
            }
            catch (Exception e) { Plugin.Log.LogError($"RenderPlayerDead error: {e.Message}"); }
        }
        public static void RenderDead() { var p = PlayerManager.GetLocalPlayer(); if (p != null) RenderPlayerDead(p); }
        public static void RenderAllDead() => ActionOnAllPlayers(RenderPlayerDead);
        #endregion

        #region Network helpers
        private static void PerformActionOnPlayer(Character target, string rpcName, params object[] parameters)
        {
            if (target == null) return;
            if (target.photonView == null)
            {
                Plugin.Log.LogWarning($"PerformActionOnPlayer: {target.characterName} has no PhotonView (RPC {rpcName}).");
                return;
            }

            try
            {
                object[] safeParams = parameters?.Select(p => ConvertParamToSerializable(p)).ToArray() ?? new object[0];
                target.photonView.RPC(rpcName, RpcTarget.All, safeParams);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"RPC call failed ({rpcName}) for {target.characterName}: {ex.Message}");
            }
        }

        private static object ConvertParamToSerializable(object p)
        {
            if (p == null) return null;

            if (p is PhotonView pv) return pv.ViewID;
            if (p is GameObject go)
            {
                var gpv = go.GetComponent<PhotonView>();
                if (gpv != null) return gpv.ViewID;
                return go.name;
            }
            if (p is Character c && c.photonView != null) return c.photonView.ViewID;
            if (p is Photon.Realtime.Player player) return player.ActorNumber;
            return p;
        }
        #endregion

        #region Movement / Stats
        public static void ToggleInfiniteStamina()
        {
            Character.InfiniteStamina();
            ModGUI.ShowNotification("Toggle", $"Infinite Stamina {(Character.localCharacter.infiniteStam ? "Enabled" : "Disabled")}");
        }
        public static void ToggleStatusImmunity()
        {
            Character.LockStatuses();
            ModGUI.ShowNotification("Toggle", $"Status Immunity {(Character.localCharacter.statusesLocked ? "Enabled" : "Disabled")}");
        }
        public static void SetSpeedMultiplier(float multiplier)
        {
            var movement = Character.localCharacter?.refs.movement;
            if (movement == null) return;
            if (_originalMovementForce == -1f) _originalMovementForce = movement.movementForce;
            movement.movementForce = _originalMovementForce * multiplier;
        }
        public static void SetJumpMultiplier(float multiplier)
        {
            var movement = Character.localCharacter?.refs.movement;
            if (movement == null) return;
            if (_originalJumpImpulse == -1f) _originalJumpImpulse = movement.jumpImpulse;
            movement.jumpImpulse = _originalJumpImpulse * multiplier;
        }
        #endregion

        #region Spawned effects (Bees / Jellyfish)
        public static void AttackWithBees(Character target)
        {
            if (!ValidateTarget(target, "AttackWithBees")) return;
            try
            {
                string prefabName = "BeeSwarm";
                Vector3 spawnPos = target.Head;
                GameObject beeObj = null;
                if (PhotonNetwork.IsConnected)
                {
                    beeObj = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
                }
                else
                {
                    beeObj = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(prefabName));
                    if (beeObj != null) beeObj.transform.position = spawnPos;
                }

                if (beeObj == null) return;

                var beeComp = beeObj.GetComponent<BeeSwarm>();
                if (beeComp != null && beeComp.photonView != null)
                {
                    beeComp.photonView.RPC("SetBeesAngryRPC", RpcTarget.All, true);
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"AttackWithBees error: {ex.Message}"); }
        }

        public static void BeesSelf() { var lp = PlayerManager.GetLocalPlayer(); if (lp != null) AttackWithBees(lp); }
        public static void BeesAll() => ActionOnAllPlayers(AttackWithBees);
        private static void SpawnNetworkedJellyfish(Character charac)
        {
            GameObject root = new GameObject("JellyfishRoot");
            PhotonView pv = root.AddComponent<PhotonView>();
            pv.ViewID = UnityEngine.Random.Range(10000, 99999);
            pv.TransferOwnership(charac.photonView.ViewID);
            TriggerRelay relay = root.AddComponent<TriggerRelay>();
            GameObject child = new GameObject("SlipperyJellyfish");
            child.transform.SetParent(root.transform);
            var jelly = child.AddComponent<SlipperyJellyfish>();
            var col = child.AddComponent<SphereCollider>();
            col.isTrigger = true;
            root.transform.position = charac.refs.head.transform.position;
            pv.RPC("RPCA_TriggerWithTarget", RpcTarget.All, new object[]
            {
            root.transform.GetSiblingIndex(),
            charac.photonView.ViewID
            });

        }
        public static void JellyfishSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) SpawnNetworkedJellyfish(p); }
        public static void JellyfishAll() => ActionOnAllPlayers(SpawnNetworkedJellyfish);

        public static void JellyfishBomb(Character target, int count = 5)
        {
            if (!ValidateTarget(target, "JellyfishBomb")) return;
            for (int i = 0; i < count; i++) SpawnNetworkedJellyfish(target);
        }

        private static void SpawnJellyfishTrapAtPosition(Vector3 position)
        {
            try
            {
                GameObject root = new GameObject("JellyfishTrapRoot");
                root.transform.position = position;
                PhotonView pv = root.AddComponent<PhotonView>();
                pv.ViewID = UnityEngine.Random.Range(10000, 99999);
                root.AddComponent<TriggerRelay>();
                GameObject child = new GameObject("SlipperyJellyfish");
                child.transform.SetParent(root.transform, false);
                child.AddComponent<SlipperyJellyfish>();
                var col = child.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.5f;
                pv.RPC("RPCA_Trigger", RpcTarget.All);
                //UnityEngine.Destroy(root, 20f);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"SpawnJellyfishTrapAtPosition error: {e.Message}");
            }
        }

        public static void PlaceJellyfishTrap()
        {
            if (Camera.main == null) return;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 100f))
            {
                SpawnJellyfishTrapAtPosition(hit.point + Vector3.up * 0.5f);
                Plugin.Log.LogInfo($"Jellyfish trap placed at {hit.point}");
            }
        }

        public static void JellyfishRain(Character target, float radius = 10f, int count = 15)
        {
            if (!ValidateTarget(target, "JellyfishRain")) return;
            Vector3 center = target.Head + Vector3.up * 5f; // Lowered the rain start position
            for (int i = 0; i < count; i++)
            {
                Vector2 c = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 spawnPos = new Vector3(center.x + c.x, center.y, center.z + c.y);
                SpawnJellyfishTrapAtPosition(spawnPos);
            }
        }
        #endregion

        #region Crash / Master client operations
        public static void CrashPlayer(Character target)
        {
            if (!ValidateTarget(target, "CrashPlayer")) return;
            try
            {
                if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                    if (PhotonNetwork.IsMasterClient && target.photonView != null && target.photonView.Controller != null)
                    {
                        PhotonNetwork.DestroyPlayerObjects(target.photonView.Controller);
                        PhotonNetwork.OpRemoveCompleteCacheOfPlayer(1);
                        PhotonNetwork.OpRemoveCompleteCache();
                        Plugin.Log.LogInfo($"Crash command sent for player {target.characterName}.");
                    }
                    else Plugin.Log.LogWarning("CrashPlayer: not master client or target has no controller.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"CrashPlayer error: {ex.Message}");
            }
        }

        public static void CrashAll() => ActionOnAllPlayers(CrashPlayer);
        public static void CrashSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) CrashPlayer(p); }
        #endregion

        #region Game end / win
        public static void EndGame()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<Player>();
            if (player?.photonView != null) player.photonView.RPC("RPCEndGame", RpcTarget.All);
        }

        public static void ForceWinGame()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<Player>();
            if (player?.photonView != null)
            {
                int actor = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
                player.photonView.RPC("RPCEndGame_ForceWin", RpcTarget.All, actor);
            }
        }
        #endregion

        #region GUI helpers (ESP)
        private static Texture2D drawingTex;
        private static Color lastTexColour = Color.clear;
        internal static void RectFilled(float x, float y, float width, float height, Color color)
        {
            if (drawingTex == null)
            {
                drawingTex = new Texture2D(1, 1) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            }
            if (color != lastTexColour)
            {
                drawingTex.SetPixel(0, 0, color);
                drawingTex.Apply();
                lastTexColour = color;
            }
            UnityEngine.GUI.DrawTexture(new Rect(x, y, width, height), drawingTex);
        }

        public static void BoxESP()
        {
            if (Camera.main == null) return;
            foreach (Character character in Character.AllCharacters)
            {
                try
                {
                    if (character == null || character.refs?.mainRenderer == null) continue;
                    Bounds bounds = character.refs.mainRenderer.bounds;
                    Vector3 center = bounds.center;
                    Vector3 extents = bounds.extents;
                    Vector3[] corners = new Vector3[8]
                    {
                        center + new Vector3(-extents.x, -extents.y, -extents.z),
                        center + new Vector3(extents.x, -extents.y, -extents.z),
                        center + new Vector3(-extents.x, -extents.y, extents.z),
                        center + new Vector3(extents.x, -extents.y, extents.z),
                        center + new Vector3(-extents.x, extents.y, -extents.z),
                        center + new Vector3(extents.x, extents.y, -extents.z),
                        center + new Vector3(-extents.x, extents.y, extents.z),
                        center + new Vector3(extents.x, extents.y, extents.z)
                    };
                    Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                    Vector2 max = new Vector2(float.MinValue, float.MinValue);
                    bool isVisible = true;
                    foreach (Vector3 corner in corners)
                    {
                        Vector3 screenPoint = Camera.main.WorldToScreenPoint(corner);
                        if (screenPoint.z < 0f)
                        {
                            isVisible = false; break;
                        }
                        Vector2 screenPos = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                        min = Vector2.Min(min, screenPos);
                        max = Vector2.Max(max, screenPos);
                    }
                    if (!isVisible) continue;
                    float width = max.x - min.x;
                    float height = max.y - min.y;
                    Color color = character.refs.customization.PlayerColor;
                    RectFilled(min.x, min.y, width, 1f, color);
                    RectFilled(min.x, min.y, 1f, height, color);
                    RectFilled(min.x + width, min.y, 1f, height, color);
                    RectFilled(min.x, min.y + height, width, 1f, color);
                }
                catch { }
            }
        }
        #endregion

        #region Crowd-control / movement effects
        public static void TumblePlayer(Character target)
        {
            if (!ValidateTarget(target, "TumblePlayer")) return;
            if (target.photonView == null) return;
            try
            {
                float randomX = UnityEngine.Random.Range(-5000f, 5000f);
                float randomY = UnityEngine.Random.Range(2000f, 6000f);
                float randomZ = UnityEngine.Random.Range(-5000f, 5000f);
                Vector3 tumbleForce = new Vector3(randomX, randomY, randomZ);
                target.photonView.RPC("RPCA_Fall", RpcTarget.All, 3f);
                target.photonView.RPC("RPCA_AddForceToBodyPart", RpcTarget.All, 0, tumbleForce);
            }
            catch (Exception e) { Plugin.Log.LogError($"TumblePlayer error: {e.Message}"); }
        }

        public static void ForcePassOutPlayer(Character target)
        {
            if (!ValidateTarget(target, "ForcePassOutPlayer")) return;
            target.photonView.RPC("RPCA_PassOut", RpcTarget.All);
        }

        public static void StunLockPlayer(Character target)
        {
            if (!ValidateTarget(target, "StunLockPlayer")) return;
            for (int i = 0; i < 10; i++)
            {
                try { target.photonView.RPC("JumpRpc", RpcTarget.All, false); } catch { }
            }
        }

        public static void FlingPlayer(Character target)
        {
            if (!ValidateTarget(target, "FlingPlayer")) return;
            try
            {
                target.photonView.RPC("RPCA_Fall", RpcTarget.All, 3f);
                Vector3 flingForce = (Vector3.up * 1000000f) + ((Camera.main != null ? Camera.main.transform.forward : Vector3.forward) * 4000f);
                target.photonView.RPC("RPCA_AddForceToBodyPart", RpcTarget.All, 0, flingForce);
            }
            catch (Exception e) { Plugin.Log.LogError($"FlingPlayer error: {e.Message}"); }
        }

        public static void StunLockAll() => ActionOnAllPlayers(StunLockPlayer);
        public static void FlingAll() => ActionOnAllPlayers(FlingPlayer);
        #endregion

        private static Dictionary<int, int> forcedCarryPairs = new Dictionary<int, int>();
        public static void ForcePlayerToCarry(Character carrier, Character target)
        {
            if (carrier == null || target == null || carrier == target) return;
            if (carrier.photonView == null || target.photonView == null) return;
            forcedCarryPairs[carrier.photonView.ViewID] = target.photonView.ViewID;
            carrier.photonView.RPC("RPCA_StartCarry", RpcTarget.All, target.photonView.ViewID);
        }

        public static void MakePlayerCarryPlayer(Character carrier, Character targetToCarry)
        {
            if (carrier == null || targetToCarry == null || carrier == targetToCarry) return;
            PerformActionOnPlayer(carrier, "RPCA_StartCarry", targetToCarry.photonView.ViewID);
        }

        public static void CarryAndFling(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (target == null || localPlayer == null) return;
            PerformActionOnPlayer(localPlayer, "RPCA_StartCarry", target.photonView.ViewID);
            FlingPlayer(localPlayer);
        }

        #region Player chain helper
        public static IEnumerator CreatePlayerChainRoutine()
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null || allPlayers.Count < 2) yield break;

            var playerList = new List<Character>(allPlayers);
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null) playerList.Remove(localPlayer);

            if (playerList.Count < 2) yield break;

            Plugin.Log.LogInfo("Starting player chain...");

            for (int i = 0; i < playerList.Count - 1; i++)
            {
                Character carrier = playerList[i];
                Character targetToCarry = playerList[i + 1];

                if (carrier != null && targetToCarry != null)
                {
                    MakePlayerCarryPlayer(carrier, targetToCarry);
                    yield return new WaitForSeconds(0.2f);
                }
            }

            Plugin.Log.LogInfo("Player chain complete.");
        }
        #endregion

        #region RPC dumping
        public static void DumpRPCsToFile()
        {
            Plugin.Log.LogInfo("Starting RPC dump process...");
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("--- RPC Dump Generated by Peak Mod ---");
                sb.AppendLine($"--- Dumped on: {DateTime.Now} ---");
                sb.AppendLine();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type type in assembly.GetTypes())
                        {
                            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                            {
                                if (Attribute.IsDefined(method, typeof(PunRPC)))
                                {
                                    sb.AppendLine($"{type.FullName}.{method.Name}");
                                }
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException) { continue; }
                }

                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                string filePath = Path.Combine(downloadsPath, "Game_RPC_Dump.txt");
                File.WriteAllText(filePath, sb.ToString());
                Plugin.Log.LogInfo($"SUCCESS: RPC dump complete. File saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"ERROR: Failed to dump RPCs. Exception: {ex.Message}");
            }
        }
        #endregion

        #region Helpers
        private static bool ValidateTarget(Character target, string ctx = null)
        {
            if (target == null)
            {
                if (!string.IsNullOrEmpty(ctx)) Plugin.Log.LogWarning($"{ctx} failed: target was null.");
                return false;
            }
            return true;
        }

        public static void ActionOnAllPlayers(Action<Character> action)
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            Character localPlayer = PlayerManager.GetLocalPlayer();
            foreach (var player in allPlayers)
            {
                if (localPlayer != null && player == localPlayer) continue;
                try { action(player); } catch { }
            }
        }
        #endregion

        public static void MakePlayerGiant(Character target) { if (target == null) return; target.transform.localScale = Vector3.one * 3f; }
        public static void ResetPlayerSize(Character target) { if (target == null) return; target.transform.localScale = Vector3.one; }
        public static void MakeAllPlayersTiny() => ActionOnAllPlayers(p => p.transform.localScale = Vector3.one * 0.3f);
        public static void ResetAllPlayerSizes() => ActionOnAllPlayers(p => p.transform.localScale = Vector3.one);

        public static void PoofPlayer(Character target) { if (target == null) return; try { target.PlayPoofVFX(target.Head); } catch { } }

        public static void ApplyForceAtPosition(Character target, Vector3 force, Vector3 position, float radius = 1f)
        {
            if (!ValidateTarget(target, "ApplyForceAtPosition")) return;
            PerformActionOnPlayer(target, "RPCA_AddForceAtPosition", force, position, radius);
        }

        public static void MoraleBoostPlayer(Character target, float staminaToAdd = 0.5f)
        {
            if (!ValidateTarget(target, "MoraleBoostPlayer")) return;
            PerformActionOnPlayer(target, "MoraleBoost", staminaToAdd, 1);
        }

        private static float _hue1 = 0f;
        private static float _hue2 = 0f;
        private static float _hue3 = 0f;

        [PunRPC]
        public static void ChangeColour(Vector3 rgb, GameObject target)
        {
            if (target == null) return;
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetColor("_Color", new Color(rgb.x, rgb.y, rgb.z));
            }
        }

        public static void ApplyRGBRainbow(Character target)
        {
            if (target == null || target.photonView == null) return;

            _hue1 += Time.deltaTime * 0.5f;
            if (_hue1 > 1f) _hue1 = 0f;

            Color c = Color.HSVToRGB(_hue1, 1f, 1f);
            target.photonView.RPC("ChangeColour", RpcTarget.AllBuffered, new object[] { new Vector3(c.r, c.g, c.b), target.gameObject });
        }

        public static void ApplyRGBStrobe(Character target)
        {
            if (target == null || target.photonView == null) return;

            _hue2 += Time.deltaTime * 4f;
            if (_hue2 > 1f) _hue2 = 0f;

            Color c = Color.HSVToRGB(_hue2, 1f, 1f);
            target.photonView.RPC("ChangeColour", RpcTarget.AllBuffered, new object[] { new Vector3(c.r, c.g, c.b), target.gameObject });
        }

        public static void ApplyRGBPulse(Character target)
        {
            if (target == null || target.photonView == null) return;

            _hue3 += Time.deltaTime * 0.5f;
            if (_hue3 > 1f) _hue3 = 0f;

            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
            Color c = Color.HSVToRGB(_hue3, 1f, pulse);
            target.photonView.RPC("ChangeColour", RpcTarget.AllBuffered, new object[] { new Vector3(c.r, c.g, c.b), target.gameObject });
        }

        public static void ApplyRGBDual(Character target, Color colorA, Color colorB)
        {
            if (target == null || target.photonView == null) return;

            float t = (Mathf.Sin(Time.time) + 1f) / 2f; // smooth blend
            Color c = Color.Lerp(colorA, colorB, t);
            target.photonView.RPC("ChangeColour", RpcTarget.AllBuffered, new object[] { new Vector3(c.r, c.g, c.b), target.gameObject });
        }

        private static float _nextFlashTime = 0f;
        private static Color _flashColor = Color.white;

        public static void ApplyRGBFlash(Character target)
        {
            if (target == null || target.photonView == null) return;

            if (Time.time > _nextFlashTime)
            {
                _flashColor = UnityEngine.Random.ColorHSV();
                _nextFlashTime = Time.time + 0.2f;
            }

            target.photonView.RPC("ChangeColour", RpcTarget.AllBuffered, new object[] { new Vector3(_flashColor.r, _flashColor.g, _flashColor.b), target.gameObject });
        }
        
        public static void ExplodePlayer(Character target)
        {
            if (!ValidateTarget(target, "ExplodePlayer")) return;

            try
            {
                string prefabName = "0_Items/Dynamite";
                Vector3 spawnPos = target.Head;
                GameObject dynamiteObj = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
                if (dynamiteObj != null)
                {
                    var pv = dynamiteObj.GetComponent<PhotonView>();
                    if (pv != null)
                    {
                        pv.RPC("RPC_Explode", RpcTarget.All);
                        PhotonNetwork.Destroy(dynamiteObj);
                        ModGUI.ShowNotification("Action", $"Exploded {target.characterName}", ModGUI.NotificationType.Warning);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"ExplodePlayer error: {e.Message}");
            }
        }
        public class MagicBean : MonoBehaviour
        {
            [PunRPC]
            void GrowVineRPC(Vector3 spawnPos)
            {
                try
                {
                    Instantiate(Resources.Load<GameObject>("0_Items/VinePrefab"), spawnPos, Quaternion.identity);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to instantiate vine prefab: {e.Message}");
                }
            }
        }
        public static void MagicBeanPlayer(Character target)
        {
            if (!ValidateTarget(target, "MagicBeanPlayer")) return;

            try
            {
                Vector3 spawnPos = target.transform.position;
                GameObject root = new GameObject("MagicBeanRoot");
                root.transform.position = spawnPos;
                PhotonView pv = root.AddComponent<PhotonView>();
                pv.ViewID = UnityEngine.Random.Range(10000, 99999);
                root.AddComponent<MagicBean>();
                pv.RPC("GrowVineRPC", RpcTarget.All, spawnPos);
                ModGUI.ShowNotification("Action", $"Grew a vine on {target.characterName}");
                //Destroy(root, 2f);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"MagicBeanPlayer error: {e.Message}");
            }
        }
        public static void MarkPlayerWithFlare(Character target)
        {
            PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
            if (!ValidateTarget(target, "MarkPlayerWithFlare")) return;
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    try
                    {

                        GameObject root = new GameObject("FlareRoot");
                        root.transform.position = target.Center;
                        PhotonView pv = root.AddComponent<PhotonView>();
                        pv.ViewID = UnityEngine.Random.Range(10000, 99999);
                        root.AddComponent<Flare>();
                        pv.RPC("SetFlareLitRPC", RpcTarget.All, true);
                        pv.RPC("TriggerHelicopter", RpcTarget.MasterClient);
                        ModGUI.ShowNotification("Action", $"Marked {target.characterName} with a flare");
                        //Destroy(root, 60f);
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"MarkPlayerWithFlare error: {e.Message}");
                    }
                }
            }
        }
        public static void CactusPlayer(Character target)
        {
            if (!ValidateTarget(target, "CactusPlayer")) return;
            if (target.photonView == null) return;

            try
            {
                GameObject root = new GameObject("CactusRoot");
                root.transform.position = target.Center + target.transform.forward * 0.5f;
                PhotonView pv = root.AddComponent<PhotonView>();
                pv.ViewID = UnityEngine.Random.Range(10000, 99999);
                root.AddComponent<CactusBall>();
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = false;
                col.radius = 0.5f;
                pv.RPC("RPC_StickToCharacterRemote", RpcTarget.All, target.photonView.ViewID);
                ModGUI.ShowNotification("Action", $"Stuck a cactus to {target.characterName}");
                //Destroy(root, 15f);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"CactusPlayer error: {e.Message}");
            }
        }
        public static void ExplodeAllPlayers() => ActionOnAllPlayers(ExplodePlayer);
        public static void ExplodeSelf()
        {
            var p = PlayerManager.GetLocalPlayer();
            if (p != null) ExplodePlayer(p);
        }
        public static void MagicBeanAllPlayers() => ActionOnAllPlayers(MagicBeanPlayer);
        public static void MagicBeanSelf()
        {
            var p = PlayerManager.GetLocalPlayer();
            if (p != null) MagicBeanPlayer(p);
        }
        public static void CactusAllPlayers() => ActionOnAllPlayers(CactusPlayer);
        public static void CactusSelf()
        {
            var p = PlayerManager.GetLocalPlayer();
            if (p != null) CactusPlayer(p);
        }
        private static bool _airJumpAvailable = true;
        private static bool _wasGroundedLastFrame = true;
        public static void HandleAirJump()
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer == null || localPlayer.data == null)
            {
                return;
            }
            bool isGrounded = localPlayer.data.isGrounded;
            if (isGrounded && !_wasGroundedLastFrame)
            {
                _airJumpAvailable = true;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!isGrounded && _airJumpAvailable)
                {
                    _airJumpAvailable = false;
                    float airJumpForce = 10;
                    PerformActionOnPlayer(localPlayer, "RPCA_AddForceToBodyPart", 0, Vector3.up * airJumpForce);
                    ModGUI.ShowNotification("Movement", "Air Jumped!", ModGUI.NotificationType.Success);
                }
            }
            _wasGroundedLastFrame = isGrounded;
        }
    }
}