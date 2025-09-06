using Photon.Pun;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public class BeeSwarm : MonoBehaviourPun { }

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
            var player = UnityEngine.Object.FindObjectOfType<Player>();
            if (player?.photonView != null) player.photonView.RPC("RPCEndGame", RpcTarget.All);
        }
        public static void ForceWinGame()
        {
            var player = UnityEngine.Object.FindObjectOfType<Player>();
            if (player?.photonView != null) player.photonView.RPC("RPCEndGame_ForceWin", RpcTarget.All, PhotonNetwork.LocalPlayer);
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