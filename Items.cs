using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClassLibrary1.Cheats
{
    internal static class Items
    {
        public static List<Item> ItemObjects { get; } = new List<Item>();
        private static Item[] _cachedItems;
        private static float _itemScanInterval = 3f;
        private static float _lastItemScanTime = -999f;
        public static List<string> LuggageLabels { get; } = new List<string>();
        public static List<Luggage> LuggageObjects { get; } = new List<Luggage>();
        public static List<Luggage> AllOpenedLuggage { get; } = new List<Luggage>();
        public static int SelectedLuggageIndex { get; set; } = -1;
        public static Item GrabbedItem { get; set; }

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
            if (local == null)
            {
                UnityEngine.Debug.LogWarning("[Items] Cannot refresh items: no local character.");
                return;
            }
            UpdateCachedItemsIfNeeded();
            var headPos = local.Head;
            foreach (var it in _cachedItems)
            {
                if (it == null) continue;
                if (Vector3.Distance(headPos, it.transform.position) <= 300f)
                {
                    ItemObjects.Add(it);
                    string tags = it.tag.ToString();
                    UnityEngine.Debug.Log($"[Items] Found item: {it.name} | Tags: {tags}");
                }
            }
            UnityEngine.Debug.Log($"[Items] Refreshed item list. Found {ItemObjects.Count} items in range.");
        }

        public static void RefreshLuggageList()
        {
            LuggageLabels.Clear();
            LuggageObjects.Clear();
            SelectedLuggageIndex = -1;
            var local = Character.localCharacter;
            if (local == null)
            {
                UnityEngine.Debug.LogWarning("[Items] Cannot refresh luggage: no local character.");
                return;
            }
            IEnumerable<Luggage> all;
            try
            {
                all = Luggage.ALL_LUGGAGE;
            }
            catch
            {
                all = UnityEngine.Object.FindObjectsOfType<Luggage>();
            }
            var head = local.Head;
            var nearby = all.Where(l => l != null).Select(l => new { Lug = l, Dist = Vector3.Distance(head, l.Center()) }).Where(x => x.Dist <= 300f).OrderBy(x => x.Dist).ToList();
            foreach (var entry in nearby)
            {
                var lug = entry.Lug;
                string name = string.IsNullOrEmpty(lug.displayName) ? "Unnamed" : lug.displayName;
                string contents = "";
                try
                {
                    var spawnSpots = lug.PublicGetSpawnSpots();
                    if (spawnSpots != null && spawnSpots.Count > 0)
                    {
                        var previewItems = lug.GetPreviewObjects<Item>();
                        if (previewItems != null && previewItems.Length > 0)
                        {
                            contents = string.Join(", ", previewItems.Select(i =>
                            {
                                string cleanName = i.name.Replace("(Clone)", "");
                                return $"{cleanName} [{i.tag}]";
                            }));
                        }
                    }
                }
                catch
                {
                    contents = "???";
                }
                string label = string.IsNullOrEmpty(contents) ? $"{name} [{entry.Dist:F1}m]" : $"{name} [{entry.Dist:F1}m] → {contents}";
                LuggageLabels.Add(label);
                LuggageObjects.Add(lug);
            }
            UnityEngine.Debug.Log($"[Items] Found {LuggageObjects.Count} luggage objects in range.");
        }

        public static string[] GetAvailableItemNamesFromResources()
        {
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Item>();
                return all.Where(i => i != null).Select(i =>
                    {
                        string cleanName = i.name.Replace("(Clone)", "");
                        string tags = i.tag.ToString();
                        return $"{cleanName} [{tags}]";
                    }).Distinct().ToArray();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[Items] GetAvailableItemNamesFromResources failed: {ex}");
                return Array.Empty<string>();
            }
        }

        public static void ItemRain(int count = 200, float radius = 5f, float lifetimeSeconds = 7f)
        {
            if (!PhotonNetwork.InRoom)
            {
                UnityEngine.Debug.LogWarning("[Items] Can't ItemRain: not in Photon room.");
                return;
            }
            var names = GetAvailableItemNamesFromResources();
            if (names.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[Items] No item prefabs found in Resources for ItemRain.");
                return;
            }
            var cam = Camera.main;
            if (cam == null)
            {
                UnityEngine.Debug.LogWarning("[Items] Cannot ItemRain: no main camera.");
                return;
            }
            for (int i = 0; i < count; i++)
            {
                try
                {
                    string prefabName = names[UnityEngine.Random.Range(0, names.Length)];
                    Vector3 spawnPos = cam.transform.position + Vector3.up * (5f + i) + UnityUtil.OrbitVector(i + 47f, radius);
                    PhotonView view = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity).GetComponent<PhotonView>();
                    if (view != null)
                    {
                        DelayedAction(() =>
                        {
                            try { if (view != null) PhotonNetwork.Destroy(view); } catch { }
                        }, lifetimeSeconds);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Items] Error spawning item: {ex}");
                }
            }
        }

        public static void RemoveMyItems()
        {
            var views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            int destroyed = 0;

            foreach (var pv in views)
            {
                if (pv == null) continue;
                var item = pv.GetComponent<Item>();
                if (item != null && pv.IsMine)
                {
                    try
                    {
                        PhotonNetwork.Destroy(pv);
                        destroyed++;
                    }
                    catch { }
                }
            }
            UnityEngine.Debug.Log($"[Items] RemoveMyItems destroyed {destroyed} PhotonViews.");
        }

        public static void BoxESPItems(bool enable)
        {
            if (enable)
            {
                if (!PhotonNetwork.InRoom) return;

                foreach (var luggage in LuggageObjects)
                {
                    if (luggage == null) continue;
                    var lr = luggage.GetComponent<LineRenderer>();
                    if (lr == null)
                    {
                        lr = luggage.gameObject.AddComponent<LineRenderer>();
                        lr.positionCount = 4;
                        lr.loop = true;
                        lr.widthMultiplier = 0.02f;
                        lr.useWorldSpace = true;
                        lr.material = new Material(Shader.Find("Unlit/Color"))
                        {
                            color = Color.HSVToRGB(Time.time * 0.5f % 1f, 1f, 1f)
                        };
                    }
                    var c = luggage.Center();
                    float size = 0.6f;
                    lr.SetPosition(0, c + new Vector3(-size, -size + 1.75f, -size));
                    lr.SetPosition(1, c + new Vector3(size, -size + 1.75f, -size));
                    lr.SetPosition(2, c + new Vector3(size, -size + 1.75f, size));
                    lr.SetPosition(3, c + new Vector3(-size, -size + 1.75f, size));
                }
            }
            else
            {
                foreach (var luggage in LuggageObjects)
                {
                    if (luggage == null) continue;
                    var lr = luggage.GetComponent<LineRenderer>();
                    if (lr != null) UnityEngine.Object.Destroy(lr);
                }
            }
        }

        public static class UnityUtil
        {
            public static Vector3 OrbitVector(float radius, float speed)
            {
                float num = Time.time * speed;
                float num2 = Mathf.Cos(num) * radius;
                float num3 = Mathf.Sin(num) * radius;
                return new Vector3(num2, 0f, num3);
            }
        }

        private static void DelayedAction(Action action, float delay)
        {
            var go = new GameObject("ItemsDelayedAction");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var runner = go.AddComponent<DelayedActionRunner>();
            runner.Run(action, delay);
        }

        private class DelayedActionRunner : MonoBehaviour
        {
            private Action _action;
            private float _delay;
            public void Run(Action action, float delay) { _action = action; _delay = delay; StartCoroutine(Do()); }
            private IEnumerator Do()
            {
                yield return new WaitForSeconds(_delay);
                try { _action?.Invoke(); } catch { }
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        public class Luggage : Spawner
        {
            public static List<Luggage> ALL_LUGGAGE = new List<Luggage>();
            public string displayName;

            public List<Transform> PublicGetSpawnSpots()
            {
                return base.GetSpawnSpots();
            }

            public T[] GetPreviewObjects<T>() where T : UnityEngine.Object
            {
                try
                {
                    var field = typeof(Spawner).GetField("spawnPrefabs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (field != null)
                    {
                        var prefabs = field.GetValue(this) as UnityEngine.Object[];
                        if (prefabs != null && prefabs.Length > 0)
                        {
                            return prefabs.OfType<T>().ToArray();
                        }
                    }
                    var field2 = typeof(Spawner).GetField("spawnTable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (field2 != null)
                    {
                        var table = field2.GetValue(this) as IEnumerable<UnityEngine.Object>;
                        if (table != null)
                        {
                            return table.OfType<T>().ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[Items] GetPreviewObjects failed on luggage {displayName}: {ex}");
                }
                return Array.Empty<T>();
            }
            public Vector3 Center()
            {
                var renderers = GetComponentsInChildren<MeshRenderer>();
                if (renderers == null || renderers.Length == 0)
                    return transform.position;

                var bounds = renderers[0].bounds;
                foreach (var r in renderers)
                {
                    if (r != null) bounds.Encapsulate(r.bounds);
                }
                return bounds.center;
            }
       }

    }
}
