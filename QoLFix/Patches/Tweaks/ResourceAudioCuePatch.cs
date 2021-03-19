using AK;
using BepInEx.Configuration;
using GameData;
using Gear;
using Player;

namespace QoLFix.Patches.Tweaks
{
    public class ResourceAudioCuePatch : Patch
    {
        private const string PatchName = nameof(ResourceAudioCuePatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Plays a sound when receiving ammo or health from a teammate."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<PlayerBackpackManager>(nameof(PlayerBackpackManager.ReceiveAmmoGive), PatchType.Postfix);
            this.PatchMethod<Dam_PlayerDamageLocal>(nameof(Dam_PlayerDamageLocal.ReceiveSetHealth), PatchType.Prefix);
            this.PatchMethod<Dam_PlayerDamageLocal>(nameof(Dam_PlayerDamageLocal.OnRevive), PatchType.Both);
            this.PatchMethod<CellSoundPlayer>(nameof(CellSoundPlayer.Post), new[] { typeof(uint) }, PatchType.Prefix);
            this.PatchMethod<ResourcePackFirstPerson>(nameof(ResourcePackFirstPerson.Setup), new[] { typeof(ItemDataBlock) }, PatchType.Postfix);
        }

        private static bool BlockApplySounds;

        private static void ResourcePackFirstPerson__Setup__Postfix(ResourcePackFirstPerson __instance)
        {
            var oldFn = __instance.m_interactApplyResource.OnTrigger;
            __instance.m_interactApplyResource.OnTrigger = (Il2CppSystem.Action)(() =>
            {
                Instance.LogDebug("OnTrigger");
                BlockApplySounds = true;
                oldFn?.Invoke();
                BlockApplySounds = false;
            });
        }

        private static bool CellSoundPlayer__Post__Prefix(uint eventID)
        {
            if (eventID == EVENTS.AMMOPACK_APPLY) return InterceptSound(nameof(EVENTS.AMMOPACK_APPLY));
            if (eventID == EVENTS.MEDPACK_APPLY) return InterceptSound(nameof(EVENTS.MEDPACK_APPLY));

            return HarmonyControlFlow.Execute;

            static bool InterceptSound(string soundName)
            {
                Instance.LogDebug($"{(BlockApplySounds ? "Blocked" : "Playing")} sound: {soundName}");
                return BlockApplySounds
                    ? HarmonyControlFlow.DontExecute
                    : HarmonyControlFlow.Execute;
            }
        }

        private static float LastKnownHealth;
        private static bool IsGettingRevived;

        private static void Dam_PlayerDamageLocal__OnRevive__Prefix()
        {
            Instance.LogDebug("IsGettingRevived = true");
            IsGettingRevived = true;
        }

        private static void Dam_PlayerDamageLocal__OnRevive__Postfix()
        {
            Instance.LogDebug("IsGettingRevived = false");
            IsGettingRevived = false;
        }

        private static void Dam_PlayerDamageLocal__ReceiveSetHealth__Prefix(Dam_PlayerDamageLocal __instance, pSetHealthData data)
        {
            var player = __instance.Owner?.Owner;
            if (player?.IsLocal != true) return;

            var newHealth = data.health.Get(__instance.HealthMax);
            var healthDelta = (newHealth - LastKnownHealth) / __instance.HealthMax;

            Instance.LogDebug($"Old health: {LastKnownHealth}");
            Instance.LogDebug($"New health: {newHealth}");
            Instance.LogDebug($"Health delta: {healthDelta}");

            LastKnownHealth = newHealth;

            if (healthDelta < 0) return;

            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            // XXX: workaround for not being able to differentiate between
            // health regen and healing through a medipack.

            // Are we regenerating health?
            if (__instance.GetHealthRel() < __instance.Owner.PlayerData.healthRegenRelMax)
            {
                // Has our health increased by more than 5%?
                // If so, then we probably got healed by a medipack
                if (healthDelta > 0.05f) PlaySound();
            }
            else
            {
                PlaySound();
            }

            void PlaySound()
            {
                if (IsGettingRevived)
                {
                    Instance.LogDebug("Receiving health... skipping because we're getting revived");
                    return;
                }

                var wasBlocking = BlockApplySounds;
                BlockApplySounds = false;
                Instance.LogDebug("Receiving health");
                __instance.Owner.Sound.Post(EVENTS.MEDPACK_APPLY);
                BlockApplySounds = wasBlocking;
            }
        }

        private static void PlayerBackpackManager__ReceiveAmmoGive__Postfix(pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;
            if (!player.IsLocal) return;

            Instance.LogDebug("Receiving ammo");

            var playerAgent = player.PlayerAgent.Cast<PlayerAgent>();

            var wasBlocking = BlockApplySounds;
            BlockApplySounds = false;
            playerAgent.Sound.Post(EVENTS.AMMOPACK_APPLY);
            BlockApplySounds = wasBlocking;
        }
    }
}
