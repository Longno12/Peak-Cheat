using System;
using System.Reflection;
using UnityEngine;

namespace ClassLibrary1.Cheats
{
    public static class ItemExtensions
    {
        private static MethodInfo _setStateMethod;
        public static void DropItem(this Item item, Character owner = null)
        {
            if (item == null) return;
            try
            {
                if (_setStateMethod == null)
                {
                    _setStateMethod = typeof(Item).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_setStateMethod != null)
                {
                    _setStateMethod.Invoke(item, new object[] { ItemState.Ground, owner });
                }
                else
                {
                    UnityEngine.Debug.LogError("[MyCoolMod] Could not find the internal method 'Item.SetState' via reflection.");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[MyCoolMod] An error occurred while calling Item.SetState via reflection: {e}");
            }
        }

        public static Item Duplicate(this Item original)
        {
            if (original == null) return null;

            GameObject clone = UnityEngine.Object.Instantiate(original.gameObject, original.transform.position + Vector3.right * 0.5f, original.transform.rotation);
            Item newItem = clone.GetComponent<Item>();

            if (newItem != null)
            {
                newItem.totalUses = original.totalUses;
                newItem.throwForceMultiplier = original.throwForceMultiplier;
                newItem.DropItem(null);
            }
            UnityEngine.Debug.Log($"[ItemExtensions] Duplicated item {original.name}");
            return newItem;
        }
    }
}