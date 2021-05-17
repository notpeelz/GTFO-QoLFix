using System;
using System.Collections.Generic;

namespace QoL.Common
{
    public static class ProjectionEqualityComparer
    {
        public static IEqualityComparer<T> Create<T, TProperty>(Func<T, TProperty> projection, IEqualityComparer<TProperty>? propertyComparer = null)
            where T : class
        {
            return new ProjectionEqualityComparer<T, TProperty>(projection, propertyComparer);
        }
    }

    public class ProjectionEqualityComparer<T, TProperty> : IEqualityComparer<T> where T : class
    {
        private readonly IEqualityComparer<TProperty> comparer;
        private readonly Func<T, TProperty> projection;

        public ProjectionEqualityComparer(Func<T, TProperty> projection, IEqualityComparer<TProperty>? propertyComparer)
        {
            this.projection = projection;
            this.comparer = propertyComparer ?? EqualityComparer<TProperty>.Default;
        }

        public bool Equals(T x, T y) => this.comparer.Equals(this.projection(x), this.projection(y));

        public int GetHashCode(T obj) => obj == null ? 0 : this.projection(obj)?.GetHashCode() ?? 0;
    }
}
