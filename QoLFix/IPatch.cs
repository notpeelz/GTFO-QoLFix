using HarmonyLib;

namespace QoLFix
{
    public interface IPatch
    {
        void Initialize();
        
        string Name { get; }

        bool Enabled { get; }

        void Patch(Harmony harmony);
    }
}