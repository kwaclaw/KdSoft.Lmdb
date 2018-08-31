
namespace KdSoft.Lmdb
{
    /// <summary>
    /// FixedMultiValueDatabase configuration
    /// </summary>
    public class FixedMultiValueDatabaseConfiguration: MultiValueDatabaseConfiguration
    {
        /// <summary>
        /// Size of records. Must be the same for all records.
        /// </summary>
        public int FixedDataSize { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Options to configure database.</param>
        /// <param name="fixedDataSize">Size of records. Must be the same for all records.</param>
        /// <param name="compare">Key compare function.</param>
        /// <param name="dupOptions">Options to configure multi-value database.</param>
        /// <param name="dupCompare">Duplicate (by key) data compare function.</param>
        public FixedMultiValueDatabaseConfiguration(
            DatabaseOptions options,
            int fixedDataSize,
            SpanComparison<byte> compare = null,
            MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None,
            SpanComparison<byte> dupCompare = null
        ) : base(options, compare, dupOptions & MultiValueDatabaseOptions.DuplicatesFixed, dupCompare) {
            this.FixedDataSize = fixedDataSize;
        }
    }
}
