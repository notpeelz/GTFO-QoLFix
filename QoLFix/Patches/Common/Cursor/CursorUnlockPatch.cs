using CellMenu;
using HarmonyLib;
using QoLFix.UI;
using UnityEngine;

namespace QoLFix.Patches.Common.Cursor
{
    public class CursorUnlockPatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
        }

        public string Name { get; } = nameof(CursorUnlockPatch);

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateCursorPosition), PatchType.Prefix);
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateButtonPress), PatchType.Prefix);
            this.PatchMethod<UnityEngine.Cursor>($"set_{nameof(UnityEngine.Cursor.lockState)}", PatchType.Prefix);
            this.PatchMethod<UnityEngine.Cursor>($"set_{nameof(UnityEngine.Cursor.visible)}", PatchType.Prefix);
        }

        private static CursorLockMode savedLockMode;
        private static bool savedVisible;

        public static void RestoreCursorState()
        {
            UnityEngine.Cursor.lockState = savedLockMode;
            UnityEngine.Cursor.visible = savedVisible;
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
