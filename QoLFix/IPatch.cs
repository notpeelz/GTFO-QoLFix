using HarmonyLib;

namespace QoLFix
{
    public interface IPatch
    {
        void Initialize();

        Harmony Harmony { get; set; }

        string Name { get; }

        bool Enabled { get; }

        void Patch();
    }
}
