namespace QoLFix.UI
{
    public interface IScheduledAction
    {
        bool Active { get; }

        void Invalidate();
    }
}
