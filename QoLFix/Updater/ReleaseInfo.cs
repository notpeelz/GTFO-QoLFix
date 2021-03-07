using System;

namespace QoLFix.Updater
{
    public static partial class UpdateManager
    {
        public class ReleaseInfo
        {
            public ReleaseInfo() { }

            public string DownloadUrl { get; set; }

            public Version Version { get; set; }

            public bool PreRelease { get; set; }
        }
    }
}
