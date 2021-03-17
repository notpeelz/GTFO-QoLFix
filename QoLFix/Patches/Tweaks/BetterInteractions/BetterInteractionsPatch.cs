using BepInEx.Configuration;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch : Patch
    {
        private const string PatchName = nameof(BetterInteractionsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigPersistentInteractions = new(PatchName, "PersistentInteractions");
        private static readonly ConfigDefinition ConfigInteractWhileReloading = new(PatchName, "InteractWhileReloading");
        private static readonly ConfigDefinition ConfigPatchInteractDistance = new(PatchName, "PatchInteractDistance");
        private static readonly ConfigDefinition ConfigPatchMineDeployer = new(PatchName, "PatchMineDeployer");
        private static readonly ConfigDefinition ConfigPatchRevive = new(PatchName, "PatchRevive");
        private static readonly ConfigDefinition ConfigPatchHackingTool = new(PatchName, "PatchHackingTool");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes several quirks of the interaction system."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPersistentInteractions, true, new ConfigDescription("Prevents timed interactions from getting cancelled by looking at another interaction."));
            QoLFixPlugin.Instance.Config.Bind(ConfigInteractWhileReloading, true, new ConfigDescription("Lets you interact while reloading."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchInteractDistance, true, new ConfigDescription("Fixes interactions cancelling when moving too far away (sentries, mines on ceiling, etc.)"));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchMineDeployer, true, new ConfigDescription("Fixes the mine deployer prioritizing doors over placing mines."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchRevive, true, new ConfigDescription("Gives you full control over your camera while reviving. NOTE: for balance reasons, this also prevents you from firing/ADSing while reviving."));
            QoLFixPlugin.Instance.Config.Bind(ConfigPatchHackingTool, true, new ConfigDescription("Prevents the hacking tool minigame from getting cancelled if you swapped weapons/moved too early."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigPersistentInteractions).Value)
            {
                this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.UpdateWorldInteractions), PatchType.Prefix);
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigInteractWhileReloading).Value)
            {
                //this.PatchMethod<Weapon>($"get_{nameof(Weapon.AllowPlayerInteraction)}", PatchType.Prefix);
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigPatchInteractDistance).Value)
            {
                this.PatchInteractDistance();
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigPatchMineDeployer).Value)
            {
                this.PatchMineDeployer();
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigPatchRevive).Value)
            {
                this.PatchRevive();
            }

            if (QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigPatchHackingTool).Value)
            {
                this.PatchHackingTool();
            }
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
