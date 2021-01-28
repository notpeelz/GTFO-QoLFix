﻿using System;
using BepInEx.Configuration;
using Gear;
using HarmonyLib;
using LevelGeneration;
using Player;

namespace QoLFix.Patches
{
    public class PingableSwapsPatch : IPatch
    {
        private static readonly string PatchName = nameof(PingableSwapsPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public void Initialize()
        {
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Lets you (terminal) ping resource packs that were swapped out."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public void Patch(Harmony harmony)
        {
            var methodInfo = typeof(ResourcePackPickup).GetMethod(nameof(ResourcePackPickup.Setup));
            harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(PingableSwapsPatch), nameof(ResourcePackPickup__Setup))));
        }

        private static void ResourcePackPickup__Setup(ResourcePackPickup __instance)
        {
            __instance.m_sync.add_OnSyncStateChange(
                (Il2CppSystem.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>)(
                    (status, placement, _, _) => ResourcePackPickup__OnSyncStateChange(__instance, status)
                )
            );
        }

        private static void ResourcePackPickup__OnSyncStateChange(ResourcePackPickup __instance, ePickupItemStatus status)
        {
            if (status != ePickupItemStatus.PlacedInLevel) return;

            var terminalItem = __instance.m_terminalItem;
            if (String.IsNullOrEmpty(terminalItem?.TerminalItemKey)) return;

            if (LG_LevelInteractionManager.Current.m_terminalItemsByKeyString.ContainsKey(terminalItem.TerminalItemKey))
                return;

            LG_LevelInteractionManager.Current.m_terminalItems.Add(terminalItem.TerminalItemId, terminalItem);
            LG_LevelInteractionManager.Current.m_terminalItemsByKeyString.Add(terminalItem.TerminalItemKey, terminalItem);
        }
    }
}
