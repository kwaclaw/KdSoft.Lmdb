using System;
using System.Linq;
using Google.FlatBuffers;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests.fbs
{
    [Collection("Environment")]
    public class DatabaseFlatBufferTests
    {
        readonly EnvironmentFixture fixture;
        readonly ITestOutputHelper output;

        public DatabaseFlatBufferTests(EnvironmentFixture fixture, ITestOutputHelper output) {
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

            var buffer = fixture.Buffers.Acquire(1024);
            try {
                FlatBufferBuilder liBuilder = new FlatBufferBuilder(64);
                FlatBufferBuilder liKeyBuilder = new FlatBufferBuilder(32);

                // in FlatBuffers, each serializable item is backed by a byte buffer, the item itself is a struct
                byte[] liBytes;
                byte[] li2Bytes;
                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    var prodCode = liKeyBuilder.CreateString("GIN");
                    var lik = LineItemKey.CreateLineItemKey(liKeyBuilder, prodCode, 1);
                    liKeyBuilder.Finish(lik.Value);

                    prodCode = liBuilder.CreateString("GIN");
                    lik = LineItemKey.CreateLineItemKey(liBuilder, prodCode, 1);
                    var li = LineItem.CreateLineItem(liBuilder, lik, 24);
                    liBuilder.Finish(li.Value);

                    liBytes = liBuilder.SizedByteArray();

                    // lineItem points into the liBuilder's data buffer
                    var lineItem = LineItem.GetRootAsLineItem(liBuilder.DataBuffer);

                    var keySpan = liKeyBuilder.DataBuffer.AsBytes();
                    var dataSpan = liBuilder.DataBuffer.AsBytes();
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    liKeyBuilder.Clear();
                    liBuilder.Clear();

                    prodCode = liKeyBuilder.CreateString("WHISKY");
                    lik = LineItemKey.CreateLineItemKey(liKeyBuilder, prodCode, 2);
                    liKeyBuilder.Finish(lik.Value);

                    prodCode = liBuilder.CreateString("WHISKY");
                    lik = LineItemKey.CreateLineItemKey(liBuilder, prodCode, 2);
                    li = LineItem.CreateLineItem(liBuilder, lik, 32);
                    liBuilder.Finish(li.Value);

                    li2Bytes = liBuilder.SizedByteArray();

                    keySpan = liKeyBuilder.DataBuffer.AsBytes();
                    dataSpan = liBuilder.DataBuffer.AsBytes();
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    tx.Commit();
                }

                byte[] liOutBytes;
                byte[] liOut2Bytes;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    liKeyBuilder.Clear();
                    var prodCode = liKeyBuilder.CreateString("GIN");
                    var lik = LineItemKey.CreateLineItemKey(liKeyBuilder, prodCode, 1);
                    liKeyBuilder.Finish(lik.Value);

                    var keySpan = liKeyBuilder.DataBuffer.AsBytes();

                    Assert.True(dbase.Get(tx, keySpan, out ReadOnlySpan<byte> dataSpan));
                    liOutBytes = dataSpan.ToArray();

                    // lineItemOut points into liOutBytes
                    var lineItemOut = LineItem.GetRootAsLineItem(new ByteBuffer(liOutBytes));

                    liKeyBuilder.Clear();
                    prodCode = liKeyBuilder.CreateString("WHISKY");
                    lik = LineItemKey.CreateLineItemKey(liKeyBuilder, prodCode, 2);
                    liKeyBuilder.Finish(lik.Value);

                    keySpan = liKeyBuilder.DataBuffer.AsBytes();

                    Assert.True(dbase.Get(tx, keySpan, out dataSpan));
                    liOut2Bytes = dataSpan.ToArray();

                    tx.Commit();
                }

                Assert.True(liBytes.SequenceEqual(liOutBytes));
                Assert.True(li2Bytes.SequenceEqual(liOut2Bytes));
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
