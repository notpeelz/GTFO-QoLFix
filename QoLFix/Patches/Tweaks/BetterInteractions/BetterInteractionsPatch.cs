using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : IPatch
    {
        private const string PatchName = nameof(BetterInteractionsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes several quirks of the interaction system."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.UpdateWorldInteractions), PatchType.Prefix);
            this.PatchMethod<Weapon>($"get_{nameof(Weapon.AllowPlayerInteraction)}", PatchType.Prefix);
            this.PatchInteractDistance();
            this.PatchMineDeployer();
            this.PatchRevive();
            this.PatchHackingTool();
        }

        private static bool Weapon__get_AllowPlayerInteraction__Prefix(ref bool __result)
        {
            // Allow interactions while reloading weapons
            __result = true;
            return HarmonyControlFlow.DontExecute;
        }

        private static bool PlayerInteraction__UpdateWorldInteractions__Prefix(PlayerInteraction __instance)
        {
            if (!PlayerInteraction.InteractionEnabled) return HarmonyControlFlow.Execute;

            // Prevent timed interacts from getting interrupted by other
            // stuff while the interact key is held.

            // As a side effect of getting rid of sphere checks, interactions
            // stay active even when moving out of range...
            // (at least until Interact_Timed.m_maxMoveDisAllowed kicks in)
            var timedInteract = __instance.m_bestSelectedInteract?.TryCast<Interact_Timed>();
            if (timedInteract?.TimerIsActive != true)
            {
                var bmd = PlayerInteraction__UpdateWorldInteractions__MineDeployer(__instance);
                return bmd ?? HarmonyControlFlow.Execute;
            }

            var player = __instance.m_owner;

            timedInteract.PlayerSetSelected(true, player);
            if (timedInteract.PlayerCheckInput(player))
            {
                timedInteract.PlayerDoInteract(player);
            }

            return HarmonyControlFlow.DontExecute;
        }
    }
}
