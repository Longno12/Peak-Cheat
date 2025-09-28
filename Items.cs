using MyCoolMod; // Assuming ModGUI and Drawing are defined here
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ClassLibrary1.Cheats
{
    /// <summary>
    /// Main static class for handling item-related cheats and utilities.
    /// </summary>
    internal static class Items
    {
        public static List<Item> ItemObjects { get; } = new List<Item>();
        private static Item[] _cachedItems;
        private static readonly float _itemScanInterval = 3f;
        private static float _lastItemScanTime = -999f;
        public static List<string> LuggageLabels { get; } = new List<string>();
        public static List<Luggage> LuggageObjects { get; } = new List<Luggage>();
        public static List<Luggage> AllOpenedLuggage { get; } = new List<Luggage>();
        public static int SelectedLuggageIndex { get; set; } = -1;
        public static Item GrabbedItem { get; set; }

        #region Item Modifiers

        public static void ToggleInfiniteItems()
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null) continue;
                item.totalUses = 9999999;
                item.SetUseRemainingPercentage(1f);
            }
            ModGUI.ShowNotification("Items", "All items set to infinite uses.");
        }

        public static void SuperThrow()
        {
            EnableSuperThrows(50f);
        }

        public static void GlowAllItems()
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null || item.mainRenderer == null) continue;
                item.mainRenderer.material.shader = Shader.Find("Hidden/Internal-Colored");
                item.mainRenderer.material.color = Color.cyan;
            }
        }

        #endregion

        #region Grabbed Item Actions

        public static void DropHeldItem()
        {
            var held = GrabbedItem;
            if (held == null) return;

            held.DropItem(Character.localCharacter);
            ModGUI.ShowNotification("Items", $"Dropped {held.GetName()}");
        }

        public static void DuplicateHeldItem()
        {
            var held = GrabbedItem;
            if (held == null) return;

            var dupe = held.Duplicate();
            if (dupe != null)
            {
                dupe.DropItem(null);
                ModGUI.ShowNotification("Duplicate", $"Duplicated {held.GetName()}");
            }
        }


        #endregion

        #region World Item Actions

        public static void FreezeGroundItems()
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item != null && item.itemState == ItemState.Ground)
                    item.SetKinematicNetworked(true);
            }
        }

        public static void LaunchAllItems(float force = 50f)
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null) continue;
                Rigidbody itemRigidbody = item.GetComponent<Rigidbody>();
                if (itemRigidbody != null)
                {
                    itemRigidbody.AddForce(Vector3.up * force, ForceMode.VelocityChange);
                }
            }
        }

        public static void BackpackAllNearby(float radius = 10f)
        {
            var player = Character.localCharacter;
            if (player == null) return;
            var backpack = player.GetComponent<BackpackReference>();
            if (object.ReferenceEquals(backpack, null)) return;
            var slots = backpack.GetVisuals().backpackSlots;
            if (slots == null || slots.Length == 0) return;
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null || item.itemState != ItemState.Ground) continue;
                if (Vector3.Distance(player.transform.position, item.transform.position) <= radius)
                {
                    for (byte slot = 0; slot < slots.Length; slot++)
                    {
                        if (slots[slot].childCount == 0)
                        {
                            item.PutInBackpackRPC(slot, backpack);
                            break;
                        }
                    }
                }
            }
            ModGUI.ShowNotification("Backpack", $"Stored nearby items within {radius}m");
        }

        public static void EnableSuperThrows(float multiplier = 100f)
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null) continue;
                item.throwForceMultiplier = multiplier;
            }
            ModGUI.ShowNotification("SuperThrows", $"Throw force set to {multiplier}x");
        }

        public static void ResetThrowForce()
        {
            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null) continue;
                item.throwForceMultiplier = 1f;
            }
            ModGUI.ShowNotification("SuperThrows", "Throw force reset to 1x");
        }

        public static void ClearGroundItems()
        {
            int count = 0;
            var groundItems = Item.ALL_ITEMS.Where(i => i != null && i.itemState == ItemState.Ground).ToList();

            foreach (var item in groundItems)
            {
                try
                {
                    PhotonView view = item.GetComponent<PhotonView>();
                    if (view != null)
                    {
                        PhotonNetwork.Destroy(view);
                        count++;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[MyCoolMod] Failed to clear an item: {e.Message}");
                }
            }
            ModGUI.ShowNotification("Items", $"Cleared {count} ground items.");
        }

        public static void LootMagnet(float radius = 20f)
        {
            var player = Character.localCharacter;
            if (player == null) return;

            foreach (var item in Item.ALL_ITEMS)
            {
                if (item == null || item.itemState != ItemState.Ground) continue;

                if (Vector3.Distance(player.transform.position, item.transform.position) <= radius)
                    item.Interact(player);
            }
            ModGUI.ShowNotification("LootMagnet", $"Pulled items within {radius}m");
        }

        public static void AutoPickupNearby()
        {
            var player = Character.localCharacter;
            if (player == null) return;

            foreach (var item in Item.ALL_ITEMS.Where(i => i != null && i.itemState == ItemState.Ground))
            {
                if (Vector3.Distance(player.transform.position, item.transform.position) < 3f)
                    item.Interact(player);
            }
        }

        public static void EquipFirstGroundItem()
        {
            var player = Character.localCharacter;
            if (player == null) return;

            var item = Item.ALL_ITEMS.FirstOrDefault(i => i != null && i.itemState == ItemState.Ground);
            if (item != null)
            {
                // FIX: Replaced SetState with Interact here as well.
                item.Interact(player);
                ModGUI.ShowNotification("Equip", $"Equipped {item.GetName()}");
            }
        }

        #endregion

        #region Internal Helpers

        private static void UpdateCachedItemsIfNeeded()
        {
            if (Time.time - _lastItemScanTime <= _itemScanInterval) return;

            _cachedItems = UnityEngine.Object.FindObjectsOfType<Item>();
            _lastItemScanTime = Time.time;
        }

        public static void RefreshItemList()
        {
            ItemObjects.Clear();
            var local = Character.localCharacter;
            if (local == null) return;

            UpdateCachedItemsIfNeeded();
            var headPos = local.Head;

            if (_cachedItems == null) return;

            foreach (var it in _cachedItems)
            {
                if (it != null && Vector3.Distance(headPos, it.transform.position) <= 300f)
                    ItemObjects.Add(it);
            }
        }

        public static void RefreshLuggageList()
        {
            LuggageLabels.Clear();
            LuggageObjects.Clear();
            SelectedLuggageIndex = -1;

            var local = Character.localCharacter;
            if (local == null) return;

            // FIX: Check if the static list is null or empty. If it is, then find the objects in the scene.
            // This avoids the type mismatch error and is more explicit about the logic.
            IEnumerable<Luggage> all = Luggage.ALL_LUGGAGE;
            if (all == null || !all.Any())
            {
                all = UnityEngine.Object.FindObjectsOfType<Luggage>();
            }

            // --- The rest of your method remains exactly the same ---

            var head = local.Head;
            var nearby = all.Where(l => l != null)
                            .Select(l => new { Lug = l, Dist = Vector3.Distance(head, l.Center()) })
                            .Where(x => x.Dist <= 300f)
                            .OrderBy(x => x.Dist)
                            .ToList();

            foreach (var entry in nearby)
            {
                var lug = entry.Lug;
                string name = string.IsNullOrEmpty(lug.displayName) ? "Unnamed Luggage" : lug.displayName;
                string contents = "???";

                try
                {
                    var previewItems = lug.GetPreviewObjects<Item>();
                    if (previewItems != null && previewItems.Length > 0)
                    {
                        contents = string.Join(", ", previewItems.Select(i => i.name.Replace("(Clone)", "")));
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[MyCoolMod] Failed to get luggage preview items: {e.Message}");
                    contents = "Error!";
                }

                string label = string.IsNullOrEmpty(contents) ? $"{name} [{entry.Dist:F1}m]" : $"{name} [{entry.Dist:F1}m] → {contents}";
                LuggageLabels.Add(label);
                LuggageObjects.Add(lug);
            }
        }

        /// <summary>
        /// Gets all available item prefab names from game resources.
        /// </summary>
        public static string[] GetAvailableItemNamesFromResources()
        {
            try
            {
                // Returns only the object name, which is required for instantiation.
                return Resources.FindObjectsOfTypeAll<Item>()
                                .Where(i => i != null && i.gameObject.scene.name == null) // Filter for prefabs, not scene objects
                                .Select(i => i.name.Replace("(Clone)", ""))
                                .Distinct()
                                .ToArray();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MyCoolMod] Failed to get item names from resources: {e.Message}");
                return Array.Empty<string>();
            }
        }

        public static void ItemRain(int count = 200, float radius = 5f, float lifetimeSeconds = 7f)
        {
            if (!PhotonNetwork.InRoom) return;

            var names = GetAvailableItemNamesFromResources();
            if (names.Length == 0)
            {
                ModGUI.ShowNotification("ItemRain", "No item prefabs found in Resources!");
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    string prefabName = names[UnityEngine.Random.Range(0, names.Length)];

                    // Corrected the arguments for OrbitVector.
                    Vector3 spawnPos = cam.transform.position + Vector3.up * (5f + (i * 0.1f)) + UnityUtil.OrbitVector(radius, i * 20f);

                    // Photon requires prefabs to be in a "Resources" folder to be instantiated by name.
                    GameObject spawnedObject = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);

                    if (spawnedObject != null)
                    {
                        var view = spawnedObject.GetComponent<PhotonView>();
                        if (view != null)
                        {
                            // This creates a temporary GameObject to delay an action.
                            // While creating many objects is not ideal, it's a reliable pattern for fire-and-forget actions.
                            DelayedActionRunner.Run(() => {
                                if (view != null) PhotonNetwork.Destroy(view);
                            }, lifetimeSeconds);
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[MyCoolMod] ItemRain failed to spawn item: {e.Message}");
                }
            }
        }

        public static void RemoveMyItems()
        {
            // Find our own PhotonViews that are attached to Item objects and destroy them.
            var views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            foreach (var pv in views)
            {
                if (pv != null && pv.IsMine && pv.GetComponent<Item>() != null)
                {
                    try { PhotonNetwork.Destroy(pv); }
                    catch (Exception e) { UnityEngine.Debug.LogError($"[MyCoolMod] Failed to destroy item: {e.Message}"); }
                }
            }
        }
        #endregion
    }

    #region Helper & Game-Specific Classes

    /// <summary>
    /// Utility functions for Unity-specific operations.
    /// Moved out of the Items class for better code structure.
    /// </summary>
    public static class UnityUtil
    {
        public static Vector3 OrbitVector(float radius, float angleDegrees)
        {
            float angle = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }

    /// <summary>
    /// A MonoBehaviour helper to run an action after a delay.
    /// Moved out of the Items class for reusability.
    /// </summary>
    internal class DelayedActionRunner : MonoBehaviour
    {
        private Action _action;

        public static void Run(Action action, float delay)
        {
            var go = new GameObject("DelayedActionRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var runner = go.AddComponent<DelayedActionRunner>();
            runner._action = action;
            runner.StartCoroutine(runner.Do(delay));
        }

        private IEnumerator Do(float delay)
        {
            yield return new WaitForSeconds(delay);
            try
            {
                _action?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MyCoolMod] Error in delayed action: {e.Message}");
            }
            finally
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }

    public class Luggage : Spawner
    {
        public static List<Luggage> ALL_LUGGAGE = new List<Luggage>();
        public string displayName;

        public List<Transform> PublicGetSpawnSpots() => base.GetSpawnSpots();

        public T[] GetPreviewObjects<T>() where T : UnityEngine.Object
        {
            FieldInfo prefabField = typeof(Spawner).GetField("spawnPrefabs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prefabField != null)
            {
                var prefabs = prefabField.GetValue(this) as UnityEngine.Object[];
                if (prefabs != null && prefabs.Length > 0)
                    return prefabs.OfType<T>().ToArray();
            }

            FieldInfo tableField = typeof(Spawner).GetField("spawnTable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (tableField != null)
            {
                var table = tableField.GetValue(this) as IEnumerable<UnityEngine.Object>;
                if (table != null)
                    return table.OfType<T>().ToArray();
            }

            return Array.Empty<T>();
        }

        public Vector3 Center()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0) return transform.position;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds.center;
        }
    }

    #endregion
}