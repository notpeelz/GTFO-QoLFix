namespace QoLFix.Updater
{
    public static partial class UpdateManager
    {
        public class ReleaseInfo
        {
            public ReleaseInfo() { }

            public string DownloadUrl { get; set; }

            public SemVer.Version Version { get; set; }

            public bool PreRelease { get; set; }
        }
    }
}
