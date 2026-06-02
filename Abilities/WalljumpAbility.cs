using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class WalljumpAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Cling Grip",
                false,
                ConfigUi.GlobalDescription("Allow Cling Grip for every save without setting PlayerData.hasWalljump.", ConfigUi.ClingGripOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Cling Grip",
                false,
                ConfigUi.SaveDescription("Allow Cling Grip without setting PlayerData.hasWalljump.", ConfigUi.ClingGripOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool HasWalljumpAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasWalljump) || IsEnabled();
        }

        private static IEnumerable<CodeInstruction> ReplaceHasWalljumpChecks(IEnumerable<CodeInstruction> instructions)
        {
            var hasWalljumpField = AccessTools.Field(typeof(PlayerData), "hasWalljump");
            var replacement = AccessTools.Method(typeof(WalljumpAbility), nameof(HasWalljumpAbility));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasWalljumpField))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(HeroController), "IsFacingNearSlideableWall")]
        private static class HeroControllerIsFacingNearSlideableWallPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasWalljumpChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "TryQueueWallJumpInterrupt")]
        private static class HeroControllerTryQueueWallJumpInterruptPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasWalljumpChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanWallJump")]
        private static class HeroControllerCanWallJumpPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasWalljumpChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanWallScramble")]
        private static class HeroControllerCanWallScramblePatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasWalljumpChecks(instructions);
            }
        }
    }
}
