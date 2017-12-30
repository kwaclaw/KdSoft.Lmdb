using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var config = new Database.Configuration(DatabaseOptions.Create);
            Database dbase;
            using (var tx = fixture.Env.BeginOpenDbTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb1", config);
                tx.Commit();
            }

            var dbs = fixture.Env.GetDatabases();
            foreach (var db in dbs)
                output.WriteLine($"Database '{db.Name}'");

            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                dbase.Drop(tx);
                tx.Commit();
            }

            Assert.Empty(fixture.Env.GetDatabases());
        }

        const string testData = "Test Data";

        [Fact]
        public void SimpleStoreRetrieve() {
            var config = new Database.Configuration(DatabaseOptions.Create);
            Database dbase;
            Statistics stats;
            using (var tx = fixture.Env.BeginOpenDbTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb2", config);
                stats = dbase.GetStats(tx);
                tx.Commit();
            }

            var keyBuf1 = Guid.NewGuid().ToByteArray();
            var keyBuf2 = Guid.NewGuid().ToByteArray();
            var buffer = fixture.Buffers.Acquire(1024);
            try {
                var putData1 = testData.AsReadOnlySpan().NonPortableCast<char, byte>();
                int byteCount = Encoding.UTF8.GetBytes(testData, 0, testData.Length, buffer, 0);
                var putData2 = new ReadOnlySpan<byte>(buffer, 0, byteCount);

                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    dbase.Put(tx, keyBuf1, putData1, PutOptions.None);
                    dbase.Put(tx, keyBuf2, putData2, PutOptions.None);
                    tx.Commit();
                }

                ReadOnlySpan<byte> getData1;
                ReadOnlySpan<byte> getData2;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    Assert.True(dbase.Get(tx, keyBuf1, out getData1));
                    Assert.True(dbase.Get(tx, keyBuf2, out getData2));
                    tx.Commit();
                }

                Assert.True(putData1.SequenceEqual(getData1));
                Assert.Equal(testData, Encoding.UTF8.GetString(getData2.ToArray()));
            }
            finally {
                fixture.Buffers.Return(buffer);
                using (var tx = fixture.Env.BeginOpenDbTransaction(TransactionModes.None)) {
                    dbase.Drop(tx);
                    tx.Commit();
                }
            }
        }
    }
}
