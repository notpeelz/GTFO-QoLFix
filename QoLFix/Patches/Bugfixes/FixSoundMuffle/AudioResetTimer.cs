using System;
using AK;
using UnityEngine;

namespace QoLFix.Patches.Bugfixes
{
    public partial class FixSoundMufflePatch
    {
        public class AudioResetTimer : MonoBehaviour
        {
            private const float RESET_DELAY = 15;

            public AudioResetTimer(IntPtr value)
                : base(value) { }

            private float time;

            internal void Update()
            {
                // Don't update unless the timer is ticking
                if (this.time <= 0) return;

                this.time -= Time.deltaTime;
                if (this.time > 0) return;

                Instance.LogDebug("Resetting");
                CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.SCOUT_SCREAM_DUCKING, 0);
            }

            public void ScheduleReset()
            {
                Instance.LogDebug("Scheduling audio reset");
                this.time = RESET_DELAY;
            }
        }
    }
}
