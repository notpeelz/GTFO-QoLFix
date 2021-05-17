using System;
using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using UnityEngine;

namespace QoL.FPSLimiter
{
    public class FPSLimiterPatch : MTFOPatch
    {
        private const string PatchName = nameof(FPSLimiterPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;
        private static ConfigEntry<int> ConfigMaxFPS = default!;

        public static FPSLimiterPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Lowers your FPS when alt-tabbing to preserve system resources."));
            ConfigMaxFPS = this.Plugin.Config.Bind(new(PatchName, "MaxFPS"), 30,
                new ConfigDescription("The maximum FPS to use when the game is out of focus."));
        }

        private static bool WarningShown;

        [HarmonyPatch(typeof(CellSettingsManager))]
        [HarmonyPatch(nameof(CellSettingsManager.OnApplicationFocus))]
        [HarmonyPostfix]
        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            if (QualitySettings.vSyncCount != 0 && !WarningShown)
            {
                Instance!.LogWarning("FPS limiting doesn't work with v-sync; this will have no effect.");
                WarningShown = true;
            }

            if (!focus)
            {
                Instance!.LogDebug("Limiting FPS while out of focus");
                var maxFPS = ConfigMaxFPS.Value;
                var targetFPS = CellSettingsManager.SettingsData.Video.TargetFramerate.Value;
                Application.targetFrameRate = Math.Clamp(maxFPS, -1, targetFPS < 0 ? 999 : targetFPS);
                return;
            }

            Instance!.LogDebug("Restoring target FPS");
            CellSettingsManager.SettingsData.Video.TargetFramerate.ApplyValue();
        }
    }
}
