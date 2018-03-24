using System;
using Google.Protobuf;
using KdSoft.Lmdb.Tests.proto;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("Environment")]
    public class DatabaseProtobufTests
    {
        readonly EnvironmentFixture fixture;
        readonly ITestOutputHelper output;

        public DatabaseProtobufTests(EnvironmentFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
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

            var lik = new LineItemKey { OrderId = 1, ProdCode = "GIN" };
            var li = new LineItem { Key = lik, Quantity = 8 };
            var lik2 = new LineItemKey { OrderId = 2, ProdCode = "WHISKY" };
            var li2 = new LineItem { Key = lik2, Quantity = 24 };

            var buffer = fixture.Buffers.Acquire(1024);
            try {
                using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                    var outStream = new CodedOutputStream(buffer);
                    lik.WriteTo(outStream);
                    var likPos = (int)outStream.Position;
                    li.WriteTo(outStream);
                    var liPos = (int)outStream.Position;
                    var keySpan = new ReadOnlySpan<byte>(buffer, 0, likPos);
                    var dataSpan = new ReadOnlySpan<byte>(buffer, likPos, liPos - likPos);
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    // CodedOutputStream intances cannot be reused
                    outStream = new CodedOutputStream(buffer);
                    lik2.WriteTo(outStream);
                    likPos = (int)outStream.Position;
                    li2.WriteTo(outStream);
                    liPos = (int)outStream.Position;
                    keySpan = new ReadOnlySpan<byte>(buffer, 0, likPos);
                    dataSpan = new ReadOnlySpan<byte>(buffer, likPos, liPos - likPos);
                    dbase.Put(tx, keySpan, dataSpan, PutOptions.None);

                    tx.Commit();
                }

                LineItem liOut;
                LineItem liOut2;
                using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                    var keyStream = new CodedOutputStream(buffer);
                    lik.WriteTo(keyStream);
                    var likPos = (int)keyStream.Position;
                    var keySpan = new ReadOnlySpan<byte>(buffer, 0, likPos);
                    Assert.True(dbase.Get(tx, keySpan, out ReadOnlySpan<byte> dataSpan));
                    var dataBytes = dataSpan.ToArray();
                    liOut = LineItem.Parser.ParseFrom(dataBytes);

                    keyStream = new CodedOutputStream(buffer);
                    lik2.WriteTo(keyStream);
                    likPos = (int)keyStream.Position;
                    keySpan = new ReadOnlySpan<byte>(buffer, 0, likPos);
                    Assert.True(dbase.Get(tx, keySpan, out dataSpan));
                    liOut2 = LineItem.Parser.ParseFrom(dataSpan.ToArray());

                    tx.Commit();
                }

                Assert.True(li.Equals(liOut));
                Assert.True(li2.Equals(liOut2));
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
