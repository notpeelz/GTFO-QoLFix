using BepInEx.Configuration;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public class BetterMovementPatch : Patch
    {
        private const string PatchName = nameof(BetterMovementPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you charge/reload while falling."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            this.PatchMethod<PLOC_Jump>(nameof(PLOC_Jump.Exit), PatchType.Both);
            this.PatchMethod<PLOC_Fall>(
                methodName: nameof(PLOC_Fall.Enter),
                patchType: PatchType.Both,
                prefixMethodName: nameof(PLOC_Fall__Prefix),
                postfixMethodName: nameof(PLOC_Fall__Postfix));
            this.PatchMethod<PLOC_Fall>(
                methodName: nameof(PLOC_Fall.Exit),
                patchType: PatchType.Both,
                prefixMethodName: nameof(PLOC_Fall__Prefix),
                postfixMethodName: nameof(PLOC_Fall__Postfix));
            this.PatchMethod<FirstPersonItemHolder>($"set_{nameof(FirstPersonItemHolder.ItemDownTrigger)}", PatchType.Prefix);
        }

        private static Vector3 HorizontalVelocity;
        private static bool BlockItemDown;

        private static void PLOC_Jump__Exit__Prefix(PLOC_Jump __instance)
        {
            HorizontalVelocity = __instance.m_owner.Locomotion.HorizontalVelocity;
        }

        private static void PLOC_Jump__Exit__Postfix(PLOC_Jump __instance)
        {
            __instance.m_owner.Locomotion.HorizontalVelocity = HorizontalVelocity;
        }

        private static bool FirstPersonItemHolder__set_ItemDownTrigger__Prefix() =>
            BlockItemDown
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static void PLOC_Fall__Prefix(PLOC_Fall __instance)
        {
            if (__instance.m_owner?.FPItemHolder == null) return;
            BlockItemDown = true;
        }

        private static void PLOC_Fall__Postfix(PLOC_Fall __instance)
        {
            if (__instance.m_owner?.FPItemHolder == null) return;
            BlockItemDown = false;
        }
    }
}
