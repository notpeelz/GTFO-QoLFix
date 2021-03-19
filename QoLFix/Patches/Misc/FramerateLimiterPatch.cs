using System;
using BepInEx.Configuration;
using UnityEngine;

namespace QoLFix.Patches.Misc
{
    public class FramerateLimiterPatch : Patch
    {
        private const string PatchName = nameof(FramerateLimiterPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigMaxFPS = new(PatchName, "MaxFPS");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lowers your FPS when alt-tabbing to preserve system resources."));
            QoLFixPlugin.Instance.Config.Bind(ConfigMaxFPS, 30, new ConfigDescription("The maximum FPS to use when the game is out of focus."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            this.PatchMethod<CellSettingsManager>(nameof(CellSettingsManager.OnApplicationFocus), PatchType.Postfix);
        }

        private static bool WarningShown;
        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            if (QualitySettings.vSyncCount != 0 && !WarningShown)
            {
                Instance.LogWarning("FPS limiting doesn't work with v-sync; this will have no effect.");
                WarningShown = true;
            }

            if (!focus)
            {
                var maxFPS = QoLFixPlugin.Instance.Config.GetConfigEntry<int>(ConfigMaxFPS).Value;
                Instance.LogDebug("Limiting FPS while out of focus");
                Application.targetFrameRate = Math.Clamp(maxFPS, -1, CellSettingsManager.SettingsData.Video.TargetFramerate.Value);
                return;
            }

            Instance.LogDebug("Restoring target FPS");
            CellSettingsManager.SettingsData.Video.TargetFramerate.ApplyValue();
        }
    }
}
