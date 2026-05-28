using BepInEx.Configuration;
using HarmonyLib;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class NeedolinAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Needolin",
                false,
                ConfigUi.GlobalDescription("Allow Needolin for every save without setting PlayerData.hasNeedolin.", ConfigUi.NeedolinOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Needolin",
                false,
                ConfigUi.SaveDescription("Allow Needolin without setting PlayerData.hasNeedolin.", ConfigUi.NeedolinOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        [HarmonyPatch(typeof(HeroController), "HasNeedolin")]
        private static class HeroControllerHasNeedolinPatch
        {
            private static void Postfix(HeroController __instance, ref bool __result)
            {
                if (!__result && IsEnabled() && __instance != null && __instance.Config.CanPlayNeedolin)
                {
                    __result = true;
                }
            }
        }
    }
}
