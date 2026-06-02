using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class DoubleJumpAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalOtherAbilities,
                "Faydown Cloak",
                false,
                ConfigUi.GlobalDescription("Allow Faydown Cloak for every save without setting PlayerData.hasDoubleJump.", ConfigUi.FaydownCloakOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.OtherAbilities,
                "Faydown Cloak",
                false,
                ConfigUi.SaveDescription("Allow Faydown Cloak without setting PlayerData.hasDoubleJump.", ConfigUi.FaydownCloakOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool HasDoubleJumpAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasDoubleJump) || IsEnabled();
        }

        private static IEnumerable<CodeInstruction> ReplaceHasDoubleJumpChecks(IEnumerable<CodeInstruction> instructions)
        {
            var hasDoubleJumpField = AccessTools.Field(typeof(PlayerData), "hasDoubleJump");
            var replacement = AccessTools.Method(typeof(DoubleJumpAbility), nameof(HasDoubleJumpAbility));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasDoubleJumpField))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanDoubleJump")]
        private static class HeroControllerCanDoubleJumpPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasDoubleJumpChecks(instructions);
            }
        }

        [HarmonyPatch(typeof(HeroController), "RegainControl", typeof(bool))]
        private static class HeroControllerRegainControlPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasDoubleJumpChecks(instructions);
            }
        }
    }
}
