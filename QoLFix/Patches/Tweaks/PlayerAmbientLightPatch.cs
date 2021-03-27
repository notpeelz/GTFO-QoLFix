using System;
using BepInEx.Configuration;
using Player;

namespace QoLFix.Patches.Tweaks
{
    public class PlayerAmbientLightPatch : Patch
    {
        private const string PatchName = nameof(PlayerAmbientLightPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigLightRange = new(PatchName, "LightRange");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription(
                $"This lets you alter the player ambient light range.\n"
                + "The ambient light is often the only source of light in dark areas. Careful not to set it too low!!\n"
                + "NOTE: for balance reasons, you can't increase it past the default vanilla value (10)."));
            QoLFixPlugin.Instance.Config.Bind(ConfigLightRange, 0f, new ConfigDescription("The range of the player's ambient light."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<PlayerAgent>(nameof(PlayerAgent.AquireAmbientPoint), PatchType.Postfix);
        }

        private static void PlayerAgent__AquireAmbientPoint__Postfix(PlayerAgent __instance)
        {
            var range = ConfigLightRange.GetConfigEntry<float>().Value;
            range = Math.Clamp(range, 0, __instance.m_ambienceLight.range);
            __instance.m_ambienceLight.range = range;
            __instance.m_ambientPoint.SetRange(range);
        }
    }
}
