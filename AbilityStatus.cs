using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using HarmonyLib;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    public sealed partial class Plugin
    {
        private string compatibilityStatus = "Starting";
        private ConfigEntry<bool> diagnosticLogging;
        internal bool DiagnosticLogging => diagnosticLogging != null && diagnosticLogging.Value;

        private void BindStatusControls()
        {
            diagnosticLogging = Config.Bind("Troubleshooting", "Debug Logging", false,
                "Enable bounded diagnostic logging. Keep disabled during normal play.");
            ModSettingsUi.BindPanel(Config, "Effective Abilities", () =>
            {
                ModSettingsUi.Label(compatibilityStatus);
                ModSettingsUi.Label(GetAbilityStatus());
            }, "Shows normal acquisition and the global/save source of each virtual ability. Normal gameplay prerequisites still apply.");
        }

        private bool EnabledIn(string section, string key)
        {
            ConfigEntry<bool> entry;
            return Config.TryGetEntry(new ConfigDefinition(section, key), out entry) && entry.Value;
        }

        private bool RawFlag(PlayerData pd, string name)
        {
            if (pd == null || name == null) return false;
            FieldInfo field = AccessTools.Field(typeof(PlayerData), name);
            return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(pd);
        }

        private void AppendAbility(StringBuilder text, PlayerData pd, string category, string key, string flag, string prerequisite = null, bool? acquired = null)
        {
            bool global = EnabledIn("Global: " + category, key);
            bool save = EnabledIn(SaveScopedConfig.ActiveSaveKey + ": " + category, key);
            text.AppendLine(key + ": " + ModSettingsUi.ExplainEnablement(acquired ?? RawFlag(pd, flag), global, save) +
                ((global || save) && prerequisite != null ? "; " + prerequisite : string.Empty));
        }

        internal string GetAbilityStatus()
        {
            if (!SaveScopedConfig.HasActiveSave || GameManager.instance == null) return "Load a save to inspect abilities.";
            var pd = GameManager.instance.playerData;
            var text = new StringBuilder();
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Swift Step", "hasDash");
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Cling Grip", "hasWalljump");
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Needolin", "hasNeedolin");
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Clawline", "hasHarpoonDash");
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Silk Soar", "hasSuperJump");
            AppendAbility(text, pd, ConfigUi.AncestralArts, "Sylphsong", null, "regenerates silk while resting at a bench",
                pd != null && pd.silkRegenMax >= pd.CurrentSilkMax);
            AppendAbility(text, pd, ConfigUi.OtherAbilities, "Needle Strike", "hasChargeSlash");
            AppendAbility(text, pd, ConfigUi.OtherAbilities, "Drifters Cloak", "hasBrolly");
            AppendAbility(text, pd, ConfigUi.OtherAbilities, "Faydown Cloak", "hasDoubleJump");
            AppendAbility(text, pd, ConfigUi.NeedolinMemories, "Beastling Call", "UnlockedFastTravelTeleport",
                pd != null && pd.UnlockedFastTravel ? "fast-travel prerequisite met" : "waiting for the normal fast-travel unlock");
            bool needolin = RawFlag(pd, "hasNeedolin") || EnabledIn(ConfigUi.GlobalAncestralArts, "Needolin") ||
                EnabledIn(SaveScopedConfig.ActiveSaveKey + ": " + ConfigUi.AncestralArts, "Needolin");
            AppendAbility(text, pd, ConfigUi.NeedolinMemories, "Elegy of the Deep", "hasNeedolinMemoryPowerup",
                needolin ? "Needolin prerequisite met" : "waiting for Needolin");
            string[] keys = { "Silkspear", "Thread Storm", "Cross Stitch", "Sharpdart", "Rune Rage", "Pale Nails" };
            string[] flags = { "hasNeedleThrow", "hasThreadSphere", "hasParry", "hasSilkCharge", "hasSilkBomb", "hasSilkBossNeedle" };
            for (int i = 0; i < keys.Length; i++) AppendAbility(text, pd, ConfigUi.SilkSkills, keys[i], flags[i]);
            text.Append("Global enables remain active when a save toggle is off. These settings never revoke normal acquisition.");
            return text.ToString();
        }
    }
}
