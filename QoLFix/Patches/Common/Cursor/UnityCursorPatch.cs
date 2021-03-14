using CellMenu;
using QoLFix.UI;
using UnityEngine;

namespace QoLFix.Patches.Common.Cursor
{
    public class UnityCursorPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
        }

        public override string Name { get; } = nameof(UnityCursorPatch);

        public override void Execute()
        {
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateCursorPosition), PatchType.Prefix);
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateButtonPress), PatchType.Prefix);
            this.PatchMethod<UnityEngine.Cursor>($"set_{nameof(UnityEngine.Cursor.lockState)}", PatchType.Prefix);
            this.PatchMethod<UnityEngine.Cursor>($"set_{nameof(UnityEngine.Cursor.visible)}", PatchType.Prefix);
            this.PatchMethod<PlayerChatManager>(nameof(PlayerChatManager.UpdateTextChatInput), PatchType.Prefix);
            this.PatchMethod<InputMapper>(nameof(InputMapper.DoGetAxis), PatchType.Prefix);
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
        }

        private static CursorLockMode savedLockMode;
        private static bool savedVisible;

        public static void RestoreCursorState()
        {
            UnityEngine.Cursor.lockState = savedLockMode;
            UnityEngine.Cursor.visible = savedVisible;
        }

        private static bool PlayerChatManager__UpdateTextChatInput__Prefix() =>
            UnityEngine.Cursor.lockState != CursorLockMode.None
                ? HarmonyControlFlow.Execute
                : HarmonyControlFlow.DontExecute;

        private static bool InputMapper__DoGetButton__Prefix(ref bool __result)
        {
            if (UnityEngine.Cursor.lockState != CursorLockMode.None) return HarmonyControlFlow.Execute;
            __result = false;
            return HarmonyControlFlow.DontExecute;
        }

        private static bool InputMapper__DoGetAxis__Prefix(ref float __result)
        {
            if (UnityEngine.Cursor.lockState != CursorLockMode.None) return HarmonyControlFlow.Execute;
            __result = 0;
            return HarmonyControlFlow.DontExecute;
        }

        private static bool CM_PageBase__UpdateCursorPosition__Prefix() =>
            UnityEngine.Cursor.lockState == CursorLockMode.None
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static bool CM_PageBase__UpdateButtonPress__Prefix() =>
            UnityEngine.Cursor.lockState == CursorLockMode.None
                ? HarmonyControlFlow.DontExecute
                : HarmonyControlFlow.Execute;

        private static void Cursor__set_lockState__Prefix(ref CursorLockMode value)
        {
            if (UIManager.UnlockCursor)
            {
                value = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                savedLockMode = value;
            }
        }

        private static void Cursor__set_visible__Prefix(ref bool value)
        {
            if (UIManager.UnlockCursor)
            {
                value = true;
            }
            else
            {
                savedVisible = value;
            }
        }
    }
}
