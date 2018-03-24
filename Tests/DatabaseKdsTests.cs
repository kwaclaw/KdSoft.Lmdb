using System;
using KdSoft.Lmdb.Tests.kds;
using KdSoft.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("Environment")]
    public class DatabaseKdsTests
    {
        readonly EnvironmentFixture fixture;
        readonly ITestOutputHelper output;

        public DatabaseKdsTests(EnvironmentFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
        }

        [Fact]
        public void SimpleStoreRetrieve() {
            var config = new DatabaseConfiguration(DatabaseOptions.Create);
            Database dbase;
            Statistics stats;
            using (var tx = fixture.Env.BeginDatabaseTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb3", config);
                stats = dbase.GetStats(tx);
                tx.Commit();
            }

            var fmt = new StdFormatter(ByteConverter.SystemByteOrder);

            // instantiate and register Field instances
            var likField = new LineItemKeyField(fmt, true);
            var limField = new LineItemField(fmt, true);

            var lik = new LineItemKey { OrderId = 1, ProdCode = "GIN" };
            var lim = new LineItem { Key = lik, Quantity = 8 };
            var lik2 = new LineItemKey { OrderId = 2, ProdCode = "WHISKY" };
            var lim2 = new LineItem { Key = lik2, Quantity = 24 };

            var buffer = fixture.Buffers.Acquire(1024);
            try {
                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    var target = new Span<byte>(buffer);

                    // use convenience API
                    int bufPos = 0;
                    fmt.SerializeObject(target, lik, ref bufPos);
                    var limPos = bufPos;
                    fmt.SerializeObject(target, lim, ref bufPos);

                    var keySpan = target.Slice(0, limPos);
                    var dataSpan = target.Slice(limPos, bufPos - limPos);
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    // use explicit API
                    fmt.InitSerialization(0);
                    fmt.SerializeObject(target, lik2);
                    limPos = fmt.FinishSerialization(target);

                    fmt.InitSerialization(limPos);
                    fmt.SerializeObject(target, lim2);
                    bufPos = fmt.FinishSerialization(target);

                    keySpan = target.Slice(0, limPos);
                    dataSpan = target.Slice(limPos, bufPos - limPos);
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    tx.Commit();
                }

                LineItem limOut;
                LineItem limOut2;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    var target = new Span<byte>(buffer);

                    // use explicit API
                    fmt.InitSerialization(0);
                    fmt.SerializeObject(target, lik);
                    int bufPos = fmt.FinishSerialization(target);
                    var keySpan = target.Slice(0, bufPos);

                    Assert.True(dbase.Get(tx, keySpan, out ReadOnlySpan<byte> dataSpan));
                    fmt.InitDeserialization(dataSpan, 0);
                    limOut = fmt.DeserializeObject<LineItem>(dataSpan);
                    bufPos = fmt.FinishDeserialization();

                    // use convenience API
                    bufPos = 0;
                    fmt.SerializeObject(target, lik2, ref bufPos);
                    keySpan = target.Slice(0, bufPos);

                    Assert.True(dbase.Get(tx, keySpan, out dataSpan));

                    bufPos = 0;
                    limOut2 = fmt.DeserializeObject<LineItem>(dataSpan, ref bufPos);

                    tx.Commit();
                }

                Assert.True(lim.Equals(limOut));
                Assert.True(lim2.Equals(limOut2));
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
