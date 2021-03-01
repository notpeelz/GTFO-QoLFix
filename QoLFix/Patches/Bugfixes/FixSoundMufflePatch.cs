using AK;
using BepInEx.Configuration;
using HarmonyLib;

namespace QoLFix.Patches.Bugfixes
{
    public class FixSoundMufflePatch : IPatch
    {
        private const string PatchName = nameof(FixSoundMufflePatch);
        private static readonly ConfigDefinition ConfigEnabled = new ConfigDefinition(PatchName, "Enabled");

        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, true, new ConfigDescription("Fixes the bug where the scout muffle sound effect doesn't go away when exiting a game too early."));
        }

        public string Name { get; } = PatchName;

        public bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public Harmony Harmony { get; set; }

        public void Patch()
        {
            this.PatchMethod<GameStateManager>(nameof(GameStateManager.ChangeState), PatchType.Postfix);
        }

        private static void GameStateManager__ChangeState__Postfix(eGameStateName nextState)
        {
            switch (nextState)
            {
                case eGameStateName.NoLobby:
                case eGameStateName.Lobby:
                case eGameStateName.Generating:
                    CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.SCOUT_SCREAM_DUCKING, 0);
                    break;
            }
        }
    }
}
