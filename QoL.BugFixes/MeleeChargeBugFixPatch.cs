using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using MTFO.Core;

namespace QoL.BugFixes
{
    public class MeleeChargeBugFixPatch : MTFOPatch
    {
        private const string PatchName = nameof(MeleeChargeBugFixPatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static MeleeChargeBugFixPatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Fixes the bug where your melee charge would get cancelled if you jumped and charged on the same frame."));
        }

        [HarmonyPatch(typeof(MeleeWeaponFirstPerson))]
        [HarmonyPatch(nameof(MeleeWeaponFirstPerson.FireButton))]
        [HarmonyPatch(MethodType.Getter)]
        [HarmonyPrefix]
        private static bool MeleeWeaponFirstPerson__get_FireButton__Prefix(MeleeWeaponFirstPerson __instance, ref bool __result)
        {
            __result = InputMapper.GetButton.Invoke(InputAction.Fire, __instance.Owner.InputFilter);
            return HarmonyControlFlow.DontExecute;
        }
    }
}
