using System;
using System.Runtime.CompilerServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// MultiValueDatabase configuration
    /// </summary>
    public class MultiValueDatabaseConfiguration: DatabaseConfiguration
    {
        /// <summary>
        /// Options to configure multi-value database.
        /// </summary>
        public MultiValueDatabaseOptions DupOptions { get; }

        /// <summary>
        /// Duplicate (by key) data compare function.
        /// </summary>
        public SpanComparison<byte> DupCompare { get; }

        [CLSCompliant(false)]
        internal protected DbLibCompareFunction LibDupCompare { get; protected set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="compare">Key compare function.</param>
        /// <param name="dupOptions">Options to configure multi-value database.</param>
        /// <param name="dupCompare">Duplicate (by key) data compare function.</param>
        public MultiValueDatabaseConfiguration(
            DatabaseOptions options,
            SpanComparison<byte> compare = null,
            MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None,
            SpanComparison<byte> dupCompare = null
        ) : base(options, compare) {
            this.DupOptions = dupOptions;
            this.DupCompare = dupCompare;
            if (dupCompare != null)
                this.LibDupCompare = DupCompareWrapper;
        }

        // no check for Compare == null
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int DupCompareWrapper(in DbValue x, in DbValue y) {
            return DupCompare(x.ToReadOnlySpan(), y.ToReadOnlySpan());
        }
    }
}
