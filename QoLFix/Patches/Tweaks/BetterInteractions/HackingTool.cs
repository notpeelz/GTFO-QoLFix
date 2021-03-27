using AK;
using LevelGeneration;
using QoLFix.UI;

namespace QoLFix.Patches.Tweaks
{
    public partial class BetterInteractionsPatch
    {
        private void PatchHackingTool()
        {
            this.PatchMethod<HackingTool>(nameof(HackingTool.AbortHack), PatchType.Prefix);
        }

        private static bool HackingTool__AbortHack__Prefix(HackingTool __instance)
        {
            if (!__instance.m_isHacking) return HarmonyControlFlow.Execute;

            // Execute the rest of the hacking sequence if interrupted
            // prematurely.
            switch (__instance.m_state)
            {
                case HackingTool.HackSequenceState.UpdateMiniGame:
                    var timingGrid = __instance.m_activeMinigame.TryCast<HackingMinigame_TimingGrid>();
                    // Skip the TimingGrid's GamePauseTimer as it only updates
                    // its state every so often.
                    if (timingGrid?.m_puzzleDone != true) return HarmonyControlFlow.Execute;
                    __instance.Sound.Post(EVENTS.HACKING_PUZZLE_SUCCESS);
                    CompleteHack(1);
                    return HarmonyControlFlow.DontExecute;
                case HackingTool.HackSequenceState.DoneWait:
                    CompleteHack(1);
                    return HarmonyControlFlow.DontExecute;
                case HackingTool.HackSequenceState.Done:
                    CompleteHack();
                    return HarmonyControlFlow.DontExecute;
                default:
                    return HarmonyControlFlow.Execute;
            }

            void CompleteHack(float? delay = null)
            {
                var hackable = __instance.m_currentHackable;
                var sound = __instance.Sound;
                var player = __instance.Owner;

                __instance.ClearScreen();
                __instance.m_activeMinigame?.EndGame();
                __instance.OnStopHacking();
                __instance.m_state = HackingTool.HackSequenceState.Idle;
                __instance.m_stateTimer = 0;
                __instance.m_currentHackable = null;

                if (delay == null)
                {
                    Impl();
                    return;
                }

                ActionScheduler.Schedule(Impl, (float)delay);

                void Impl()
                {
                    sound.Post(EVENTS.BUTTONGENERICSEQUENCEFINISHED);
                    if (hackable == null) return;
                    LG_LevelInteractionManager.WantToSetHackableStatus(hackable, eHackableStatus.Success, player);
                }
            }
        }
    }
}
