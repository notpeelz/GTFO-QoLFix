using System;

namespace QoLFix.Patches.Common
{
    public class SharedResourceLease : IDisposable
    {
        public SharedResourceLease(SharedResource owner)
        {
            this.Owner = owner;
        }

        protected internal SharedResource Owner { get; private set; }

        public void Dispose()
        {
            this.Owner.Release(this);
        }
    }
}
