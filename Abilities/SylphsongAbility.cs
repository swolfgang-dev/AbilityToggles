using BepInEx.Configuration;
using GlobalSettings;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using WheresWolfgang.SaveScopedConfig;

namespace AbilitiesUnlocked
{
    internal static class SylphsongAbility
    {
        private const float BenchSettleDelay = 0.05f;
        private const float MinDelayBeforeRegen = 1.65f;
        private const float SilkRegenInterval = 0.25f;

        private static ConfigEntry<bool> globalEnabled;
        private static SaveScopedConfigEntry<bool> saveEnabled;

        private static Coroutine benchRegenCoroutine;
        private static bool waitedForBenchRestore;

        public static void Bind(ConfigFile config, SaveScopedConfigFile saveConfig)
        {
            globalEnabled = config.Bind(
                ConfigUi.GlobalAncestralArts,
                "Sylphsong",
                false,
                ConfigUi.GlobalDescription("Regenerate silk over time while resting on a bench for every save without setting progression flags.", ConfigUi.SylphsongOrder));

            saveEnabled = saveConfig.Bind(
                ConfigUi.AncestralArts,
                "Sylphsong",
                false,
                ConfigUi.SaveDescription("Regenerate silk over time while resting on a bench without setting progression flags.", ConfigUi.SylphsongOrder));
        }

        private static bool Enabled
        {
            get
            {
                return (globalEnabled != null && globalEnabled.Value) ||
                    (saveEnabled != null && saveEnabled.Value);
            }
        }

        private static bool CanRun(PlayerData playerData)
        {
            return Enabled &&
                playerData != null &&
                playerData.atBench &&
                playerData.silk < playerData.CurrentSilkMax &&
                GetProgressionSilkRegenMax(playerData) < playerData.CurrentSilkMax;
        }

        private static int GetProgressionSilkRegenMax(PlayerData playerData)
        {
            if (playerData == null)
            {
                return 0;
            }

            var regenMax = playerData.silkRegenMax;
            if (Gameplay.WhiteRingTool.IsEquipped)
            {
                regenMax += Gameplay.WhiteRingSilkRegenIncrease;
            }

            return regenMax;
        }

        private static void StartBenchRegen()
        {
            if (benchRegenCoroutine != null || Plugin.Instance == null)
            {
                return;
            }

            benchRegenCoroutine = Plugin.Instance.StartCoroutine(BenchRegenRoutine());
        }

        private static void StopBenchRegen()
        {
            if (benchRegenCoroutine != null && Plugin.Instance != null)
            {
                Plugin.Instance.StopCoroutine(benchRegenCoroutine);
            }

            benchRegenCoroutine = null;
        }

        private static void ResetBenchState()
        {
            waitedForBenchRestore = false;
            StopBenchRegen();
        }

        private static IEnumerator BenchRegenRoutine()
        {
            if (!waitedForBenchRestore)
            {
                waitedForBenchRestore = true;

                yield return new WaitForSeconds(BenchSettleDelay);
                yield return WaitBeforeRegen();
            }

            yield return RegenSilkUntilFull();
            benchRegenCoroutine = null;
        }

        private static IEnumerator WaitBeforeRegen()
        {
            var waitUntil = Time.time + MinDelayBeforeRegen;

            while (Time.time < waitUntil && CanRun(PlayerData.instance))
            {
                yield return null;
            }
        }

        private static IEnumerator RegenSilkUntilFull()
        {
            var tickDelay = new WaitForSeconds(SilkRegenInterval);

            while (CanRun(PlayerData.instance))
            {
                AddSilkPip();
                yield return tickDelay;
            }
        }

        private static void AddSilkPip()
        {
            var hero = HeroController.instance;
            if (hero == null)
            {
                return;
            }

            hero.AddSilk(1, false);
            EventRegister.SendEvent(EventRegisterEvents.RegeneratedSilkChunk, null);
        }

        [HarmonyPatch(typeof(HeroController), "Update")]
        private static class HeroControllerUpdatePatch
        {
            private static void Postfix()
            {
                var playerData = PlayerData.instance;
                if (playerData == null || !playerData.atBench)
                {
                    ResetBenchState();
                    return;
                }

                if (CanRun(playerData))
                {
                    StartBenchRegen();
                    return;
                }

                StopBenchRegen();
            }
        }
    }
}
