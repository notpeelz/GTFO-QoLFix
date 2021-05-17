using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using UnityEngine;

namespace QoL.BugFixes
{
    public class VelocityBugFixPatch : MTFOPatch
    {
        private const string PatchName = nameof(VelocityBugFixPatch);

        private const float RESET_DELAY = 15f;

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static VelocityBugFixPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Fixes the bug where you would lose all horizontal velocity while bunny-hopping."));
        }

        private static Vector3 HorizontalVelocity;

        [HarmonyPatch(typeof(PLOC_Jump))]
        [HarmonyPatch(nameof(PLOC_Jump.Exit))]
        [HarmonyPrefix]
        private static void PLOC_Jump__Exit__Prefix(PLOC_Jump __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            HorizontalVelocity = __instance.m_owner.Locomotion.HorizontalVelocity;
        }

        [HarmonyPatch(typeof(PLOC_Jump))]
        [HarmonyPatch(nameof(PLOC_Jump.Exit))]
        [HarmonyPostfix]
        private static void PLOC_Jump__Exit__Postfix(PLOC_Jump __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            __instance.m_owner.Locomotion.HorizontalVelocity = HorizontalVelocity;
        }
    }
}
