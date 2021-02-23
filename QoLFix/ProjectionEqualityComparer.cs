using System;
using System.Collections.Generic;

namespace QoLFix
{
    public class ProjectionEqualityComparer
    {
        public static IEqualityComparer<T> Create<T, TProperty>(Func<T, TProperty> projection, IEqualityComparer<TProperty> propertyComparer = null)
            where T : class
        {
            return new ProjectionEqualityComparer<T, TProperty>(projection, propertyComparer);
        }
    }

    public class ProjectionEqualityComparer<T, TProperty> : IEqualityComparer<T> where T : class
    {
        private readonly IEqualityComparer<TProperty> comparer;
        private readonly Func<T, TProperty> projection;

        public ProjectionEqualityComparer(Func<T, TProperty> projection, IEqualityComparer<TProperty> propertyComparer)
        {
            this.projection = projection;
            this.comparer = propertyComparer ?? EqualityComparer<TProperty>.Default;
        }

        public new bool Equals(object x, object y) => this.Equals(x as T, y as T);

        public bool Equals(T x, T y) => this.comparer.Equals(this.projection(x), this.projection(y));

        public int GetHashCode(object obj) => this.GetHashCode(obj as T);

        public int GetHashCode(T obj) => obj == null ? 0 : this.projection(obj)?.GetHashCode() ?? 0;
    }
}
