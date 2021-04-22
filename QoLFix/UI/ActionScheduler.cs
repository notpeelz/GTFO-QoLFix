using System;
using System.Collections.Generic;
using UnityEngine;

namespace QoLFix.UI
{
    public static class ActionScheduler
    {
        private class ActionEntry : IScheduledAction
        {
            public ActionEntry(Action action, float delay, float interval)
            {
                this.action = action;
                this.delay = delay;
                this.interval = interval;
            }

            public Action action;
            public float delay;
            public float interval;
            public bool enabled;
            public bool completed;
            public bool markedForDeletion;

            public void Invalidate() => this.markedForDeletion = true;

            public bool Active => (!this.completed || this.interval >= 0) && !this.markedForDeletion;
        }

        private static readonly List<ActionEntry> Actions = new(64);

        public static IScheduledAction Schedule(Action action, float delay = 0)
        {
            var entry = new ActionEntry(action, delay, -1);
            Actions.Add(entry);
            return entry;
        }

        public static IScheduledAction Repeat(Action action, float interval = 0)
        {
            var entry = new ActionEntry(action, interval, interval);
            Actions.Add(entry);
            return entry;
        }

        internal static void Update()
        {
            var deltaTime = Time.deltaTime;
            for (var i = Actions.Count - 1; i >= 0; i--)
            {
                var entry = Actions[i];
                if (!entry.Active)
                {
                    Actions.RemoveAt(i);
                    continue;
                }

                if (!entry.enabled)
                {
                    entry.enabled = true;
                    continue;
                }

                entry.delay -= deltaTime;
                if (entry.delay > 0) continue;

                entry.completed = true;

                try
                {
                    entry.action();
                }
                catch (Exception ex)
                {
                    QoLFixPlugin.LogError($"An exception was caught while running a scheduled action: {ex}");
                }

                if (entry.interval < 0) Actions.RemoveAt(i);
                else entry.delay = entry.interval;
            }
        }
    }
}
