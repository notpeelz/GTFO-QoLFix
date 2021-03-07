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

            private float scheduledTime;

            internal void Update()
            {
                if (this.scheduledTime <= 0) return;
                if (this.scheduledTime > Time.time) return;
                this.scheduledTime = 0;

                Instance.LogDebug("Resetting");
                CellSound.SetGlobalRTPCValue(GAME_PARAMETERS.SCOUT_SCREAM_DUCKING, 0);
            }

            public void ScheduleReset()
            {
                Instance.LogDebug("Scheduling audio reset");
                this.scheduledTime = Time.time + RESET_DELAY;
            }
        }
    }
}
