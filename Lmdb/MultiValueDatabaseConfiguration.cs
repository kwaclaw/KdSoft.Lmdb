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
        public MultiValueDatabaseOptions DupOptions { get; }
        public SpanComparison<byte> DupCompare { get; }

        [CLSCompliant(false)]
        internal protected DbLibCompareFunction LibDupCompare { get; protected set; }

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
