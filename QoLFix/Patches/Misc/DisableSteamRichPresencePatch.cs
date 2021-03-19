using BepInEx.Configuration;
using SNetwork;

namespace QoLFix.Patches.Misc
{
    public class DisableSteamRichPresencePatch : Patch
    {
        private const string PatchName = nameof(DisableSteamRichPresencePatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Disables Steam Rich Presence updates; also prevents Steam friends from seeing your lobby from the rundown screen."));
        }

        public override string Name { get; } = PatchName;

#if RELEASE
        public override bool Enabled => ConfigEnabled.GetConfigEntry<bool>().Value;
#else
        public override bool Enabled => true;
#endif

        public override void Execute()
        {
            this.PatchMethod<SNet_Core_STEAM>(nameof(SNet_Core_STEAM.SetFriendsData), new[] { typeof(string), typeof(string) }, PatchType.Prefix);
        }

        private static bool SNet_Core_STEAM__SetFriendsData__Prefix() => HarmonyControlFlow.DontExecute;
    }
}
