using QoLFix.Patches.Misc;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch
    {
        private static bool IsReviving;

        private void PatchRevive()
        {
            this.PatchMethod<Interact_Revive>(nameof(Interact_Revive.Setup), PatchType.Postfix);
            this.PatchMethod<Interact_Revive>(nameof(Interact_Revive.SetTimerActive), PatchType.Postfix);
            this.PatchMethod<InputMapper>(
                methodName: nameof(InputMapper.DoGetButton),
                patchType: PatchType.Prefix,
                prefixMethodName: nameof(InputMapper__DoGetButton__Prefix));
            this.PatchMethod<InputMapper>(
                methodName: nameof(InputMapper.DoGetButtonUp),
                patchType: PatchType.Prefix,
                prefixMethodName: nameof(InputMapper__DoGetButton__Prefix));
            this.PatchMethod<InputMapper>(
                methodName: nameof(InputMapper.DoGetButtonDown),
                patchType: PatchType.Prefix,
                prefixMethodName: nameof(InputMapper__DoGetButton__Prefix));

            LevelCleanupPatch.OnExitLevel += () =>
            {
                IsReviving = false;
            };
        }

        private static void Interact_Revive__Setup__Postfix(Interact_Revive __instance)
        {
            // Give the player full control over their camera while reviving
            __instance.m_minCamDotAllowed = -1;
        }

        private static void Interact_Revive__SetTimerActive__Postfix(bool t) => IsReviving = t;

        private static bool InputMapper__DoGetButton__Prefix(ref bool __result, InputAction action)
        {
            if (!IsReviving) return HarmonyControlFlow.Execute;

            // For balance reasons, we prevent firing weapons while reviving
            // since we're giving the player full control over their camera.
            if (action is not InputAction.Fire
                and not InputAction.Aim) return HarmonyControlFlow.Execute;
            __result = false;
            return HarmonyControlFlow.DontExecute;
        }
    }
}
