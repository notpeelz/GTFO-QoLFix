using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

namespace QoLFix.Patches.Tweaks
{
    public class ScreenLiquidRemovalPatch : Patch
    {
        private const string PatchName = nameof(ScreenLiquidRemovalPatch);
        private static readonly ConfigDefinition ConfigEnabled = new(PatchName, "Enabled");
        private static readonly ConfigDefinition ConfigFilteredEffects = new(PatchName, "FilteredEffects");

        public static Patch Instance { get; private set; }

        public override void Initialize()
        {
            Instance = this;
            QoLFixPlugin.Instance.Config.Bind(ConfigEnabled, false, new ConfigDescription("Prevents on-screen liquid effects from playing."));
            QoLFixPlugin.Instance.Config.Bind(ConfigFilteredEffects, ScreenLiquidCategory.None, new ConfigDescription("Controls what effects should be blocked from playing."));
        }

        public override string Name { get; } = PatchName;

        public override bool Enabled => QoLFixPlugin.Instance.Config.GetConfigEntry<bool>(ConfigEnabled).Value;

        public override void Execute()
        {
            var classType = typeof(ScreenLiquidManager);
            var methods = classType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods.Where(x => ApplyMethodNames.Contains(x.Name)))
            {
                this.PatchMethod(classType, method, PatchType.Prefix, nameof(ScreenLiquidManager__Apply__Prefix));
            }
        }

        private static readonly string[] ApplyMethodNames = new[]
        {
            nameof(ScreenLiquidManager.DirectApply),
            nameof(ScreenLiquidManager.TryApply),
            nameof(ScreenLiquidManager.Apply),
        };

        private static bool ScreenLiquidManager__Apply__Prefix(ScreenLiquidSettingName setting)
        {
            switch (setting)
            {
                case ScreenLiquidSettingName.enemyBlood_BigBloodBomb:
                case ScreenLiquidSettingName.enemyBlood_SmallRandomStreak:
                case ScreenLiquidSettingName.enemyBlood_Squirt:
                case ScreenLiquidSettingName.shooterGoo:
                    return InterceptEffect(ScreenLiquidCategory.EnemyDamage);
                case ScreenLiquidSettingName.playerBlood_SmallDamage:
                case ScreenLiquidSettingName.playerBlood_BigDamage:
                case ScreenLiquidSettingName.playerBlood_Downed:
                case ScreenLiquidSettingName.playerBlood:
                    return InterceptEffect(ScreenLiquidCategory.PlayerDamage);
                case ScreenLiquidSettingName.spitterJizz:
                    return InterceptEffect(ScreenLiquidCategory.Spitter);
                case ScreenLiquidSettingName.elevatorRain:
                case ScreenLiquidSettingName.waterDrizzle:
                case ScreenLiquidSettingName.waterDrip:
                    return InterceptEffect(ScreenLiquidCategory.Water);
                case ScreenLiquidSettingName.disinfectionPack_Apply:
                case ScreenLiquidSettingName.disinfectionStation_Apply:
                    return InterceptEffect(ScreenLiquidCategory.Disinfection);
                case ScreenLiquidSettingName.infectionSweat:
                    return InterceptEffect(ScreenLiquidCategory.Infection);
                default:
                    Instance.LogWarning($"Playing unhandled ScreenLiquid effect: {setting}");
                    return HarmonyControlFlow.Execute;
            }

            bool InterceptEffect(ScreenLiquidCategory category)
            {
                var filteredCategories = QoLFixPlugin.Instance.Config.GetConfigEntry<ScreenLiquidCategory>(ConfigFilteredEffects).Value;
                var shouldFilter = (filteredCategories & category) != 0;
                Instance.LogDebug($"{(shouldFilter ? "Blocking" : "Playing")} ScreenLiquid effect: {setting}");
                return shouldFilter
                    ? HarmonyControlFlow.DontExecute
                    : HarmonyControlFlow.Execute;
            }
        }

        [Flags]
        public enum ScreenLiquidCategory
        {
            None = 0,
            All = EnemyDamage | PlayerDamage | Spitter | Disinfection | Infection | Water,
            EnemyDamage = 1 << 0,
            PlayerDamage = 1 << 1,
            Spitter = 1 << 2,
            Disinfection = 1 << 3,
            Infection = 1 << 4,
            Water = 1 << 5,
        }
    }
}
