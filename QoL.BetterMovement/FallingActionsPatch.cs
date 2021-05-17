using BepInEx.Configuration;
using HarmonyLib;
using MTFO.Core;
using MTFO.Core.Attributes;

namespace QoL.BetterMovement
{
    public class FallingActionsPatch : MTFOPatch
    {
        private const string PatchName = nameof(FallingActionsPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static FallingActionsPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Lets you charge/reload/shoot while falling."));
        }

        protected override void Apply()
        {
            base.Apply();
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
        }

        private static bool BlockItemDown;

        [OnGameStateChange(eGameStateName.Lobby)]
        private static void OnExitLevel()
        {
            BlockItemDown = false;
        }

        [HarmonyPatch(typeof(FirstPersonItemHolder))]
        [HarmonyPatch(nameof(FirstPersonItemHolder.ItemDownTrigger))]
        [HarmonyPatch(MethodType.Setter)]
        [HarmonyPrefix]
        private static bool FirstPersonItemHolder__set_ItemDownTrigger__Prefix() =>
            BlockItemDown
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static void PLOC_Fall__Prefix(PLOC_Fall __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            if (__instance.m_owner?.FPItemHolder == null) return;
            BlockItemDown = true;
        }

        private static void PLOC_Fall__Postfix(PLOC_Fall __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            if (__instance.m_owner?.FPItemHolder == null) return;
            BlockItemDown = false;
        }
    }
}
