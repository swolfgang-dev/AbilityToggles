using BepInEx.Configuration;
using GlobalEnums;
using HutongGames.PlayMaker;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class BeastlingCallAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;
        private static readonly HashSet<string> LoggedDetections = new HashSet<string>();

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalNeedolinMemories,
                "Beastling Call",
                false,
                ConfigUi.GlobalDescription("Use Beastling Call fast travel behavior for every save without setting PlayerData.UnlockedFastTravelTeleport.", ConfigUi.BeastlingCallOrder));

            saveEnabled = saveConfig.Bind(
                ConfigUi.NeedolinMemories,
                "Beastling Call",
                false,
                ConfigUi.SaveDescription("Use Beastling Call fast travel behavior without setting PlayerData.UnlockedFastTravelTeleport.", ConfigUi.BeastlingCallOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool IsAvailable()
        {
            var playerData = PlayerData.instance;
            return IsEnabled() &&
                playerData != null &&
                playerData.UnlockedFastTravel;
        }

        private static bool RawUnlocked
        {
            get
            {
                return PlayerData.instance != null && PlayerData.instance.UnlockedFastTravelTeleport;
            }
        }

        private static void LogDetection(string key, string message)
        {
            if (Plugin.Instance != null && Plugin.Instance.DiagnosticLogging &&
                LoggedDetections.Count < 128 && LoggedDetections.Add(key))
            {
                Plugin.Log.LogInfo("[Beastling Call detection] " + message);
            }
        }

        private static bool ShouldLogPlayerDataVariable(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return false;
            }

            return HeroPerformanceRegion.IsPerforming ||
                variableName.Contains("FastTravel") ||
                variableName.Contains("Teleport") ||
                variableName.Contains("Centipede") ||
                variableName.Contains("Bell") ||
                variableName.Contains("Beast");
        }

        [HarmonyPatch(typeof(StagTravel), "UseChildrenVersion")]
        private static class StagTravelUseChildrenVersionPatch
        {
            private static void Postfix(ref bool __result)
            {
                var original = __result;
                var enabled = IsEnabled();
                var available = IsAvailable();
                if (!__result && available)
                {
                    __result = true;
                }

                LogDetection(
                    "StagTravel.UseChildrenVersion",
                    $"StagTravel.UseChildrenVersion original={original}, final={__result}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={enabled}, available={available}");
            }
        }

        [HarmonyPatch(typeof(PlayerData), "get_BellCentipedeWaiting")]
        private static class PlayerDataBellCentipedeWaitingPatch
        {
            private static void Postfix(ref bool __result)
            {
                var original = __result;
                var enabled = IsEnabled();
                var available = IsAvailable();
                if (__result && available)
                {
                    __result = false;
                }

                LogDetection(
                    "PlayerData.BellCentipedeWaiting",
                    $"PlayerData.BellCentipedeWaiting original={original}, final={__result}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={enabled}, available={available}");
            }
        }

        [HarmonyPatch(typeof(PlayerData), "get_BellCentipedeLocked")]
        private static class PlayerDataBellCentipedeLockedPatch
        {
            private static void Postfix(ref bool __result)
            {
                var original = __result;
                var enabled = IsEnabled();
                var available = IsAvailable();
                if (__result && available)
                {
                    __result = false;
                }

                LogDetection(
                    "PlayerData.BellCentipedeLocked",
                    $"PlayerData.BellCentipedeLocked original={original}, final={__result}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={enabled}, available={available}");
            }
        }

        [HarmonyPatch(typeof(OpenFastTravelUI), "OnEnter")]
        private static class OpenFastTravelUIOnEnterPatch
        {
            private static void Prefix(OpenFastTravelUI __instance)
            {
                var thisLocation = __instance?.ThisLocation?.Value;
                var hasSpawnedUi = __instance?.SpawnedUI?.Value != null;

                LogDetection(
                    $"OpenFastTravelUI.OnEnter.{thisLocation}.{hasSpawnedUi}",
                    $"OpenFastTravelUI.OnEnter thisLocation={thisLocation}, hasSpawnedUI={hasSpawnedUi}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={IsEnabled()}, available={IsAvailable()}");
            }
        }

        [HarmonyPatch(typeof(ControlNeedolin), "OnEnter")]
        private static class ControlNeedolinOnEnterPatch
        {
            private static void Prefix(ControlNeedolin __instance)
            {
                var requested = __instance?.isPlaying?.Value ?? false;
                LogDetection(
                    $"ControlNeedolin.OnEnter.{requested}.{Time.frameCount}",
                    $"ControlNeedolin.OnEnter requestedIsPlaying={requested}, beforeIsPerforming={HeroPerformanceRegion.IsPerforming}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={IsEnabled()}, available={IsAvailable()}");
            }
        }

        [HarmonyPatch(typeof(HeroPerformanceRegion), "SetIsPerforming")]
        private static class HeroPerformanceRegionSetIsPerformingPatch
        {
            private static void Prefix(bool value)
            {
                LogDetection(
                    $"HeroPerformanceRegion.SetIsPerforming.{value}.{Time.frameCount}",
                    $"HeroPerformanceRegion.SetIsPerforming value={value}, beforeIsPerforming={HeroPerformanceRegion.IsPerforming}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={IsEnabled()}, available={IsAvailable()}");
            }
        }

        [HarmonyPatch(typeof(GetPlayerDataVariable), "OnEnter")]
        private static class GetPlayerDataVariableOnEnterPatch
        {
            private static bool Prefix(GetPlayerDataVariable __instance)
            {
                var variableName = __instance?.VariableName?.Value;
                if (!ShouldLogPlayerDataVariable(variableName))
                {
                    return true;
                }

                LogDetection(
                    $"GetPlayerDataVariable.OnEnter.{variableName}.{Time.frameCount}",
                    $"GetPlayerDataVariable.OnEnter variable='{variableName}', type={__instance?.StoreValue?.RealType?.Name}, isPerforming={HeroPerformanceRegion.IsPerforming}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={IsEnabled()}, available={IsAvailable()}");

                if (IsAvailable() &&
                    variableName == "UnlockedFastTravelTeleport" &&
                    __instance != null &&
                    !__instance.StoreValue.IsNone)
                {
                    __instance.StoreValue.SetValue(true);
                    __instance.Finish();
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerDataVariableTest), "OnEnter")]
        private static class PlayerDataVariableTestOnEnterPatch
        {
            private static bool Prefix(PlayerDataVariableTest __instance)
            {
                var variableName = __instance?.VariableName?.Value;
                if (!ShouldLogPlayerDataVariable(variableName))
                {
                    return true;
                }

                LogDetection(
                    $"PlayerDataVariableTest.OnEnter.{variableName}.{Time.frameCount}",
                    $"PlayerDataVariableTest.OnEnter variable='{variableName}', expected={__instance?.ExpectedValue?.GetValue()}, type={__instance?.ExpectedValue?.RealType?.Name}, isPerforming={HeroPerformanceRegion.IsPerforming}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={IsEnabled()}, available={IsAvailable()}");

                if (IsAvailable() &&
                    variableName == "UnlockedFastTravelTeleport" &&
                    __instance != null &&
                    !__instance.ExpectedValue.IsNone)
                {
                    var expected = __instance.ExpectedValue.GetValue();
                    var isExpected = expected is bool expectedBool && expectedBool;
                    __instance.Fsm.Event(isExpected ? __instance.IsExpectedEvent : __instance.IsNotExpectedEvent);
                    __instance.Finish();

                    LogDetection(
                        $"PlayerDataVariableTest.Virtualized.{variableName}.{Time.frameCount}",
                        $"PlayerDataVariableTest virtualized variable='{variableName}' as True, expected={expected}, sent={(isExpected ? __instance.IsExpectedEvent?.Name : __instance.IsNotExpectedEvent?.Name)}");

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch]
        private static class FastTravelMapButtonIsUnlockedPatch
        {
            private static readonly FieldInfo PlayerDataBoolField =
                AccessTools.Field(typeof(FastTravelMapButtonBase<FastTravelLocations>), "playerDataBool");

            private static readonly FieldInfo TargetLocationField =
                AccessTools.Field(typeof(FastTravelMapButtonBase<FastTravelLocations>), "targetLocation");

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(FastTravelMapButtonBase<FastTravelLocations>), "IsUnlocked");
            }

            private static void Postfix(object __instance, ref bool __result)
            {
                var playerDataBool = PlayerDataBoolField?.GetValue(__instance) as string;
                var targetLocation = TargetLocationField?.GetValue(__instance);
                var original = __result;
                var enabled = IsEnabled();
                var available = IsAvailable();

                if (!__result &&
                    available &&
                    playerDataBool == "UnlockedFastTravelTeleport")
                {
                    __result = true;
                }

                LogDetection(
                    $"FastTravelMapButton.IsUnlocked.{targetLocation}.{playerDataBool}",
                    $"FastTravelMapButton.IsUnlocked target={targetLocation}, playerDataBool='{playerDataBool}', original={original}, final={__result}, rawUnlocked={RawUnlocked}, fastTravelUnlocked={PlayerData.instance?.UnlockedFastTravel}, modEnabled={enabled}, available={available}");
            }
        }
    }
}
