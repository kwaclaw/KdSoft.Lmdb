using System;
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
        public SpanComparison<byte> DupCompare => DupCompareHolder.Compare;

#if !NETSTANDARD2_0
        /// <summary>
        /// Duplicate (by key) data compare function that is only callable from unmanaged code.
        /// </summary>
        [CLSCompliant(false)]
        public unsafe delegate* unmanaged<DbValue, DbValue, int> UnsafeDupCompare => DupCompareHolder.UnsafeCompare;
#endif

        /// <summary>Wraps managed Key comparison function to be called from native code.</summary>
        internal CompareHolder DupCompareHolder { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="compare">Key compare function. Can be <c>null</c>.</param>
        /// <param name="dupOptions">Options to configure multi-value database.</param>
        public MultiValueDatabaseConfiguration(
            DatabaseOptions options,
            SpanComparison<byte> compare = null,
            MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None
        ) : base(options, compare) {
            this.DupOptions = dupOptions;
            this.DupCompareHolder = new CompareHolder(compare);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="compare">Key compare function. Can be <c>null</c>.</param>
        /// <param name="dupOptions">Options to configure multi-value database.</param>
        /// <param name="dupCompare">Duplicate (by key) data compare function. Can be <c>null</c>.</param>
        public MultiValueDatabaseConfiguration(
            DatabaseOptions options,
            SpanComparison<byte> compare = null,
            MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None,
            SpanComparison<byte> dupCompare = null
        ) : base(options, compare) {
            this.DupOptions = dupOptions;
            this.DupCompareHolder = new CompareHolder(dupCompare);
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="unsafeCompare">Key compare function. Mandatory.</param>
        /// <param name="dupOptions">Options to configure multi-value database.</param>
        /// <param name="unsafeDupCompare">Duplicate (by key) data compare function. Can be <c>null</c>.</param>
        [CLSCompliant(false)]
        public unsafe MultiValueDatabaseConfiguration(
            DatabaseOptions options,
            delegate* unmanaged<DbValue, DbValue, int> unsafeCompare,
            MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None,
            delegate* unmanaged<DbValue, DbValue, int> unsafeDupCompare = null
        ) : base(options, unsafeCompare) {
            this.DupOptions = dupOptions;
            this.DupCompareHolder = new CompareHolder(unsafeDupCompare);
        }
#endif
    }
}
