using CellMenu;
using HarmonyLib;
using QoLFix.UI;
using UnityEngine;

namespace QoLFix.Patches.Common
{
    public class CursorUnlockPatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
        }

        public string Name { get; } = nameof(CursorUnlockPatch);

        public bool Enabled => true;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<CM_PageBase>(nameof(CM_PageBase.UpdateCursorPosition), PatchType.Prefix);
            this.PatchMethod<Cursor>($"set_{nameof(Cursor.lockState)}", PatchType.Prefix);
            this.PatchMethod<Cursor>($"set_{nameof(Cursor.visible)}", PatchType.Prefix);
        }

        private static CursorLockMode savedLockMode;
        private static bool savedVisible;

        public static void RestoreCursorState()
        {
            Cursor.lockState = savedLockMode;
            Cursor.visible = savedVisible;
        }

        private static bool CM_PageBase__UpdateCursorPosition__Prefix()
        {
            if (UIManager.UnlockCursor) return false;
            return true;
        }

        private static void Cursor__set_lockState__Prefix(ref CursorLockMode value)
        {
            if (UIManager.UnlockCursor)
            {
                value = CursorLockMode.None;
                Cursor.visible = true;
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
