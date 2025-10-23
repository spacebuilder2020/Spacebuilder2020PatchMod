using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Assets.Scripts;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace Spacebuilder2020PatchMod
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    class GameVersion : Attribute
    {
        public Version MinVersion;
        public Version MaxVersion;

        public GameVersion(string minVersion, string maxVersion)
        {
            MinVersion = new Version(minVersion);
            MaxVersion = new Version(maxVersion);
        }
    }

    class VersionAwareClassProcessor : PatchClassProcessor
    {
        public VersionAwareClassProcessor(Harmony instance, Type type, Version version,
            bool allowUnannotatedType = false) : base(instance, type, allowUnannotatedType)
        {
            var patchMethods = (IList)typeof(PatchClassProcessor)
                .GetField("patchMethods", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(this);
            if (patchMethods == null || patchMethods.Count == 0)
            {
                return;
            }

            var attributePatchInfo = patchMethods[0].GetType()
                .GetField("info", BindingFlags.NonPublic | BindingFlags.Instance);

            var toRemove = new List<object>();
            if (attributePatchInfo == null)
            {
                return;
            }

            foreach (var patchMethod in patchMethods)
            {
                var info = (HarmonyMethod)attributePatchInfo.GetValue(patchMethod);
                GameVersion ver = info.method.GetCustomAttributes().OfType<GameVersion>().FirstOrDefault();
                if (ver != null && (ver.MinVersion > version || ver.MaxVersion < version))
                {
                    Debug.Log(
                        $"Patch in {type.FullName}.{info.method.Name} for {info.declaringType.Name}.{info.methodName} ignored because game version does not match!");
                    Debug.Log($"Current: {version} Min: {ver?.MinVersion} Max: {ver?.MaxVersion}");
                    toRemove.Add(patchMethod);
                }
            }

            foreach (var patchMethod in toRemove)
            {
                patchMethods.Remove(patchMethod);
            }
        }
    }

    public static class VersionAwarePatcher
    {
        public static void VersionAwarePatchAll(this Harmony harmony)
        {
            AccessTools.GetTypesFromAssembly(new StackTrace().GetFrame(1).GetMethod().ReflectedType?.Assembly).Do(type =>
            {
                var ver = type.GetCustomAttributes(true).OfType<GameVersion>().FirstOrDefault();
                var version = typeof(GameManager).Assembly.GetName().Version;
                
                if (ver != null && (ver.MinVersion > version || ver.MaxVersion < version) )
                {
                    Debug.Log($"Patch class {type.FullName} ignored because game version does not match!");
                    Debug.Log($"Current: {version} Min: {ver?.MinVersion} Max: {ver?.MaxVersion}");
                    return;
                }
                new VersionAwareClassProcessor(harmony, type, version).Patch();
            });
        }
    }
}