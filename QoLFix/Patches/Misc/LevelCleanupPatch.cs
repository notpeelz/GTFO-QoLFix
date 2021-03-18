using System;

namespace QoLFix.Patches.Misc
{
    public class LevelCleanupPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override string Name { get; } = nameof(LevelCleanupPatch);

        public override void Initialize()
        {
            Instance = this;
        }

        public override void Execute()
        {
            this.PatchMethod<GameStateManager>(nameof(GameStateManager.ChangeState), PatchType.Postfix);
        }

        private static eGameStateName GameState;

        public static event Action OnExitLevel;

        private static void GameStateManager__ChangeState__Postfix(eGameStateName nextState)
        {
            if (nextState != eGameStateName.InLevel && GameState == eGameStateName.InLevel)
            {
                Instance.LogDebug("Cleaning up patch states");
                OnExitLevel?.Invoke();
            }
            GameState = nextState;
        }
    }
}
