using System.Reflection;
using HarmonyLib;

namespace MyCoolMod.Patches
{
    public static class HP
    {
        public static bool IsPatched { get; private set; }
        private static Harmony instance;

        public static Harmony Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Harmony(PluginInfo.PLUGIN_GUID);
                }
                return instance;
            }
        }

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                Instance.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (Instance != null && IsPatched)
            {
                Instance.UnpatchSelf();
                IsPatched = false;
            }
        }
    }
}