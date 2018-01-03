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
                        Assert.Equal(fixture.TestData[key][0], data);
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
                    Assert.True(cursor.MoveToKey(keyBytes));
                    Assert.True(cursor.GetCurrent(out KeyDataPair entry));

                    var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                    Assert.Equal(4, ckey);
                    var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                    Assert.Equal(fixture.TestData[4][0], cdata);

                    output.WriteLine($"{ckey}: {cdata}");

                    int secondIndx = DatabaseFixture.FirstCount + DatabaseFixture.Gap;
                    if (secondIndx % 2 == 0) secondIndx++;  // make sure it is odd

                    keyBytes = BitConverter.GetBytes(secondIndx);
                    Assert.True(cursor.MoveToKey(keyBytes));
                    Assert.True(cursor.GetCurrent(out entry));

                    ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                    Assert.Equal(secondIndx, ckey);
                    cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                    Assert.Equal(fixture.TestData[secondIndx][0], cdata);

                    output.WriteLine($"{ckey}: {cdata}");
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
                    Assert.True(cursor.MoveToKey(keyBytes));

                    int indx = 0;
                    foreach (var value in cursor.ValuesForward) {
                        var cdata = Encoding.UTF8.GetString(value.ToArray());
                        Assert.Equal(fixture.TestData[secondIndx][indx], cdata);

                        indx++;
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
                    Assert.True(cursor.GetAt(keyData, out KeyDataPair entry));

                    var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                    Assert.Equal(4, ckey);
                    var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                    Assert.Equal(fixture.TestData[4][0], cdata);
                    output.WriteLine($"{ckey}: {cdata}");

                    int secondIndx = DatabaseFixture.FirstCount + DatabaseFixture.Gap;
                    if (secondIndx % 2 == 0) secondIndx++;  // make sure it is odd

                    keyBytes = BitConverter.GetBytes(secondIndx);
                    dataString = $"Test Data {secondIndx}bty"; // this is between the second and third duplicate (b, c)
                    byteCount = Encoding.UTF8.GetBytes(dataString, 0, dataString.Length, buffer, 0);
                    keyData = new KeyDataPair(keyBytes, new ReadOnlySpan<byte>(buffer, 0, byteCount));
                    Assert.True(cursor.GetNearest(keyData, out entry));

                    ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                    Assert.Equal(secondIndx, ckey);
                    cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                    Assert.Equal(fixture.TestData[secondIndx][2], cdata);  // we should be positioned at the third duplicate
                    output.WriteLine($"{ckey}: {cdata}");
                }
            }
        }

        [Fact]
        public void IterateByKeyFromKey() {
            var byKeyList = new List<(int, string)>();
            var fromNearestList = new List<(int, string)>();

            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int key = DatabaseFixture.FirstCount - 3;
                    var keyBytes = BitConverter.GetBytes(key);
                    foreach (var entry in cursor.ForwardByKeyFrom(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        byKeyList.Add((ckey, cdata));
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    output.WriteLine("================================");

                    key = DatabaseFixture.FirstCount + 1;
                    keyBytes = BitConverter.GetBytes(key);
                    foreach (var entry in cursor.ForwardByKeyFromNearest(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        fromNearestList.Add((ckey, cdata));
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }

            int startKey = (DatabaseFixture.FirstCount - 3);
            var orderedByKeys = fixture.TestData.Keys.Where(k => k >= startKey).OrderBy(k => k).ToList();
            Assert.Equal(orderedByKeys.Count, byKeyList.Count);
            for (int indx = 0; indx < orderedByKeys.Count; indx++) {
                var compKey = orderedByKeys[indx];
                Assert.Equal(compKey, byKeyList[indx].Item1);
                Assert.Equal(fixture.TestData[compKey][0], byKeyList[indx].Item2);
            }

            startKey = (DatabaseFixture.FirstCount + DatabaseFixture.Gap);
            var fromNearestKeys = fixture.TestData.Keys.Where(k => k >= startKey).OrderBy(k => k).ToList();
            for (int indx = 0; indx < fromNearestKeys.Count; indx++) {
                var compKey = fromNearestKeys[indx];
                Assert.Equal(compKey, fromNearestList[indx].Item1);
                Assert.Equal(fixture.TestData[compKey][0], fromNearestList[indx].Item2);
            }
        }

        [Fact]
        public void IterateFromEntry() {
            var entryList = new List<(int, string)>();
            var fromNearestList = new List<(int, string)>();

            using (var tx = fixture.Env.BeginReadOnlyTransaction()) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    int key = DatabaseFixture.FirstCount - 4;
                    var keyBytes = BitConverter.GetBytes(key);
                    var data = Encoding.UTF8.GetBytes(fixture.TestData[key][1]);
                    foreach (var entry in cursor.ForwardFrom(new KeyDataPair(keyBytes, data))) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        entryList.Add((ckey, cdata));
                        output.WriteLine($"{ckey}: {cdata}");
                    }

                    output.WriteLine("================================");

                    key = DatabaseFixture.FirstCount + 1;
                    keyBytes = BitConverter.GetBytes(key);
                    // if we used ForwardFromNearest(in KeyDataPair) then we would need to start at an existing key
                    foreach (var entry in cursor.ForwardFromNearest(keyBytes)) {
                        var ckey = BitConverter.ToInt32(entry.Key.ToArray(), 0);
                        var cdata = Encoding.UTF8.GetString(entry.Data.ToArray());
                        fromNearestList.Add((ckey, cdata));
                        output.WriteLine($"{ckey}: {cdata}");
                    }
                }
            }

            int startKey = (DatabaseFixture.FirstCount - 4);
            string startData = fixture.TestData[startKey][1];
            var orderedKeys = fixture.TestData.Keys.Where(k => k >= startKey).OrderBy(k => k).ToList();
            var expectedList = new List<(int, string)>();
            foreach (var key in orderedKeys) {
                var dataList = fixture.TestData[key];
                for (int indx = 0; indx < dataList.Count; indx++) {
                    // we start at the second value of the first key
                    if (key == startKey && indx == 0)
                        continue;
                    expectedList.Add((key, dataList[indx]));
                }
            }
            Assert.Equal(expectedList, entryList);

            startKey = (DatabaseFixture.FirstCount + DatabaseFixture.Gap);
            startData = fixture.TestData[startKey][0];
            var fromNearestKeys = fixture.TestData.Keys.Where(k => k >= startKey).OrderBy(k => k).ToList();
            expectedList.Clear();
            foreach (var key in fromNearestKeys) {
                // we are not skipping any values
                foreach (var data in fixture.TestData[key])
                    expectedList.Add((key, data));
            }
            Assert.Equal(expectedList, fromNearestList);
        }

        [Fact]
        public void IterateFromEntryReverse() {
            //TODO add Assert checks
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
