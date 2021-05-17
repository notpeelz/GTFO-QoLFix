using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using QoL.Common.Patches;

namespace QoL.BugFixes
{
    public class WeaponAnimationBugFixPatch : MTFOPatch
    {
        private const string PatchName = nameof(WeaponAnimationBugFixPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static WeaponAnimationBugFixPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Fixes the bug where animation sequences would carry over to other items when switching weapons too early (e.g. the reload animation)."));
        }

        [HarmonyPatch(typeof(ItemEquippable))]
        [HarmonyPatch(nameof(ItemEquippable.OnUnWield))]
        [HarmonyPrefix]
        private static void ItemEquippable__OnUnWield__Prefix(ItemEquippable __instance)
        {
            if (__instance.Owner?.IsLocallyOwned != true) return;
            ItemEquippableAnimationSequencePatch.StopAnimation();
        }

        [HarmonyPatch(typeof(ItemEquippable))]
        [HarmonyPatch(nameof(ItemEquippable.OnWield))]
        [HarmonyPrefix]
        private static void ItemEquippable__OnWield__Prefix(ItemEquippable __instance)
        {
            if (__instance.Owner?.IsLocallyOwned != true) return;
            ItemEquippableAnimationSequencePatch.StopAnimation();
        }

        [HarmonyPatch(typeof(ItemEquippable))]
        [HarmonyPatch(nameof(ItemEquippable.OnWield))]
        [HarmonyPostfix]
        private static void ItemEquippable__OnWield__Postfix(ItemEquippable __instance)
        {
            if (__instance.Owner?.IsLocallyOwned != true) return;

            var fpItemHolder = __instance.Owner.FPItemHolder;

            if (__instance.Owner.Locomotion.IsRunning)
            {
                var animator = fpItemHolder.m_fpsItemAnim;
                var animName = __instance.ItemFPSData?.runAnimData?.Anim;
                if (animName != null)
                {
                    animator?.CrossFadeInFixedTime(animName, fpItemHolder.m_itemRefTransTime, 0);
                }
            }
            else
            {
                fpItemHolder.OnIdleStart();
            }
        }
    }
}
