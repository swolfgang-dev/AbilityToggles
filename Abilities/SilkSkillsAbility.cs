using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    internal static class SilkSkillsAbility
    {
        private static readonly SilkSkillDefinition[] Skills =
        {
            new SilkSkillDefinition("Silk Spear", "Silkspear", ConfigUi.SilkspearOrder, "hasNeedleThrow"),
            new SilkSkillDefinition("Thread Sphere", "Thread Storm", ConfigUi.ThreadStormOrder, "hasThreadSphere"),
            new SilkSkillDefinition("Parry", "Cross Stitch", ConfigUi.CrossStitchOrder, "hasParry"),
            new SilkSkillDefinition("Silk Charge", "Sharpdart", ConfigUi.SharpdartOrder, "hasSilkCharge"),
            new SilkSkillDefinition("Silk Bomb", "Rune Rage", ConfigUi.RuneRageOrder, "hasSilkBomb"),
            new SilkSkillDefinition("Silk Boss Needle", "Pale Nails", ConfigUi.PaleNailsOrder, "hasSilkBossNeedle"),
        };

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            foreach (SilkSkillDefinition skill in Skills)
            {
                skill.GlobalEnabled = config.Bind(
                    ConfigUi.GlobalSilkSkills,
                    skill.DisplayName,
                    false,
                    ConfigUi.GlobalDescription("Allow " + skill.DisplayName + " for every save without setting PlayerData." + skill.PlayerDataField + ".", skill.Order));

                skill.SaveEnabled = saveConfig.Bind(
                    ConfigUi.SilkSkills,
                    skill.DisplayName,
                    false,
                    ConfigUi.SaveDescription("Allow " + skill.DisplayName + " without setting PlayerData." + skill.PlayerDataField + ".", skill.Order));

                skill.GlobalEnabled.SettingChanged += OnConfigChanged;
                skill.SaveEnabled.SettingChanged += OnConfigChanged;
            }
        }

        private static void OnConfigChanged(object sender, EventArgs args)
        {
            RemoveDisabledVirtualSkillsFromCrests();
            RefreshToolState();
        }

        private static bool IsEnabled(SilkSkillDefinition skill)
        {
            return (skill.GlobalEnabled != null && skill.GlobalEnabled.Value) ||
                (skill.SaveEnabled != null && skill.SaveEnabled.Value);
        }

        private static bool IsActualProgressionUnlocked(SilkSkillDefinition skill)
        {
            var playerData = PlayerData.instance;
            if (playerData == null)
            {
                return false;
            }

            FieldInfo field = AccessTools.Field(typeof(PlayerData), skill.PlayerDataField);
            return field != null && (bool)field.GetValue(playerData);
        }

        private static bool ShouldVirtualize(ToolItem tool)
        {
            SilkSkillDefinition skill = GetSkill(tool);
            return skill != null && IsEnabled(skill);
        }

        private static bool ShouldHideFromToolList(ToolItem tool)
        {
            SilkSkillDefinition skill = GetSkill(tool);
            return skill != null && !IsEnabled(skill) && !IsActualProgressionUnlocked(skill);
        }

        private static SilkSkillDefinition GetSkill(ToolItem tool)
        {
            if (!tool)
            {
                return null;
            }

            for (int i = 0; i < Skills.Length; i++)
            {
                if (tool.name == Skills[i].ToolName)
                {
                    return Skills[i];
                }
            }

            return null;
        }

        private static void RemoveDisabledVirtualSkillsFromCrests()
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                SilkSkillDefinition skill = Skills[i];
                if (IsEnabled(skill) || IsActualProgressionUnlocked(skill))
                {
                    continue;
                }

                ToolItem tool = ToolItemManager.GetToolByName(skill.ToolName);
                if (tool)
                {
                    ToolItemManager.RemoveToolFromAllCrests(tool);
                    RemoveToolFromExtraSlots(tool);
                }
            }
        }

        private static void RemoveToolFromExtraSlots(ToolItem tool)
        {
            var playerData = PlayerData.instance;
            if (!tool || playerData == null)
            {
                return;
            }

            bool removedAny = false;
            foreach (string slotId in playerData.ExtraToolEquips.GetValidNames(null))
            {
                ToolCrestsData.SlotData data = playerData.ExtraToolEquips.GetData(slotId);
                if (data.EquippedTool != tool.name)
                {
                    continue;
                }

                data.EquippedTool = string.Empty;
                playerData.ExtraToolEquips.SetData(slotId, data);
                removedAny = true;
            }

            if (removedAny)
            {
                ToolItemManager.RefreshEquippedState();
                ToolItemManager.ReportAllBoundAttackToolsUpdated();
                ToolItemManager.SendEquippedChangedEvent(true);
            }
        }

        private static void RefreshToolState()
        {
            ToolItemManager.RefreshEquippedState();
            ToolItemManager.ReportAllBoundAttackToolsUpdated();
            ToolItemManager.SendEquippedChangedEvent(true);
        }

        [HarmonyPatch(typeof(ToolItem), "IsUnlocked", MethodType.Getter)]
        private static class ToolItemIsUnlockedPatch
        {
            private static void Postfix(ToolItem __instance, ref bool __result)
            {
                if (!__result && ShouldVirtualize(__instance))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(ToolItem), "IsUnlockedNotHidden", MethodType.Getter)]
        private static class ToolItemIsUnlockedNotHiddenPatch
        {
            private static void Postfix(ToolItem __instance, ref bool __result)
            {
                if (!__result && ShouldVirtualize(__instance))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryItemToolManager), "GetItems")]
        private static class InventoryItemToolManagerGetItemsPatch
        {
            private static void Postfix(List<ToolItem> __result)
            {
                if (__result == null)
                {
                    return;
                }

                for (int i = __result.Count - 1; i >= 0; i--)
                {
                    if (ShouldHideFromToolList(__result[i]))
                    {
                        __result.RemoveAt(i);
                    }
                }
            }
        }

        private sealed class SilkSkillDefinition
        {
            public SilkSkillDefinition(string toolName, string displayName, int order, string playerDataField)
            {
                ToolName = toolName;
                DisplayName = displayName;
                Order = order;
                PlayerDataField = playerDataField;
            }

            public string ToolName { get; private set; }
            public string DisplayName { get; private set; }
            public int Order { get; private set; }
            public string PlayerDataField { get; private set; }
            public ConfigEntry<bool> GlobalEnabled { get; set; }
            public SaveScopedConfigEntry<bool> SaveEnabled { get; set; }
        }
    }
}
