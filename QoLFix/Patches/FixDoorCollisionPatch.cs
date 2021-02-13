using BepInEx.Configuration;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace QoLFix.Patches
{
    /// <summary>
    /// WeakDoor has two states: Skinned and Simple (default).
    /// The door switches to the Skinned state when damaged or c-foamed.
    /// The Simple state uses a single BoxCollider, whereas Skinned is split
    /// into multiple parts with separate BoxColliders. However, the
    /// colliders have a gap small gap between them causing all sorts of
    /// problems, namely:
    ///   - c-foam can pass right through the gap and hit enemies behind them
    ///   - players can hit both colliders at once by aiming at the gap
    ///
    /// This patch increases the collision hitbox of the individual "parts".
    /// </summary>
    public class FixDoorCollisionPatch : IPatch
    {
        private static readonly string PatchName = nameof(FixDoorCollisionPatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the door collision bug where c-foam globs could go through if aimed at the cracks."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
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

        /*private static void GlueGunProjectile__OnProjectileCollision(GlueGunProjectile __instance, RaycastHit rayHit)
        {
            if (rayHit.collider == null) return;
            var gameObject = rayHit.collider.gameObject;
            if (gameObject.layer == LayerManager.LAYER_DYNAMIC)
            {
                MelonLogger.Log("Hit Dynamic GO!");
                var wdd = gameObject.GetComponentInParent<iLG_WeakDoor_Destruction>();
                MelonLogger.Log("Hit WDD: " + wdd);
            }
        }

        private static void LG_WeakDoor_Destruction__Setup(LG_WeakDoor_Destruction __instance)
        {
            var collision = __instance.m_doorBladeSimple.GetChildren().SingleOrDefault(x => x.name == "Collision");
            if (collision == null) return;

            var collider = collision.GetComponent<BoxCollider>();
            if (collider == null) return;

            var newCollision = new GameObject("Collision");
            //var meshRenderer = __instance.m_doorBladeSkinned.GetComponent<MeshRenderer>();
            var newCollider = newCollision.AddComponent<BoxCollider>();
            newCollider.size = collider.size;
            newCollider.center = collider.center;
            newCollider.transform.parent = __instance.m_doorBladeSkinned.transform;
            newCollider.transform.localPosition = collision.transform.localPosition;
            newCollider.transform.localRotation = collision.transform.localRotation;
            newCollider.transform.localScale = collision.transform.localScale;
            newCollider.gameObject.layer = collision.layer;
        }*/
    }
}
