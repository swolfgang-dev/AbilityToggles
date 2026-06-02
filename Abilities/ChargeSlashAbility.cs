using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class ChargeSlashAbility
    {
        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalOtherAbilities,
                "Needle Strike",
                false,
                ConfigUi.GlobalDescription("Allow Needle Strike for every save without setting PlayerData.hasChargeSlash.", ConfigUi.NeedleStrikeOrder));
            saveEnabled = saveConfig.Bind(
                ConfigUi.OtherAbilities,
                "Needle Strike",
                false,
                ConfigUi.SaveDescription("Allow Needle Strike without setting PlayerData.hasChargeSlash.", ConfigUi.NeedleStrikeOrder));
        }

        private static bool IsEnabled()
        {
            return (globalEnabled != null && globalEnabled.Value) ||
                (saveEnabled != null && saveEnabled.Value);
        }

        private static bool HasChargeSlashAbility(PlayerData playerData)
        {
            return (playerData != null && playerData.hasChargeSlash) || IsEnabled();
        }

        [HarmonyPatch(typeof(HeroController), "CanNailCharge")]
        private static class HeroControllerCanNailChargePatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var hasChargeSlashField = AccessTools.Field(typeof(PlayerData), "hasChargeSlash");
                var replacement = AccessTools.Method(typeof(ChargeSlashAbility), nameof(HasChargeSlashAbility));

                foreach (var instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, hasChargeSlashField))
                    {
                        yield return new CodeInstruction(OpCodes.Call, replacement);
                        continue;
                    }

                    yield return instruction;
                }
            }
        }
    }
}
