



// THIS IS SO THE GUI WILL NOT BREAK IF ANY OF THE PATCHES GETS CHANGED



using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Encryptic
{
    public static class PatchManager
    {
        private static Harmony harmonyInstance;
        private static bool isPatched = false;
        private static int errorCount = 0;
        private static readonly List<Type> patchTypes = new List<Type>();
        public static bool IsPatched => isPatched;
        public static int ErrorCount => errorCount;

        public static void ApplyAll()
        {
            if (isPatched) return;
            harmonyInstance = new Harmony("com.encryptic.patcher");
            patchTypes.Clear();
            errorCount = 0;
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsClass && Attribute.IsDefined(type, typeof(HarmonyPatch)))
                {
                    patchTypes.Add(type);
                    try
                    {
                        harmonyInstance.CreateClassProcessor(type).Patch();
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                    }
                }
            }
            isPatched = true;
        }

        public static void RemoveAll()
        {
            if (!isPatched || harmonyInstance == null) return;
            harmonyInstance.UnpatchSelf();
            isPatched = false;
            patchTypes.Clear();
            harmonyInstance = null;
        }

        public static void ApplySingle(Type targetType, string methodName, MethodInfo prefix = null, MethodInfo postfix = null, Type[] paramTypes = null)
        {
            MethodInfo original = GetMethod(targetType, methodName, paramTypes);
            if (original == null) throw new Exception($"Method '{methodName}' not found in {targetType.FullName}");
            harmonyInstance.Patch( original, prefix != null ? new HarmonyMethod(prefix) : null, postfix != null ? new HarmonyMethod(postfix) : null);
        }

        public static void RemoveSingle(Type targetType, string methodName, Type[] paramTypes = null)
        {
            MethodInfo original = GetMethod(targetType, methodName, paramTypes);
            if (original == null) throw new Exception($"Method '{methodName}' not found in {targetType.FullName}");
            harmonyInstance.Unpatch(original, HarmonyPatchType.All, harmonyInstance.Id);
        }

        private static MethodInfo GetMethod(Type type, string name, Type[] parameters = null)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return parameters == null ? type.GetMethod(name, flags) : type.GetMethod(name, flags, null, parameters, null);
        }
    }
}
