using System;
using System.Collections.Generic;
using System.Linq;

namespace QoLFix.Patches.Common
{
    public class SharedResource<T> : SharedResource
        where T : SharedResourceLease, new()
    {
        public new IEnumerable<T> Leases => base.Leases.Cast<T>();

        public new T AcquireLease() => (T)this.AcquireLeaseImpl();

        protected override SharedResourceLease AcquireLeaseImpl()
        {
            var res = new T();
            this.AddLease(res);
            return res;
        }
    }

    public class SharedResource
    {
        private readonly List<SharedResourceLease> leases = new();

        public SharedResource() { }

        public IEnumerable<SharedResourceLease> Leases => this.leases.AsReadOnly();

        public SharedResourceLease AcquireLease() => this.AcquireLeaseImpl();

        public void Release(SharedResourceLease lease) => this.ReleaseImpl(lease);

        protected virtual SharedResourceLease AcquireLeaseImpl()
        {
            var res = new SharedResourceLease(this);
            this.AddLease(res);
            return res;
        }

        protected virtual void ReleaseImpl(SharedResourceLease lease) => this.RemoveLease(lease);

        public bool InUse => this.leases.Count > 0;

        protected void AddLease(SharedResourceLease lease) => this.leases.Add(lease);

        protected void RemoveLease(SharedResourceLease lease)
        {
            if (lease.Owner != this)
            {
                throw new InvalidOperationException($"The resource doesn't belong to this {nameof(SharedResource)}.");
            }
            this.leases.Remove(lease);
        }
    }
}
