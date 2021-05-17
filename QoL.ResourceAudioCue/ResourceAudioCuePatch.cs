using AK;
using BepInEx.Configuration;
using BoosterImplants;
using GameData;
using Gear;
using HarmonyLib;
using MTFO.Core;
using Player;

namespace QoL.ResourceAudioCue
{
    public class ResourceAudioCuePatch : MTFOPatch
    {
        private const string PatchName = nameof(ResourceAudioCuePatch);

        private static ConfigEntry<bool> ConfigEnabled = default!;

        public static ResourceAudioCuePatch? Instance { get; private set; }

        public override bool Enabled => ConfigEnabled.Value;

        public override void Initialize()
        {
            Instance = this;
            ConfigEnabled = this.Plugin.Config.Bind(new(PatchName, "Enabled"), true,
                new ConfigDescription("Plays a sound when receiving ammo or health from a teammate."));
        }

        private static bool BlockApplySounds;

        [HarmonyPatch(typeof(ResourcePackFirstPerson))]
        [HarmonyPatch(nameof(ResourcePackFirstPerson.Setup))]
        [HarmonyPatch(new[] { typeof(ItemDataBlock) })]
        [HarmonyPostfix]
        private static void ResourcePackFirstPerson__Setup__Postfix(ResourcePackFirstPerson __instance)
        {
            var oldFn = __instance.m_interactApplyResource.OnTrigger;
            __instance.m_interactApplyResource.OnTrigger = (Il2CppSystem.Action)(() =>
            {
                Instance!.LogDebug("OnTrigger");
                BlockApplySounds = true;
                oldFn?.Invoke();
                BlockApplySounds = false;
            });
        }

        [HarmonyPatch(typeof(CellSoundPlayer))]
        [HarmonyPatch(nameof(CellSoundPlayer.Post))]
        [HarmonyPatch(new[] { typeof(uint) })]
        [HarmonyPrefix]
        private static bool CellSoundPlayer__Post__Prefix(uint eventID)
        {
            if (eventID == EVENTS.AMMOPACK_APPLY) return InterceptSound(nameof(EVENTS.AMMOPACK_APPLY));
            if (eventID == EVENTS.MEDPACK_APPLY) return InterceptSound(nameof(EVENTS.MEDPACK_APPLY));

            return HarmonyControlFlow.Execute;

            static bool InterceptSound(string soundName)
            {
                Instance!.LogDebug($"{(BlockApplySounds ? "Blocked" : "Playing")} sound: {soundName}");
                return BlockApplySounds
                    ? HarmonyControlFlow.DontExecute
                    : HarmonyControlFlow.Execute;
            }
        }

        private static float LastKnownHealth;
        private static bool IsGettingRevived;

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal))]
        [HarmonyPatch(nameof(Dam_PlayerDamageLocal.OnRevive))]
        [HarmonyPrefix]
        private static void Dam_PlayerDamageLocal__OnRevive__Prefix()
        {
            Instance!.LogDebug("IsGettingRevived = true");
            IsGettingRevived = true;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal))]
        [HarmonyPatch(nameof(Dam_PlayerDamageLocal.OnRevive))]
        [HarmonyPostfix]
        private static void Dam_PlayerDamageLocal__OnRevive__Postfix()
        {
            Instance!.LogDebug("IsGettingRevived = false");
            IsGettingRevived = false;
        }

        [HarmonyPatch(typeof(Dam_PlayerDamageLocal))]
        [HarmonyPatch(nameof(Dam_PlayerDamageLocal.ReceiveSetHealth))]
        [HarmonyPostfix]
        private static void Dam_PlayerDamageLocal__ReceiveSetHealth__Prefix(Dam_PlayerDamageLocal __instance, pSetHealthData data)
        {
            if (__instance.Owner?.Owner?.IsLocal != true) return;

            var newHealth = data.health.Get(__instance.HealthMax);
            var healthDelta = (newHealth - LastKnownHealth) / __instance.HealthMax;
            if (healthDelta <= 0) return;

            Instance!.LogDebug($"Old health: {LastKnownHealth}");
            Instance!.LogDebug($"New health: {newHealth}");
            Instance!.LogDebug($"Health delta: {healthDelta}");

            LastKnownHealth = newHealth;

            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            // XXX: workaround for not being able to differentiate between
            // health regen and healing through a medipack.

            var maxRegenHealth = BoosterImplantManager.ApplyEffect(__instance.Owner, BoosterEffect.RegenerationCap, newHealth);
            var isBelowRegenCap = newHealth <= maxRegenHealth;

            Instance!.LogDebug($"maxRegenHealth: {maxRegenHealth}");
            Instance!.LogDebug($"isBelowRegenCap: {isBelowRegenCap}");

            if (isBelowRegenCap)
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
                    Instance!.LogDebug("Receiving health... skipping because we're getting revived");
                    return;
                }

                var wasBlocking = BlockApplySounds;
                BlockApplySounds = false;
                Instance!.LogDebug("Receiving health");
                __instance.Owner.Sound.Post(EVENTS.MEDPACK_APPLY);
                BlockApplySounds = wasBlocking;
            }
        }

        [HarmonyPatch(typeof(PlayerBackpackManager))]
        [HarmonyPatch(nameof(PlayerBackpackManager.ReceiveAmmoGive))]
        [HarmonyPostfix]
        private static void PlayerBackpackManager__ReceiveAmmoGive__Postfix(pAmmoGive data)
        {
            if (!data.targetPlayer.TryGetPlayer(out var player)) return;
            if (!player.IsLocal) return;

            Instance!.LogDebug("Receiving ammo");

            var playerAgent = player.PlayerAgent.Cast<PlayerAgent>();

            var wasBlocking = BlockApplySounds;
            BlockApplySounds = false;
            playerAgent.Sound.Post(EVENTS.AMMOPACK_APPLY);
            BlockApplySounds = wasBlocking;
        }
    }
}
