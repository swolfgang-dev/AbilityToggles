using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class DashAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Swift Step",
                false,
                ConfigUi.GlobalDescription("Allow Swift Step for every save without setting PlayerData.hasDash.", ConfigUi.SwiftStepOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Swift Step",
                false,
                ConfigUi.SaveDescription("Allow Swift Step without setting PlayerData.hasDash.", ConfigUi.SwiftStepOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool HasDashAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasDash) || IsEnabled();
        }

        private static IEnumerable<CodeInstruction> ReplaceHasDashChecks(IEnumerable<CodeInstruction> instructions)
        {
            var hasDashField = AccessTools.Field(typeof(PlayerData), "hasDash");
            var replacement = AccessTools.Method(typeof(DashAbility), nameof(HasDashAbility));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasDashField))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanDash")]
        private static class HeroControllerCanDashPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasDashChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "GetCanAirDashCancel")]
        private static class HeroControllerGetCanAirDashCancelPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasDashChecks(instructions);
            }
        }
    }
}
