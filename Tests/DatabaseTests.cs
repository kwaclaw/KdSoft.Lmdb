using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("Environment")]
    public class DatabaseTests
    {
        readonly EnvironmentFixture fixture;
        readonly ITestOutputHelper output;

        public DatabaseTests(EnvironmentFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
        }

        [Fact]
        public void OpenDatabase() {
            var config = new DatabaseConfiguration(DatabaseOptions.Create);
            Database dbase;
            using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb1", config);
                tx.Commit();
            }

            var dbs = fixture.Env.Databases;
            foreach (var db in dbs)
                output.WriteLine($"Database '{db.Name}'");

            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                dbase.Drop(tx);
                tx.Commit();
            }

            Assert.Empty(fixture.Env.Databases);
        }

        const string testData = "Test Data";

        [Fact]
        public void BasicStoreRetrieve() {
            var config = new DatabaseConfiguration(DatabaseOptions.Create);
            Database dbase;
            using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("SimpleStoreRetrieve", config);
                tx.Commit();
            }

            int key = 234;
            var keyBuf = BitConverter.GetBytes(key);
            string putData = testData;
            try {
                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    dbase.Put(tx, keyBuf, Encoding.UTF8.GetBytes(putData), PutOptions.None);
                    tx.Commit();
                }

                ReadOnlySpan<byte> getData;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    Assert.True(dbase.Get(tx, keyBuf, out getData));
                    tx.Commit();
                }

                Assert.Equal(putData, Encoding.UTF8.GetString(getData));
            }
            finally {
                using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                    dbase.Drop(tx);
                    tx.Commit();
                }
            }
        }

        [Fact]
        public void SimpleStoreRetrieve() {
            var config = new DatabaseConfiguration(DatabaseOptions.Create);
            Database dbase;
            Statistics stats;
            using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb2", config);
                stats = dbase.GetStats(tx);
                tx.Commit();
            }

            output.WriteLine($"Entries: {stats.Entries}");
            output.WriteLine($"Depth: {stats.Depth}");
            output.WriteLine($"PageSize: {stats.PageSize}");
            output.WriteLine($"BranchPages: {stats.BranchPages}");
            output.WriteLine($"LeafPages: {stats.LeafPages}");
            output.WriteLine($"OverflowPages: {stats.OverflowPages}");

            var buffer = fixture.Buffers.Acquire(1024);  // re-usable buffer

            var key1 = Guid.NewGuid();
            var key2 = Guid.NewGuid();
            try {
                // use the same buffer for some keys and data
                int bufPos = 0;
                var keySpan1 = new Span<byte>(buffer, bufPos, 16);
                key1.TryWriteBytes(keySpan1);
                bufPos += 16;
                var keySpan2 = new Span<byte>(buffer, bufPos, 16);
                key2.TryWriteBytes(keySpan2);
                bufPos += 16;
                // this one encoded as UTF-8
                int byteCount = Encoding.UTF8.GetBytes(testData, 0, testData.Length, buffer, bufPos);
                var putData2 = new ReadOnlySpan<byte>(buffer, bufPos, byteCount);
                // this one encoded as UTF-16, we can access the memory directly
                var putData1 = MemoryMarshal.AsBytes(testData.AsSpan());

                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    dbase.Put(tx, keySpan1, putData1, PutOptions.None);
                    dbase.Put(tx, keySpan2, putData2, PutOptions.None);
                    tx.Commit();
                }

                ReadOnlySpan<byte> getData1;
                ReadOnlySpan<byte> getData2;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    Assert.True(dbase.Get(tx, key1.ToByteArray(), out getData1));
                    Assert.True(dbase.Get(tx, key2.ToByteArray(), out getData2));
                    tx.Commit();
                }

                Assert.True(putData1.SequenceEqual(getData1));
                Assert.Equal(testData, Encoding.UTF8.GetString(getData2));
            }
            finally {
                fixture.Buffers.Return(buffer);
                using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                    dbase.Drop(tx);
                    tx.Commit();
                }
            }
        }

        static int IntKeyCompare(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            var xInt = BitConverter.ToInt32(x.ToArray(), 0);
            var yInt = BitConverter.ToInt32(y.ToArray(), 0);
            return Comparer<int>.Default.Compare(xInt, yInt);
        }

        [Fact]
        public void UseCompareFunction() {
            var config = new DatabaseConfiguration(DatabaseOptions.Create, IntKeyCompare);
            Database dbase;
            using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb3", config);
                tx.Commit();
            }

            var buffer = fixture.Buffers.Acquire(1024);
            try {
                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    for (int key = 0; key < 10; key++) {
                        var putData = $"Test Data {key}";
                        int byteCount = Encoding.UTF8.GetBytes(putData, 0, putData.Length, buffer, 0);
                        dbase.Put(tx, BitConverter.GetBytes(key), new ReadOnlySpan<byte>(buffer, 0, byteCount), PutOptions.None);
                    }
                    tx.Commit();
                }

                ReadOnlySpan<byte> getData;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    for (int key = 0; key < 10; key++) {
                        var compareData = $"Test Data {key}";
                        dbase.Get(tx, BitConverter.GetBytes(key), out getData);
                        Assert.Equal(compareData, Encoding.UTF8.GetString(getData));

                        // check if two adjacent keys are comparing correctly when using the database's compare function
                        if (key > 0) {
                            int compResult = dbase.Compare(tx, BitConverter.GetBytes(key), BitConverter.GetBytes(key - 1));
                            Assert.True(compResult > 0);
                        }
                    }
                    tx.Commit();
                }
            }
            finally {
                fixture.Buffers.Return(buffer);
                using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                    dbase.Drop(tx);
                    tx.Commit();
                }
            }
        }
    }
}
