﻿using HarmonyLib;
using Stunlock.Core;
using Stunlock.Localization;
using UIBuddy.Managers;

namespace UIBuddy.Patches;

[HarmonyPatch]
internal static class LocalizationPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Localization), nameof(Localization.Get), typeof(AssetGuid), typeof(bool))]
    private static bool Get(AssetGuid guid, ref string __result)
    {
        if (!LocalizationManager.HasKey(guid))
        {
            return true;
        }

        __result = LocalizationManager.GetKey(guid);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Localization), nameof(Localization.Get), typeof(LocalizationKey), typeof(bool))]
    private static bool Get(LocalizationKey key, ref string __result)
    {
        if (!LocalizationManager.HasKey(key))
        {
            return true;
        }

        __result = LocalizationManager.GetKey(key);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Localization), nameof(Localization.HasKey))]
    private static bool HasKey(AssetGuid guid, ref bool __result)
    {
        if (!LocalizationManager.HasKey(guid))
        {
            return true;
        }

        __result = true;
        return false;
    }
}
