using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MyCoolMod
{
    public static class ModUtilities
    {
        private static float _originalMovementForce = -1f;
        private static float _originalJumpImpulse = -1f;

        #region Basic player utilities
        public static void KillPlayer(Character target)
        {
            if (!ValidateTarget(target, "KillPlayer")) return;
            try
            {
                Vector3 itemSpawnPoint = target.Center + Vector3.up * 0.2f + Vector3.forward * 0.1f;
                PerformActionOnPlayer(target, "RPCA_Die", itemSpawnPoint);
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
                    Plugin.Log.LogInfo($"Sent PassOut RPC to {target.characterName}.");
                }
                else Plugin.Log.LogWarning($"PassOutPlayer: {target.characterName} has no PhotonView.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"PassOutPlayer error: {ex.Message}");
            }
        }

        public static void WakeUpPlayer(Character target) => PerformActionOnPlayer(target, "RPCA_UnPassOut");
        public static void ForcePushPlayer(Character target) => PerformActionOnPlayer(target, "RPCA_AddForceToBodyPart", 0, Vector3.up * 3000f);

        public static void RevivePlayer(Character target)
        {
            if (!ValidateTarget(target, "RevivePlayer")) return;
            try
            {
                Vector3 revivePos = target.Ghost != null ? target.Ghost.transform.position : target.Head;
                PerformActionOnPlayer(target, "RPCA_ReviveAtPosition", revivePos + Vector3.up, false);
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
                Plugin.Log.LogWarning("Cannot TeleportAllPlayersToMe, local character is not available.");
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
        }

        public static void TeleportPlayerToMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null && target != null && target != localPlayer)
                PerformActionOnPlayer(target, "WarpPlayerRPC", localPlayer.transform.position);
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
        public static void ToggleInfiniteStamina() => Character.InfiniteStamina();
        public static void ToggleStatusImmunity() => Character.LockStatuses();
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

        public static void SpawnNetworkedJellyfish(Character charac)
        {
            if (!ValidateTarget(charac, "SpawnNetworkedJellyfish")) return;

            try
            {
                string prefab = "SlipperyJellyfish";
                Vector3 pos = charac.refs.head.transform.position;

                if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Instantiate(prefab, pos, Quaternion.identity);
                }
                else
                {
                    var go = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(prefab));
                    if (go != null) go.transform.position = pos;
                }
            }
            catch (Exception e) { Plugin.Log.LogError($"SpawnNetworkedJellyfish error: {e.Message}"); }
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
                string prefab = "SlipperyJellyfish";
                if (PhotonNetwork.IsConnected)
                {
                    PhotonNetwork.Instantiate(prefab, position, Quaternion.identity);
                }
                else
                {
                    var go = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(prefab));
                    if (go != null) go.transform.position = position;
                }
            }
            catch (Exception e) { Plugin.Log.LogError($"SpawnJellyfishTrapAtPosition: {e.Message}"); }
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
            Vector3 center = target.Head + Vector3.up * 30f;
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
                catch {}
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

        #region Toggles (flags stored on Plugin)
        public static void ToggleAlwaysSprint() { Plugin.AlwaysSprintEnabled = !Plugin.AlwaysSprintEnabled; Plugin.Log.LogInfo($"AlwaysSprint set to {Plugin.AlwaysSprintEnabled}"); }
        public static void ToggleNoFallDamage() { Plugin.NoFallDamageEnabled = !Plugin.NoFallDamageEnabled; Plugin.Log.LogInfo($"NoFallDamage set to {Plugin.NoFallDamageEnabled}"); }
        public static void ToggleKeepItemsOnDeath() { Plugin.KeepItemsEnabled = !Plugin.KeepItemsEnabled; Plugin.Log.LogInfo($"KeepItemsOnDeath set to {Plugin.KeepItemsEnabled}"); }
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
        }

        public static void KillAllPlayers() => ActionOnAllPlayers(KillPlayer);
        public static void PassOutAll() => ActionOnAllPlayers(PassOutPlayer);
        #endregion

        #region Carry mechanics
        public static void ForcePlayerToCarryMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (target == null || localPlayer == null || target == localPlayer) return;
            PerformActionOnPlayer(target, "RPCA_StartCarry", localPlayer.photonView.ViewID);
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

        private static Dictionary<int, int> forcedCarryPairs = new Dictionary<int, int>();
        public static void ForcePlayerToCarry(Character carrier, Character target)
        {
            if (carrier == null || target == null || carrier == target) return;
            if (carrier.photonView == null || target.photonView == null) return;
            forcedCarryPairs[carrier.photonView.ViewID] = target.photonView.ViewID;
            carrier.photonView.RPC("RPCA_StartCarry", RpcTarget.All, target.photonView.ViewID);
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
        }

        public static void BreakAllCarries()
        {
            foreach (var id in forcedCarryPairs.Keys.ToList())
            {
                var pv = PhotonView.Find(id);
                if (pv != null) pv.RPC("RPCA_StopCarry", RpcTarget.All);
            }
            forcedCarryPairs.Clear();
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

                    Plugin.Log.LogInfo($"Requested MasterClient spawn '{itemName}' for {target.characterName} (view {viewId}).");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"GiveItemToPlayer error: {e.Message}");
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

        public static void SpamPoofEffect(Character target)
        {
            if (!ValidateTarget(target, "SpamPoofEffect")) return;
            PerformActionOnPlayer(target, "WarpPlayerRPC", target.transform.position, true);
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
                }
                catch (Exception e) { Plugin.Log.LogError($"DisarmPlayer error: {e.Message}"); }
            }
        }

        public static void MakePlayerGiant(Character target) { if (target == null) return; target.transform.localScale = Vector3.one * 3f; }
        public static void ResetPlayerSize(Character target) { if (target == null) return; target.transform.localScale = Vector3.one; }
        public static void MakeAllPlayersTiny() => ActionOnAllPlayers(p => p.transform.localScale = Vector3.one * 0.3f);
        public static void ResetAllPlayerSizes() => ActionOnAllPlayers(p => p.transform.localScale = Vector3.one);

        public static void FreezePlayer(Character target)
        {
            if (!ValidateTarget(target, "FreezePlayer")) return;
            PerformActionOnPlayer(target, "RPCA_Stick", (int)BodypartType.Hip, target.transform.position, target.transform.position, (int)CharacterAfflictions.STATUSTYPE.Cold, 0.1f);
        }

        public static void UnfreezePlayer(Character target)
        {
            if (!ValidateTarget(target, "UnfreezePlayer")) return;
            PerformActionOnPlayer(target, "RPCA_Unstick");
        }

        public static void PoofPlayer(Character target) { if (target == null) return; try { target.PlayPoofVFX(target.Head); } catch { } }

        public static IEnumerator PoofAllPlayersContinuously(float duration)
        {
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                ActionOnAllPlayers(PoofPlayer);
                yield return new WaitForSeconds(0.2f);
            }
        }

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

        public static void FeedPlayer(Character target, Item itemToFeed)
        {
            if (!ValidateTarget(target, "FeedPlayer") || itemToFeed == null) return;
            PerformActionOnPlayer(target, "GetFedItemRPC", itemToFeed.photonView != null ? itemToFeed.photonView.ViewID : -1);
        }
        #endregion

        #region Appearance
        public static void ForceChangeAppearance(Character target, Customization.Type type, int index)
        {
            if (!ValidateTarget(target, "ForceChangeAppearance")) return;
            if (target.photonView == null) return;
            string rpcName = string.Empty;
            switch (type)
            {
                case Customization.Type.Skin:
                    rpcName = "RPC_SetSkinColor";
                    break;
                case Customization.Type.Eyes:
                    rpcName = "RPC_SetEyes";
                    break;
                case Customization.Type.Mouth:
                    rpcName = "RPC_SetMouth";
                    break;
                case Customization.Type.Hat:
                    rpcName = "RPC_SetHat";
                    break;
                case Customization.Type.Accessory:
                    rpcName = "RPC_SetAccessory";
                    break;
                case Customization.Type.Fit:
                    rpcName = "RPC_SetOutfit";
                    break;
                case Customization.Type.Sash:
                    rpcName = "RPC_SetSash";
                    break;
            }
            if (!string.IsNullOrEmpty(rpcName))
            {
                try
                {
                    target.photonView.RPC(rpcName, RpcTarget.All, index);
                    Plugin.Log.LogInfo($"Sent {rpcName} RPC for {target.characterName} with index {index}.");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"ForceChangeAppearance error: {e.Message}");
                }
            }
        }

        public static void CopyPlayerAppearance(Character source, Character destination)
        {
            if (source == null || destination == null) return;
            var playerDataService = GameHandler.GetService<PersistentPlayerDataService>();
            if (playerDataService == null) return;
            var sourceData = playerDataService.GetPlayerData(source.photonView.Owner).customizationData;
            ForceChangeAppearance(destination, Customization.Type.Skin, sourceData.currentSkin);
            ForceChangeAppearance(destination, Customization.Type.Eyes, sourceData.currentEyes);
            ForceChangeAppearance(destination, Customization.Type.Mouth, sourceData.currentMouth);
            ForceChangeAppearance(destination, Customization.Type.Hat, sourceData.currentHat);
            ForceChangeAppearance(destination, Customization.Type.Accessory, sourceData.currentAccessory);
            ForceChangeAppearance(destination, Customization.Type.Fit, sourceData.currentOutfit);
            ForceChangeAppearance(destination, Customization.Type.Sash, sourceData.currentSash);
            Plugin.Log.LogInfo($"Copied appearance from {source.characterName} to {destination.characterName}.");
        }

        public static IEnumerator AppearanceScramblerRoutine()
        {
            Plugin.Log.LogInfo("Starting Appearance Scrambler!");
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) yield break;

            for (int i = 0; i < 20; i++)
            {
                foreach (var player in allPlayers)
                {
                    if (player != null)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, 20);
                        Customization.Type randomType = (Customization.Type)UnityEngine.Random.Range(0, 7);
                        ForceChangeAppearance(player, randomType, randomIndex);
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
            Plugin.Log.LogInfo("Appearance Scrambler finished.");
        }
        #endregion

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
    }
}
