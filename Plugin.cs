using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using WheresWolfgang.SaveScopedConfig;

namespace AbilityToggles
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(SaveScopedConfigPlugin.PluginGuid)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "whereswolfgang.abilitytoggles";
        public const string PluginName = "Ability Toggles";
        public const string PluginVersion = "0.1.0";

        private static readonly Harmony Harmony = new Harmony(PluginGuid);
        private SaveScopedConfigFile saveConfig;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            saveConfig = SaveScopedConfig.For(this);

            SilkSkillsAbility.Bind(Config, saveConfig);
            DashAbility.Bind(Config, saveConfig);
            WalljumpAbility.Bind(Config, saveConfig);
            NeedolinAbility.Bind(Config, saveConfig);
            HarpoonDashAbility.Bind(Config, saveConfig);
            SuperJumpAbility.Bind(Config, saveConfig);
            SylphsongAbility.Bind(Config, saveConfig);
            BeastlingCallAbility.Bind(Config, saveConfig);
            ElegyOfTheDeepAbility.Bind(Config, saveConfig);
            ChargeSlashAbility.Bind(Config, saveConfig);
            BrollyAbility.Bind(Config, saveConfig);
            DoubleJumpAbility.Bind(Config, saveConfig);

            Harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(PluginName + " loaded.");
        }

        private void OnDestroy()
        {
            if (saveConfig != null)
            {
                saveConfig.Dispose();
                saveConfig = null;
            }

            Harmony.UnpatchSelf();
            Instance = null;
            Log = null;
        }
    }
}
