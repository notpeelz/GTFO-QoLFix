using System.Linq;
using BepInEx.Configuration;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    public class FixTerminalDisplayPingPatch : Patch
    {
        private const string PatchName = nameof(FixTerminalDisplayPingPatch);
        private const string WarningMessage = "NOTICE: this patch is forcefully disabled due to a bug in GTFO code causing world generation issues.";
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription($"Fixes the bug where monitor-only terminals on the tech tileset aren't pingable.\n{WarningMessage}"));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => false; // ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<LG_MarkerFactory>(nameof(LG_MarkerFactory.InstantiateMarkerGameObject), PatchType.Postfix);
        }

        private static void LG_MarkerFactory__InstantiateMarkerGameObject__Postfix(ref GameObject __result)
        {
            if (__result == null) return;
            var terminals = __result?.GetComponentsInChildren<LG_ComputerTerminal>().ToArray();
            if (terminals?.Length == 0) return;

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
