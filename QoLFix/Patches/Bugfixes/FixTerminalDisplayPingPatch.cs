using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    public class FixTerminalDisplayPingPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixTerminalDisplayPingPatch);
        private static readonly string WarningMessage = "WARNING: this patch causes exceptions to show up in the logs. This is unfortunately unavoidable but it shouldn't cause any issues.";
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription($"Fixes the bug where monitor-only terminals on the tech tileset aren't pingable.\n{WarningMessage}"));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<LG_MarkerFactory>(nameof(LG_MarkerFactory.InstantiateMarkerGameObject), PatchType.Postfix);
        }

        private static void LG_MarkerFactory__InstantiateMarkerGameObject__Postfix(ref GameObject __result)
        {
            if (__result == null) return;
            var terminals = __result?.GetComponentsInChildren<LG_ComputerTerminal>();
            if (terminals == null || !terminals.Any()) return;

            var count = 0;
            foreach (var terminal in terminals)
            {
                Instance.LogDebug("terminal: " + terminal.name);
                var colliders = terminal.GetComponentsInChildren<BoxCollider>();

                var pingTargetCount = 0;
                foreach (var collider in colliders)
                {
                    Instance.LogDebug("collider: " + collider.name);
                    var parent = collider.transform.parent.gameObject;
                    Instance.LogDebug("collider.parent: " + parent.name);
                    if (!parent.name.StartsWith("kit_ElectronicsTerminalScreen_tech")) continue;
                    if (collider.gameObject.GetComponent<PlayerPingTarget>() != null) continue;
                    var pingTarget = collider.gameObject.AddComponent<PlayerPingTarget>();
                    pingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingTerminal;
                    pingTargetCount++;
                }

                if (pingTargetCount > 0)
                {
                    var interact = terminal.GetComponentInChildren<Interact_ComputerTerminal>();
                    var pingTarget = interact.gameObject.AddComponent<PlayerPingTarget>();
                    pingTarget.m_pingTargetStyle = eNavMarkerStyle.PlayerPingTerminal;
                    count++;
                    Instance.LogDebug($"Patched terminal {terminal.name}");
                }
            }

            if (count > 1)
            {
                Instance.LogWarning("There was more than 1 terminal on this marker?");
            }
        }
    }
}
