using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("Database")]
    public class CursorTests
    {
        readonly DatabaseFixture fixture;
        readonly ITestOutputHelper output;

        public CursorTests(DatabaseFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
        }

        [Fact]
        public void BasicIterationByKey() {
            var getData = new Dictionary<int, IList<string>>();
            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    foreach (var entry in cursor.ForwardByKey) {
                        var key = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var data = Encoding.UTF8.GetString(entry.Data.ToArray());
                        getData[key] = new[] { data };
                    }
                }
            }
            foreach (var entry in getData) {
                output.WriteLine($"{entry.Key}: {entry.Value[0]}");
            }
        }

        [Fact]
        public void MultiValueIterationByKey() {
            var getData = new Dictionary<int, IList<string>>();
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    foreach (var keyEntry in cursor.ForwardByKey) {
                        var key = BitConverter.ToInt32(keyEntry.Key.ToArray(), 0);
                        var valueList = new List<string>();
                        foreach (var value in cursor.ValuesForward) {
                            var data = Encoding.UTF8.GetString(value.ToArray());
                            valueList.Add(data);
                        }
                        getData[key] = valueList;
                    }
                }
            }
            foreach (var entry in getData) {
                output.WriteLine($"{entry.Key}:");
                foreach (var val in entry.Value)
                    output.WriteLine($"\t\t{val}");
            }

            Assert.Equal(fixture.TestData, getData);
        }

        [Fact]
        public void MultiValueIteration() {
            var getData = new Dictionary<int, IList<string>>();
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    foreach (var entry in cursor.Forward) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                        if (getData.TryGetValue(ckey, out IList<string> dataList))
                            dataList.Add(cdata);
                        else
                            getData[ckey] = new List<string> { cdata };
                    }
                }
            }

            Assert.Equal(fixture.TestData, getData);
        }


        [Fact]
        public void MoveToKey() {
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    var keyBytes = BitConverter.GetBytes(4);
                    cursor.MoveToKey(keyBytes);
                    if (cursor.GetCurrent(out KeyDataPair entry)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    int secondIndx = DatabaseFixture.FirstCount + DatabaseFixture.Gap;
                    if (secondIndx % 2 == 0) secondIndx++;  // make sure it is odd

                    keyBytes = BitConverter.GetBytes(secondIndx);
                    cursor.MoveToKey(keyBytes);
                    if (cursor.GetCurrent(out entry)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }
        }

        [Fact]
        public void MoveToKeyIterateMultiple() {
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int secondIndx = DatabaseFixture.FirstCount + DatabaseFixture.Gap;
                    if (secondIndx % 2 == 0) secondIndx++;  // make sure it is odd

                    var keyBytes = BitConverter.GetBytes(secondIndx);
                    cursor.MoveToKey(keyBytes);
                    foreach (var value in cursor.ValuesForward) {
                        var cdata = Encoding.UTF8.GetString(value.ToArray());
                        output.WriteLine($"{secondIndx}: {cdata}");
                    }
                }
            }
        }

        [Fact]
        public void MoveToData() {
            var buffer = new byte[1024];
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    var keyBytes = BitConverter.GetBytes(4);
                    var dataString = fixture.TestData[4][0];
                    int byteCount = Encoding.UTF8.GetBytes(dataString, 0, dataString.Length, buffer, 0);
                    var keyData = new KeyDataPair(keyBytes, new ReadOnlySpan<byte>(buffer, 0, byteCount));
                    if (cursor.GetAt(keyData, out KeyDataPair entry)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    int secondIndx = DatabaseFixture.FirstCount + DatabaseFixture.Gap;
                    if (secondIndx % 2 == 0) secondIndx++;  // make sure it is odd

                    keyBytes = BitConverter.GetBytes(secondIndx);
                    dataString = $"Test Data {secondIndx}bty";
                    byteCount = Encoding.UTF8.GetBytes(dataString, 0, dataString.Length, buffer, 0);
                    keyData = new KeyDataPair(keyBytes, new ReadOnlySpan<byte>(buffer, 0, byteCount));
                    if (cursor.GetNearest(keyData, out entry)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }
        }

        [Fact]
        public void IterateByKeyFromKey() {
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int key = 4;
                    var keyBytes = BitConverter.GetBytes(key);
                    foreach (var entry in cursor.ForwardByKeyFrom(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    output.WriteLine("================================");

                    key = DatabaseFixture.FirstCount + 1;
                    keyBytes = BitConverter.GetBytes(key);
                    foreach (var entry in cursor.ForwardByKeyFromNearest(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }
        }

        [Fact]
        public void IterateFromEntry() {
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int key = 3;
                    var keyBytes = BitConverter.GetBytes(key);
                    var data = Encoding.UTF8.GetBytes(fixture.TestData[key][1]);
                    foreach (var entry in cursor.ForwardFrom(new KeyDataPair(keyBytes, data))) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    output.WriteLine("================================");

                    key = DatabaseFixture.FirstCount + 1;
                    keyBytes = BitConverter.GetBytes(key);
                    // if we used ForwardFromNearestEntry then at least the key would have to match
                    foreach (var entry in cursor.ForwardFromNearest(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }
        }

        [Fact]
        public void IterateFromEntryReverse() {
            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int key = 3;
                    var keyBytes = BitConverter.GetBytes(key);
                    var data = Encoding.UTF8.GetBytes(fixture.TestData[key][2]);
                    foreach (var entry in cursor.ReverseFrom(new KeyDataPair(keyBytes, data))) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    output.WriteLine("================================");

                    key = DatabaseFixture.FirstCount + 1;
                    keyBytes = BitConverter.GetBytes(key);
                    // if we used ForwardFromNearestEntry then at least the key would have to match
                    foreach (var entry in cursor.ReverseFromNearest(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }
        }
    }
}
