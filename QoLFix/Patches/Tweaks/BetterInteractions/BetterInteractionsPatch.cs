using BepInEx.Configuration;
using Gear;
using QoLFix.Patches.Misc;

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

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            QoLFixPlugin.RegisterPatch<WorldInteractionBlockerPatch>();
            this.PatchMethod<PlayerInteraction>(nameof(PlayerInteraction.UpdateWorldInteractions), PatchType.Prefix);

            if (ConfigPatchMineDeployer.GetConfigEntry<bool>().Value)
            {
                WorldInteractions_PersistentInteractions = true;
                this.PatchMethod<ResourcePackFirstPerson>(nameof(ResourcePackFirstPerson.Update), PatchType.Postfix);
                this.PatchMethod<CarryItemEquippableFirstPerson>(nameof(CarryItemEquippableFirstPerson.Update), PatchType.Postfix);
            }

            if (ConfigPersistentInteractions.GetConfigEntry<bool>().Value)
            {
                //this.PatchMethod<Weapon>($"get_{nameof(Weapon.AllowPlayerInteraction)}", PatchType.Prefix);
            }

            if (ConfigPatchInteractDistance.GetConfigEntry<bool>().Value)
            {
                this.PatchInteractDistance();
            }

            if (ConfigPatchMineDeployer.GetConfigEntry<bool>().Value)
            {
                WorldInteractions_MineDeployer = true;
                this.PatchMineDeployer();
            }

            if (ConfigPatchRevive.GetConfigEntry<bool>().Value)
            {
                this.PatchRevive();
            }

            if (ConfigPatchHackingTool.GetConfigEntry<bool>().Value)
            {
                this.PatchHackingTool();
            }

            LevelCleanupPatch.OnExitLevel += () =>
            {
                WorldInteractionTimerRunning = false;
                ResourcePackInteractionTimerRunning = false;
                CarryItemInteractionTimerRunning = false;
            };
        }

        private static bool WorldInteractions_MineDeployer;
        private static bool WorldInteractions_PersistentInteractions;
        private static bool WorldInteractionTimerRunning;
        private static bool ResourcePackInteractionTimerRunning;
        private static bool CarryItemInteractionTimerRunning;

        private static void CarryItemEquippableFirstPerson__Update__Postfix(CarryItemEquippableFirstPerson __instance)
        {
            var interact = __instance.m_interactDropItem;
            if (interact.TimerIsActive != CarryItemInteractionTimerRunning)
            {
                CarryItemInteractionTimerRunning = interact.TimerIsActive;
                WorldInteractionBlockerPatch.IgnoreWorldInteractions += CarryItemInteractionTimerRunning ? 1 : -1;
            }
        }

        private static void ResourcePackFirstPerson__Update__Postfix(ResourcePackFirstPerson __instance)
        {
            var interact = __instance.m_interactApplyResource;
            if (interact.TimerIsActive != ResourcePackInteractionTimerRunning)
            {
                ResourcePackInteractionTimerRunning = interact.TimerIsActive;
                WorldInteractionBlockerPatch.IgnoreWorldInteractions += ResourcePackInteractionTimerRunning ? 1 : -1;
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
            if (ResourcePackInteractionTimerRunning) return HarmonyControlFlow.DontExecute;
            if (CarryItemInteractionTimerRunning) return HarmonyControlFlow.DontExecute;
            if (!PlayerInteraction.InteractionEnabled) return HarmonyControlFlow.Execute;

            // Prevent timed interacts from getting interrupted by other
            // stuff while the interact key is held.

            // As a side effect of getting rid of sphere checks, interactions
            // stay active even when moving out of range...
            // (at least until Interact_Timed.m_maxMoveDisAllowed kicks in)
            var timedInteract = __instance.m_bestSelectedInteract?.TryCast<Interact_Timed>();
            if (timedInteract?.TimerIsActive != true)
            {
                if (!WorldInteractions_MineDeployer) return HarmonyControlFlow.Execute;
                var bmd = PlayerInteraction__UpdateWorldInteractions__MineDeployer(__instance);
                return bmd ?? HarmonyControlFlow.Execute;
            }

            if (!WorldInteractions_PersistentInteractions) return HarmonyControlFlow.Execute;

            var player = __instance.m_owner;

            timedInteract.PlayerSetSelected(true, player);
            if (timedInteract.PlayerCheckInput(player))
            {
                timedInteract.PlayerDoInteract(player);
            }

            if (timedInteract.TimerIsActive != WorldInteractionTimerRunning)
            {
                WorldInteractionTimerRunning = timedInteract.TimerIsActive;
                WorldInteractionBlockerPatch.IgnoreWorldInteractions += WorldInteractionTimerRunning ? 1 : -1;
            }

            return HarmonyControlFlow.DontExecute;
        }
    }
}
