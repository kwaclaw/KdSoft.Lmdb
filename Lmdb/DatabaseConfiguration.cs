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
        public SpanComparison<byte> Compare { get; }

        /// <summary>Wraps managed Key comparison function to be called from native code.</summary>
        [CLSCompliant(false)]
        internal protected DbLibCompareFunction LibCompare { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="compare">Key comparison function.</param>
        public DatabaseConfiguration(DatabaseOptions options, SpanComparison<byte> compare = null) {
            this.Options = options;
            this.Compare = compare;
            if (compare != null)
                this.LibCompare = CompareWrapper;
        }

        // no check for Compare == null
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int CompareWrapper(in DbValue x, in DbValue y) {
            return Compare(x.ToReadOnlySpan(), y.ToReadOnlySpan());
        }
    }
}
