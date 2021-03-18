using BepInEx.Configuration;
using Gear;
using QoLFix.Patches.Misc;
using UnityEngine;

namespace QoLFix.Patches.Tweaks
{
    public class BetterMovementPatch : Patch
    {
        private const string PatchName = nameof(BetterMovementPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigAllowFallingActions = new(PatchName, "AllowFallingActions");
        private static readonly ConfigDefinition ConfigFixVelocityBug = new(PatchName, "FixVelocityBug");
        private static readonly ConfigDefinition ConfigFixMeleeChargeBug = new(PatchName, "FixMeleeChargeBug");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Improves the GTFO movement system."));
            QoLFixPlugin.Instance.Config.Bind(ConfigAllowFallingActions, true, new ConfigDescription("Lets you charge/reload/shoot while falling."));
            QoLFixPlugin.Instance.Config.Bind(ConfigFixVelocityBug, true, new ConfigDescription("Fixes the bug where you would lose all horizontal velocity while bunny-hopping."));
            QoLFixPlugin.Instance.Config.Bind(ConfigFixMeleeChargeBug, true, new ConfigDescription("Fixes the bug where your melee charge would get cancelled if you jumped and charged on the same frame."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigAllowFallingActions).Value)
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

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigFixVelocityBug).Value)
            {
                this.PatchMethod<PLOC_Jump>(nameof(PLOC_Jump.Exit), PatchType.Both);
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigFixMeleeChargeBug).Value)
            {
                this.PatchMethod<MeleeWeaponFirstPerson>($"get_{nameof(MeleeWeaponFirstPerson.FireButton)}", PatchType.Prefix);
            }

            LevelCleanupPatch.OnExitLevel += () =>
            {
                BlockItemDown = false;
            };
        }

        private static Vector3 HorizontalVelocity;
        private static bool BlockItemDown;

        private static bool MeleeWeaponFirstPerson__get_FireButton__Prefix(MeleeWeaponFirstPerson __instance, ref bool __result)
        {
            __result = InputMapper.GetButton.Invoke(InputAction.Fire, __instance.Owner.InputFilter);
            return HarmonyControlFlow.DontExecute;
        }

        private static void PLOC_Jump__Exit__Prefix(PLOC_Jump __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            HorizontalVelocity = __instance.m_owner.Locomotion.HorizontalVelocity;
        }

        private static void PLOC_Jump__Exit__Postfix(PLOC_Jump __instance)
        {
            if (!__instance.m_owner.IsLocallyOwned) return;
            __instance.m_owner.Locomotion.HorizontalVelocity = HorizontalVelocity;
        }

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
