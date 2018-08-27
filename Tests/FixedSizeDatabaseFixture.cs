using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace KdSoft.Lmdb.Tests
{
    public class FixedSizeDatabaseFixture: EnvironmentFixture
    {
        public const string dbName = "Fixed Size Database";

        public static int IntKeyCompare(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            var xInt = BitConverter.ToInt32(x.ToArray(), 0);
            var yInt = BitConverter.ToInt32(y.ToArray(), 0);
            return Comparer<int>.Default.Compare(xInt, yInt);
        }

        public static int GuidDataCompare(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            var xStr = Encoding.UTF8.GetString(x.ToArray());
            var yStr = Encoding.UTF8.GetString(y.ToArray());
            return StringComparer.Ordinal.Compare(xStr, yStr);
        }

        public FixedSizeDatabaseFixture() {
            int recordSize = Guid.NewGuid().ToString().Length * 2;

            using (var tx = Env.BeginDatabaseTransaction(TransactionModes.None)) {
                var config = new FixedMultiValueDatabaseConfiguration(
                    DatabaseOptions.Create,
                    recordSize,
                    IntKeyCompare,
                    MultiValueDatabaseOptions.None,
                    GuidDataCompare);
                Db = tx.OpenFixedMultiValueDatabase(dbName, config);
                tx.Commit();
            }

            var stats = Env.GetStats();
            var pgSize = stats.PageSize;

            // Lets fill 100 pages with 20 GUIDS per key

            TestData = new Dictionary<int, List<string>>();

            int maxSize = 100 * pgSize;
            int totalSize = 0;

            int key = 0;
            while (totalSize < maxSize) {
                var data = new List<string>(20);
                for (int gi = 0; gi < 20; gi++) {
                    var guid = Guid.NewGuid().ToString();
                    data.Add(guid);
                    totalSize += recordSize;
                }
                TestData[key] = data;
                key++;
            }
                        }

        public Dictionary<int, List<string>> TestData { get; }

        public FixedMultiValueDatabase Db { get; }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                using (var tx = Env.BeginTransaction(TransactionModes.None)) {
                    Db.Drop(tx);
                    tx.Commit();
                }
            }
            base.Dispose(disposing);
        }
    }

    [CollectionDefinition("FixedDatabase")]
    public class FixedDatabaseGroup: ICollectionFixture<FixedSizeDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply to be
        // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
    }
}
