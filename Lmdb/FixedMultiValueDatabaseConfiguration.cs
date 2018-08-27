
namespace KdSoft.Lmdb
{
    /// <summary>
    /// FixedMultiValueDatabase configuration
    /// </summary>
    public class FixedMultiValueDatabaseConfiguration: MultiValueDatabaseConfiguration
    {
        public int FixedDataSize { get; }

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
