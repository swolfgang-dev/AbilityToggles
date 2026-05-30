using BepInEx.Configuration;
using GenericVariableExtension;
using HarmonyLib;
using System;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class ElegyOfTheDeepAbility
    {
        private const string PlayerDataField = "hasNeedolinMemoryPowerup";

        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalNeedolinMemories,
                "Elegy of the Deep",
                false,
                ConfigUi.GlobalDescription("Allow Elegy of the Deep for every save without setting PlayerData.hasNeedolinMemoryPowerup. Requires Needolin.", ConfigUi.ElegyOfTheDeepOrder));

            saveEnabled = saveConfig.Bind(
                ConfigUi.NeedolinMemories,
                "Elegy of the Deep",
                false,
                ConfigUi.SaveDescription("Allow Elegy of the Deep without setting PlayerData.hasNeedolinMemoryPowerup. Requires Needolin.", ConfigUi.ElegyOfTheDeepOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool IsAvailable()
        {
            if (!IsEnabled())
            {
                return false;
            }

            var hero = HeroController.instance;
            if (hero != null)
            {
                return hero.HasNeedolin();
            }

            return PlayerData.instance != null && PlayerData.instance.hasNeedolin;
        }

        [HarmonyPatch(typeof(VariableExtensionsGeneric), "GetVariable", new[] { typeof(object), typeof(string), typeof(Type) })]
        private static class VariableExtensionsGenericGetVariablePatch
        {
            private static void Postfix(string fieldName, Type type, ref object __result)
            {
                if (fieldName == PlayerDataField &&
                    type == typeof(bool) &&
                    __result is bool current &&
                    !current &&
                    IsAvailable())
                {
                    __result = true;
                }
            }
        }
    }
}
