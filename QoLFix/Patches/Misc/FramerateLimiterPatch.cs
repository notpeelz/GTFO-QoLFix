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

        private static void CellSettingsManager__OnApplicationFocus__Postfix(bool focus)
        {
            if (!focus)
            {
                var maxFPS = QoLFixPlugin.Instance.Config.GetConfigEntry<int>(ConfigMaxFPS).Value;
                Application.targetFrameRate = Math.Clamp(maxFPS, -1, 999);
                return;
            }

            CellSettingsManager.SettingsData.Video.TargetFramerate.ApplyValue();
        }
    }
}
