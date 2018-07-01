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
        public DatabaseOptions Options { get; }
        public SpanComparison<byte> Compare { get; }

        [CLSCompliant(false)]
        internal protected DbLibCompareFunction LibCompare { get; protected set; }

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
