using HarmonyLib;
using UnityEngine.CrashReportHandler;

namespace QoLFix.Patches.Misc
{
    public class DisableCrashReporterPatch : IPatch
    {
        public static IPatch Instance { get; private set; }

        public void Initialize()
        {
            Instance = this;
        }

        public string Name { get; } = nameof(DisableCrashReporterPatch);

        public Harmony Harmony { get; set; }

        public void Patch()
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
