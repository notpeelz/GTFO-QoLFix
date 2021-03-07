using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public class NoiseRemovalPatch : IPatch
    {
        private const string PatchName = nameof(NoiseRemovalPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Disables the blue noise shader. This makes the game look clearer, although some areas might look a lot darker than normal."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PE_BlueNoise>(nameof(PE_BlueNoise.Update), PatchType.Prefix);
        }

        private static Texture EmptyTexture;

        private static bool PE_BlueNoise__Update__Prefix()
        {
            if (PE_BlueNoise.s_computeShader == null) return false;
            if (EmptyTexture == null)
            {
                EmptyTexture = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            }
            Shader.SetGlobalTexture("_PE_BlueNoise", EmptyTexture);
            PE_BlueNoise.s_computeShader.SetTexture(0, "_PE_BlueNoise", EmptyTexture);
            return HarmonyControlFlow.DontExecute;
        }
    }
}
