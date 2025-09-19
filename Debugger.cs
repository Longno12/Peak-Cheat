using System;
using UnityEngine;
using Zorro.Core;
using Zorro.Core.CLI;

namespace Debug.Miscellaneous
{
    internal class DebugScreen
    {
        private static readonly KeyCode toggleKey = KeyCode.F3;

        private static DebugUIHandler uiHandler;

        public static bool Active { get; private set; } = true;

        public static void Update()
        {
            if (!Active) return;

            if (Input.GetKeyDown(toggleKey))
            {
                if (TryGetUI(out var ui))
                {
                    if (ui.IsOpened)
                    {
                        ui.Hide();
                        UnityEngine.Debug.Log("[DebugScreen] Debug UI hidden.");
                    }
                    else
                    {
                        ui.Show();
                        UnityEngine.Debug.Log("[DebugScreen] Debug UI shown.");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[DebugScreen] Debug UI not found.");
                }
            }
        }

        public static void SetActive(bool enable)
        {
            Active = enable;
            UnityEngine.Debug.Log($"[DebugScreen] Feature {(enable ? "enabled" : "disabled")}.");
        }

        private static bool TryGetUI(out DebugUIHandler handler)
        {
            if (uiHandler == null)
                uiHandler = Singleton<DebugUIHandler>.Instance;

            if (uiHandler != null)
            {
                handler = uiHandler;
                return true;
            }

            handler = null;
            return false;
        }
    }
}
