using BepInEx.Configuration;
using HarmonyLib;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class HarpoonDashAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Clawline",
                false,
                ConfigUi.GlobalDescription("Allow Clawline for every save without setting PlayerData.hasHarpoonDash.", ConfigUi.ClawlineOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Clawline",
                false,
                ConfigUi.SaveDescription("Allow Clawline without setting PlayerData.hasHarpoonDash.", ConfigUi.ClawlineOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        [HarmonyPatch(typeof(HeroController), "HasHarpoonDash")]
        private static class HeroControllerHasHarpoonDashPatch
        {
            private static void Postfix(HeroController __instance, ref bool __result)
            {
                if (!__result && IsEnabled() && __instance != null && __instance.Config.CanHarpoonDash)
                {
                    __result = true;
                }
            }
        }
    }
}
