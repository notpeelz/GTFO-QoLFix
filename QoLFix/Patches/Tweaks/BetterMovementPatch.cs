using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches.Tweaks
{
    public class BetterMovementPatch : IPatch
    {
        private const string PatchName = nameof(BetterMovementPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you charge/reload while falling."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
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

        private static bool BlockItemDown;

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
