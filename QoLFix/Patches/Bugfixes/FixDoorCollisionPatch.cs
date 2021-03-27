using BepInEx.Configuration;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    /// <summary>
    /// <para>
    /// WeakDoor has two states: Skinned and Simple (default).
    /// The door switches to the Skinned state when damaged or c-foamed.
    /// The Simple state uses a single BoxCollider, whereas Skinned is split
    /// into multiple parts with separate BoxColliders. However, the
    /// colliders have a gap small gap between them causing all sorts of
    /// problems, namely:
    ///   - c-foam can pass right through the gap and hit enemies behind them
    ///   - players can hit both colliders at once by aiming at the gap
    /// </para>
    /// <para>This patch increases the collision hitbox of the individual "parts".</para>
    /// </summary>
    public class FixDoorCollisionPatch : Patch
    {
        private const string PatchName = nameof(FixDoorCollisionPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the door collision bug where c-foam globs could go through if aimed at the cracks."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;

        public override void Execute()
        {
            this.PatchMethod<LG_WeakDoor_Destruction>(nameof(LG_WeakDoor_Destruction.Setup), PatchType.Postfix);
        }

        private static void LG_WeakDoor_Destruction__Setup__Postfix(LG_WeakDoor_Destruction __instance)
        {
            var skinned = __instance.m_doorBladeSkinned;
            foreach (var collider in skinned.GetComponentsInChildren<BoxCollider>())
            {
                // Thanks to Spartan for this big brain idea
                // This hack is so much simpler than my previous approach :D
                collider.size = new Vector3(collider.size.x, collider.size.y * 1.1f, collider.size.z);
            }
        }
    }
}
