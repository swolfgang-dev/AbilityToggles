using BepInEx.Configuration;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class ConfigUi
    {
        public const string GlobalSilkSkills = "Global: Silk Skills";
        public const string GlobalAncestralArts = "Global: Ancestral Arts";
        public const string GlobalNeedolinMemories = "Global: Needolin Memories";
        public const string GlobalOtherAbilities = "Global: Other Abilities";
        public const string GlobalAdditionalOptions = "Global: Additional Options";

        public const string SilkSkills = "Silk Skills";
        public const string AncestralArts = "Ancestral Arts";
        public const string NeedolinMemories = "Needolin Memories";
        public const string OtherAbilities = "Other Abilities";
        public const string AdditionalOptions = "Additional Options";

        public const int SilkspearOrder = 5000;
        public const int ThreadStormOrder = 5001;
        public const int CrossStitchOrder = 5002;
        public const int SharpdartOrder = 5003;
        public const int RuneRageOrder = 5004;
        public const int PaleNailsOrder = 5005;

        public const int SwiftStepOrder = 4000;
        public const int ClingGripOrder = 4001;
        public const int NeedolinOrder = 4002;
        public const int ClawlineOrder = 4003;
        public const int SilkSoarOrder = 4004;
        public const int SylphsongOrder = 4005;

        public const int BeastlingCallOrder = 3000;
        public const int ElegyOfTheDeepOrder = 3001;

        public const int NeedleStrikeOrder = 2000;
        public const int DriftersCloakOrder = 2002;
        public const int FaydownCloakOrder = 2003;

        public const int BindDriftersCloakOrder = 1000;

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
