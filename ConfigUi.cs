using BepInEx.Configuration;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class ConfigUi
    {
        public const string GlobalAncestralArts = "Global: Ancestral Arts";
        public const string GlobalSilkSkills = "Global: Silk Skills";
        public const string GlobalNeedolinMelodies = "Global: Needolin Melodies";
        public const string GlobalOtherAbilities = "Global: Other Abilities";
        public const string GlobalAdditionalOptions = "Global: Additional Options";

        public const string AncestralArts = "Ancestral Arts";
        public const string SilkSkills = "Silk Skills";
        public const string NeedolinMelodies = "Needolin Melodies";
        public const string OtherAbilities = "Other Abilities";
        public const string AdditionalOptions = "Additional Options";

        public const int SilkspearOrder = 0;
        public const int ThreadStormOrder = 1;
        public const int CrossStitchOrder = 2;
        public const int SharpdartOrder = 3;
        public const int RuneRageOrder = 4;
        public const int PaleNailsOrder = 5;

        public const int SwiftStepOrder = 100;
        public const int ClingGripOrder = 101;
        public const int NeedolinOrder = 102;
        public const int ClawlineOrder = 103;
        public const int SilkSoarOrder = 104;
        public const int SylphsongOrder = 105;

        public const int BeastlingCallOrder = 200;
        public const int ElegyOfTheDeepOrder = 201;

        public const int NeedleStrikeOrder = 300;
        public const int DriftersCloakOrder = 302;
        public const int FaydownCloakOrder = 303;

        public const int BindDriftersCloakOrder = 400;

        public static ConfigDescription GlobalDescription(string description, int order)
        {
            return GlobalDescription(description, order, null);
        }

        public static ConfigDescription GlobalDescription(string description, int order, string displayName)
        {
            return new ConfigDescription(
                description,
                null,
                new ConfigurationManagerAttributes { DispName = displayName, Order = order });
        }

        public static SaveScopedConfigDescription SaveDescription(string description, int order)
        {
            return SaveDescription(description, order, null);
        }

        public static SaveScopedConfigDescription SaveDescription(string description, int order, string displayName)
        {
            var configDescription = new SaveScopedConfigDescription(description)
            {
                Order = () => order
            };

            if (!string.IsNullOrEmpty(displayName))
            {
                configDescription.DisplayName = () => displayName;
            }

            return configDescription;
        }
    }
}
