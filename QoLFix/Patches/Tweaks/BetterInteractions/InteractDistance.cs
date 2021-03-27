namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch
    {
        private void PatchInteractDistance()
        {
            this.PatchMethod<SentryGunFirstPerson>(nameof(SentryGunFirstPerson.Setup), PatchType.Postfix);
            this.PatchMethod<MineDeployerInstance>(nameof(MineDeployerInstance.Setup), PatchType.Postfix);
            this.PatchMethod<MineDeployerFirstPerson>(nameof(MineDeployerFirstPerson.Setup), PatchType.Postfix);
        }

        private static void SentryGunFirstPerson__Setup__Postfix(SentryGunFirstPerson __instance)
        {
            // Prevents the "Place sentry" interaction from getting interrupted
            // if the player walks too far away from where they started holding
            // the interact key.
            __instance.m_interactPlaceItem.m_maxMoveDisAllowed = float.MaxValue;
        }

        private static void MineDeployerFirstPerson__Setup__Postfix(MineDeployerFirstPerson __instance)
        {
            // Same thing as the SentryGunFirstPerson::Setup patch
            // Not very useful but at least it stays consistent with the
            // sentry behavior.
            __instance.m_interactPlaceItem.m_maxMoveDisAllowed = float.MaxValue;
        }

        private static void MineDeployerInstance__Setup__Postfix(MineDeployerInstance __instance)
        {
            // Lets the player pick up mines that would otherwise be really
            // hard to reach (e.g. ceiling mines)
            __instance.m_interactPickup.m_maxMoveDisAllowed = float.MaxValue;
        }
    }
}
