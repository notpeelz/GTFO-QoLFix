using BepInEx.Configuration;
using QoLFix.Patches.Misc;

namespace QoLFix.Patches.Bugfixes
{
    public class FixWeaponAnimationsPatch : Patch
    {
        private const string PatchName = nameof(FixWeaponAnimationsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where animation sequences would carry over to other items when switching weapons too early (e.g. the reload animation)."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            QoLFixPlugin.RegisterPatch<ItemEquippableAnimationSequencePatch>();
            this.PatchMethod<ItemEquippable>(nameof(ItemEquippable.OnUnWield), PatchType.Prefix);
            this.PatchMethod<ItemEquippable>(nameof(ItemEquippable.OnWield), PatchType.Both);
        }
        private static void ItemEquippable__OnUnWield__Prefix(ItemEquippable __instance)
        {
            if (!__instance.Owner?.IsLocallyOwned == true) return;
            ItemEquippableAnimationSequencePatch.StopAnimation();
        }

        private static void ItemEquippable__OnWield__Prefix(ItemEquippable __instance)
        {
            if (!__instance.Owner?.IsLocallyOwned == true) return;
            ItemEquippableAnimationSequencePatch.StopAnimation(__instance);
        }

        private static void ItemEquippable__OnWield__Postfix(ItemEquippable __instance)
        {
            if (!__instance.Owner.IsLocallyOwned) return;
            // This forces the existing animations to stop
            __instance.Owner?.FPItemHolder?.OnIdleStart();
        }
    }
}
