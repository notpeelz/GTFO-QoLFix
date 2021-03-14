using UnityEngine.CrashReportHandler;

namespace QoLFix.Patches.Misc
{
    public class DisableCrashReporterPatch : Patch
    {
        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
        }

        public override string Name { get; } = nameof(DisableCrashReporterPatch);

        public override void Execute()
        {
            CrashReportHandler.enableCaptureExceptions = false;
            this.PatchMethod<CrashReportHandler>($"set_{nameof(CrashReportHandler.enableCaptureExceptions)}", PatchType.Prefix);
            this.PatchMethod<CrashReportHandler>(nameof(CrashReportHandler.SetUserMetadata), PatchType.Prefix);
        }

        private static void CrashReportHandler__set_enableCaptureExceptions__Prefix(ref bool value) =>
            value = false;

        private static bool CrashReportHandler__SetUserMetadata__Prefix() =>
            HarmonyControlFlow.DontExecute;
    }
}
