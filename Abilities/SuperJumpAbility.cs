using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class SuperJumpAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Silk Soar",
                false,
                ConfigUi.GlobalDescription("Allow Silk Soar for every save without setting PlayerData.hasSuperJump.", ConfigUi.SilkSoarOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Silk Soar",
                false,
                ConfigUi.SaveDescription("Allow Silk Soar without setting PlayerData.hasSuperJump.", ConfigUi.SilkSoarOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool HasSuperJumpAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasSuperJump) || IsEnabled();
        }

        private static IEnumerable<CodeInstruction> ReplaceHasSuperJumpChecks(IEnumerable<CodeInstruction> instructions)
        {
            var hasSuperJumpField = AccessTools.Field(typeof(PlayerData), "hasSuperJump");
            var replacement = AccessTools.Method(typeof(SuperJumpAbility), nameof(HasSuperJumpAbility));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasSuperJumpField))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacement);
                    continue;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(HeroController), "CanSuperJump")]
        private static class HeroControllerCanSuperJumpPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceHasSuperJumpChecks(instructions);
            }
        }
    }
}
