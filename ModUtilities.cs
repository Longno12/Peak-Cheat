using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace MyCoolMod
{
    public static class ModUtilities
    {
        private static float _originalMovementForce = -1f;
        private static float _originalJumpImpulse = -1f;

        public static void KillPlayer(Character target)
        {
            if (target == null) return;
            Vector3 itemSpawnPoint = target.Center + Vector3.up * 0.2f + Vector3.forward * 0.1f;
            PerformActionOnPlayer(target, "RPCA_Die", itemSpawnPoint);
        }
        public static void TripPlayer(Character target)
        {
            if (target == null) return;
            float fallDuration = 3f;
            PerformActionOnPlayer(target, "RPCA_Fall", fallDuration);
        }
        public static void PassOutPlayer(Character target)
        {
            if (target == null)
            {
                Plugin.Log.LogWarning("PassOutPlayer failed: target was null.");
                return;
            }

            try
            {
                if (target.photonView != null)
                {
                    target.photonView.RPC("RPCA_PassOut", RpcTarget.All);
                    Plugin.Log.LogInfo($"Sent PassOut RPC to {target.characterName}.");
                }
                else
                {
                    Plugin.Log.LogError($"PassOutPlayer failed: {target.characterName} has no PhotonView.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"An error occurred while sending PassOut RPC: {ex.Message}");
            }
        }
        public static void WakeUpPlayer(Character target) => PerformActionOnPlayer(target, "RPCA_UnPassOut");
        public static void ForcePushPlayer(Character target) => PerformActionOnPlayer(target, "RPCA_AddForceToBodyPart", 0, Vector3.up * 3000f);
        public static void RevivePlayer(Character target)
        {
            if (target == null) return;
            Vector3 revivePos = target.Ghost != null ? target.Ghost.transform.position : target.Head;
            PerformActionOnPlayer(target, "RPCA_ReviveAtPosition", revivePos + Vector3.up, false);
        }

        public static void TeleportAllPlayersToMe()
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer == null)
            {
                Plugin.Log.LogWarning("Cannot TeleportAllPlayersToMe, local character is not available.");
                return;
            }

            Vector3 myPos = localPlayer.Head + new Vector3(0f, 1f, 0f);
            foreach (var character in PlayerManager.GetAllCharacters())
            {
                if (character != null && !character.IsLocal)
                {
                    character.photonView.RPC("WarpPlayerRPC", RpcTarget.All, myPos, true);
                }
            }
        }

        public static void RenderPlayerDead(Character target)
        {
            if (target?.refs?.customization?.GetComponent<PhotonView>() != null)
            {
                target.refs.customization.GetComponent<PhotonView>().RPC("CharacterDied", RpcTarget.All);
            }
        }
        public static void RenderDead()
        {
            var p = PlayerManager.GetLocalPlayer();
            if (p != null) RenderPlayerDead(p);
        }
        public static void RenderAllDead()
        {
            ActionOnAllPlayers(RenderPlayerDead);
        }
        public static void TeleportPlayerToMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null && target != null && target != localPlayer)
                PerformActionOnPlayer(target, "WarpPlayerRPC", localPlayer.transform.position);
        }
        private static void PerformActionOnPlayer(Character target, string rpcName, params object[] parameters)
        {
            if (target != null && target.photonView != null)
                target.photonView.RPC(rpcName, RpcTarget.All, parameters);
        }
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

        public static void ReviveSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) RevivePlayer(p); }
        public static void KillSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) KillPlayer(p); }
        public static void PassOutSelf()
        {
            var p = PlayerManager.GetLocalPlayer();
            if (p != null) PassOutPlayer(p);
        }
        public static void TripSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) TripPlayer(p); }

        public static void StickPlayer(Character target)
        {
            if (target == null || Camera.main == null) return;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 100f))
            {
                Vector3 headPos = target.Head;
                Vector3 offset = hit.point - headPos;
                target.transform.position += offset;
                target.transform.SetParent(hit.transform, true);
            }
        }
        public static void UnStickPlayer(Character target)
        {
            if (target != null) target.photonView.RPC("RPCA_Unstick", RpcTarget.All);
        }

        public static void ActionOnAllPlayers(Action<Character> action)
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            Character localPlayer = PlayerManager.GetLocalPlayer();
            foreach (var player in allPlayers)
            {
                if (localPlayer != null && player == localPlayer) continue;
                action(player);
            }
        }
        public static void AttackWithBees(Character target)
        {
            if (target == null) return;
            try
            {
                GameObject beeSwarmObject = PhotonNetwork.Instantiate("BeeSwarm", target.Head, Quaternion.identity);
                if (beeSwarmObject == null) return;
                BeeSwarm beeComponent = beeSwarmObject.GetComponent<BeeSwarm>();
                if (beeComponent != null && beeComponent.photonView != null)
                {
                    beeComponent.photonView.RPC("SetBeesAngryRPC", RpcTarget.All, true);
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"Bee spawn error: {ex.Message}"); }
        }
        public static void BeesSelf()
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null)
            {
                AttackWithBees(localPlayer);
            }
        }
        public static void PassOutAll()
        {
            ActionOnAllPlayers(PassOutPlayer);
        }
        public static void BeesAll()
        {
            ActionOnAllPlayers(AttackWithBees);
        }
        public static void EndGame()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<Player>();
            if (player?.photonView != null) player.photonView.RPC("RPCEndGame", RpcTarget.All);
        }
        public static void SpawnNetworkedJellyfish(Character charac)
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
        public static void JellyfishSelf()
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null)
            {
                SpawnNetworkedJellyfish(localPlayer);
            }
        }
        public static void JellyfishAll()
        {
            ActionOnAllPlayers(SpawnNetworkedJellyfish);
        }
        public static void JellyfishBomb(Character target, int count = 5)
        {
            if (target == null) return;

            for (int i = 0; i < count; i++)
            {
                SpawnNetworkedJellyfish(target);
            }
        }
        private static void SpawnJellyfishTrapAtPosition(Vector3 position)
        {
            GameObject root = new GameObject("JellyfishTrapRoot");
            root.transform.position = position;
            PhotonView pv = root.AddComponent<PhotonView>();
            pv.ViewID = UnityEngine.Random.Range(10000, 99999);
            pv.TransferOwnership(PhotonNetwork.LocalPlayer);
            GameObject child = new GameObject("SlipperyJellyfish");
            child.transform.SetParent(root.transform);
            child.AddComponent<SlipperyJellyfish>();
            var col = child.AddComponent<SphereCollider>();
            col.isTrigger = true;
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
            if (target == null) return;

            Vector3 center = target.Head + Vector3.up * 30f;

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 spawnPos = new Vector3(center.x + randomCirclePoint.x, center.y, center.z + randomCirclePoint.y);
                SpawnJellyfishTrapAtPosition(spawnPos);
            }
        }
        public static void CrashPlayer(Character target)
        {
            if (target?.photonView?.Controller == null)
            {
                Plugin.Log.LogWarning("CrashPlayer failed: Target or its Controller was null.");
                return;
            }

            try
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                PhotonNetwork.CurrentRoom.SetMasterClient(PhotonNetwork.LocalPlayer);
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.DestroyPlayerObjects(target.photonView.Controller);
                    Plugin.Log.LogInfo($"Crash command sent for player {target.characterName}.");
                }
                else
                {
                    Plugin.Log.LogWarning("Failed to send crash command: Could not become Master Client.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"An error occurred during CrashPlayer: {ex.Message}");
            }
        }
        public static void CrashAll() => ActionOnAllPlayers(CrashPlayer);
        public static void CrashSelf() { var p = PlayerManager.GetLocalPlayer(); if (p != null) CrashPlayer(p); }
        public static void ForceWinGame()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<Player>();
            if (player?.photonView != null) player.photonView.RPC("RPCEndGame_ForceWin", RpcTarget.All, PhotonNetwork.LocalPlayer);
        }
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
            foreach (Character character in Character.AllCharacters)
            {
                if (character == null || character.refs.mainRenderer == null) continue;

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
                        isVisible = false;
                        break;
                    }

                    Vector2 screenPos = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                    min = Vector2.Min(min, screenPos);
                    max = Vector2.Max(max, screenPos);
                }

                if (isVisible)
                {
                    float width = max.x - min.x;
                    float height = max.y - min.y;

                    Color color = character.refs.customization.PlayerColor;


                    RectFilled(min.x, min.y, width, 1f, color);
                    RectFilled(min.x, min.y, 1f, height, color);
                    RectFilled(min.x + width, min.y, 1f, height, color);
                    RectFilled(min.x, min.y + height, width, 1f, color);
                }
            }
        }
        public static void StunLockAll()
        {
            ActionOnAllPlayers(StunLockPlayer);
        }
        public static void FlingAll()
        {
            ActionOnAllPlayers(FlingPlayer);
        }
        public static void TumblePlayer(Character target)
        {
            if (target == null || target.photonView == null) return;
            float randomX = UnityEngine.Random.Range(-5000f, 5000f);
            float randomY = UnityEngine.Random.Range(2000f, 6000f);
            float randomZ = UnityEngine.Random.Range(-5000f, 5000f);
            Vector3 tumbleForce = new Vector3(randomX, randomY, randomZ);
            target.photonView.RPC("RPCA_Fall", RpcTarget.All, 3f);
            target.photonView.RPC("RPCA_AddForceToBodyPart", RpcTarget.All, 0, tumbleForce);
        }
        public static void ForcePassOutPlayer(Character target)
        {
            if (target == null || target.photonView == null) return;
            target.photonView.RPC("RPCA_PassOut", RpcTarget.All);
        }
        public static void StunLockPlayer(Character target)
        {
            if (target == null || target.photonView == null) return;
            for (int i = 0; i < 10; i++)
            {
                target.photonView.RPC("JumpRpc", RpcTarget.All, false);
            }
        }
        public static void FlingPlayer(Character target)
        {
            if (target == null || target.photonView == null) return;

            target.photonView.RPC("RPCA_Fall", RpcTarget.All, 3f);
            Vector3 flingForce = (Vector3.up * 1000000f) + (Camera.main.transform.forward * 4000f);
            target.photonView.RPC("RPCA_AddForceToBodyPart", RpcTarget.All, 0, flingForce);
        }
        public static void ToggleAlwaysSprint()
        {
            Plugin.AlwaysSprintEnabled = !Plugin.AlwaysSprintEnabled;
        }
        public static void ToggleNoFallDamage()
        {
            Plugin.NoFallDamageEnabled = !Plugin.NoFallDamageEnabled;
        }
        public static void ToggleKeepItemsOnDeath()
        {
            Plugin.KeepItemsEnabled = !Plugin.KeepItemsEnabled;
        }
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
        public static void KillAllPlayers()
        {
            ActionOnAllPlayers(KillPlayer);
        }
        public static void ForcePlayerToCarryMe(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (target == null || localPlayer == null || target == localPlayer) return;
            PerformActionOnPlayer(target, "RPCA_StartCarry", localPlayer.photonView);
        }
        public static void MakePlayerCarryPlayer(Character carrier, Character targetToCarry)
        {
            if (carrier == null || targetToCarry == null || carrier == targetToCarry) return;
            PerformActionOnPlayer(carrier, "RPCA_StartCarry", targetToCarry.photonView);
        }
        public static void CarryAndFling(Character target)
        {
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (target == null || localPlayer == null) return;
            PerformActionOnPlayer(localPlayer, "RPCA_StartCarry", target.photonView);
            FlingPlayer(localPlayer);
        }
        public static System.Collections.IEnumerator CreatePlayerChainRoutine()
        {
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null || allPlayers.Count < 2) yield break;

            var playerList = new List<Character>(allPlayers);
            var localPlayer = PlayerManager.GetLocalPlayer();
            if (localPlayer != null)
            {
                playerList.Remove(localPlayer);
            }

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
        public static void GiveItemToPlayer(Character target, string itemName)
        {
            if (target == null || string.IsNullOrEmpty(itemName) || target.photonView == null)
            {
                Plugin.Log.LogWarning("GiveItemToPlayer failed: Target or itemName was invalid.");
                return;
            }
            target.photonView.RPC("RPC_SpawnItemInHandMaster", RpcTarget.MasterClient, itemName);
            Plugin.Log.LogInfo($"Sent request to Master Client to give '{itemName}' to {target.characterName}.");
        }
        public static void MeteorStrikePlayer(Character target)
        {
            if (target == null || target.photonView == null) return;
            Vector3 currentPos = target.transform.position;
            Vector3 skyPos = new Vector3(currentPos.x, currentPos.y + 200f, currentPos.z);
            PerformActionOnPlayer(target, "WarpPlayerRPC", skyPos, false);
            Vector3 downwardForce = Vector3.down * 50000f;
            PerformActionOnPlayer(target, "RPCA_AddForceToBodyPart", 0, downwardForce);
        }
        public static void SpamPoofEffect(Character target)
        {
            if (target == null || target.photonView == null) return;
            PerformActionOnPlayer(target, "WarpPlayerRPC", target.transform.position, true);
        }
        public static void DisarmPlayer(Character target)
        {
            if (target == null || target.refs?.items?.currentSelectedSlot == null) return;
            if (target.refs.items.currentSelectedSlot.IsSome)
            {
                byte equippedSlotId = target.refs.items.currentSelectedSlot.Value;
                Vector3 dropPosition = target.Center;
                PerformActionOnPlayer(target, "DropItemFromSlotRPC", equippedSlotId, dropPosition);
            }
        }
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
    }
}
