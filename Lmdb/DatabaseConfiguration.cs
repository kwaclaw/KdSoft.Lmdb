using System;
using System.Runtime.CompilerServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Database configuration
    /// </summary>
    public class DatabaseConfiguration
    {
        /// <summary>
        /// Options to configure database.
        /// </summary>
        public DatabaseOptions Options { get; }

        /// <summary>
        /// Key comparison function.
        /// </summary>
        public SpanComparison<byte> Compare => CompareHolder.Compare;

#if !NETSTANDARD2_0
        /// <summary>
        /// Key comparison function that is only callable from unmanaged code.
        /// </summary>
        [CLSCompliant(false)]
        public unsafe delegate* unmanaged<DbValue, DbValue, int> UnsafeCompare => CompareHolder.UnsafeCompare;
#endif

        /// <summary>Wraps managed Key comparison function to be called from native code.</summary>
        internal CompareHolder CompareHolder { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="compare">Key comparison function. Can be <c>null</c>.</param>
        public DatabaseConfiguration(DatabaseOptions options, SpanComparison<byte> compare = null) {
            this.Options = options;
            this.CompareHolder = new CompareHolder(compare);
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="unsafeCompare">Key comparison function. Mandatory.</param>
        [CLSCompliant(false)]
        public unsafe DatabaseConfiguration(DatabaseOptions options, delegate* unmanaged<DbValue, DbValue, int> unsafeCompare) {
            this.Options = options;
            this.CompareHolder = new CompareHolder(unsafeCompare);
        }
#endif
    }

    class CompareHolder
    {
        /// <summary>
        /// Key comparison function.
        /// </summary>
        public SpanComparison<byte> Compare { get; }

#if !NETSTANDARD2_0
        /// <summary>
        /// Key comparison function that is only callable from unmanaged code.
        /// </summary>
        public unsafe delegate* unmanaged<DbValue, DbValue, int> UnsafeCompare { get; }
#endif
        /// <summary>
        /// Key comparison function for internal use only.
        /// </summary>
        public DbLibCompareFunction LibCompare { get; }

        public CompareHolder(SpanComparison<byte> compare = null) {
            this.Compare = compare;
            if (compare != null)
                this.LibCompare = CompareWrapper;
        }

#if !NETSTANDARD2_0
        public unsafe CompareHolder(delegate* unmanaged<DbValue, DbValue, int> unsafeCompare) {
            this.UnsafeCompare = unsafeCompare;
        }
#endif

        // no check for Compare == null
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CompareWrapper(in DbValue x, in DbValue y) {
            return Compare(x.ToReadOnlySpan(), y.ToReadOnlySpan());
        }
    }
}
