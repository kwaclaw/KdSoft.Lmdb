using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("FixedDatabase")]
    public class FixedSizeDatabaseTests
    {
        readonly FixedSizeDatabaseFixture fixture;
        readonly ITestOutputHelper output;

        public FixedSizeDatabaseTests(FixedSizeDatabaseFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
        }

        [Fact]
        public void PutMultiple() {
            var putBuffer = (Span<byte>)new byte[4096];

            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                using (var cur = fixture.Db.OpenFixedMultiValueCursor(tx)) {
                    foreach (var testEntry in fixture.TestData) {
                        var key = testEntry.Key;
                        var keyBytes = BitConverter.GetBytes(key);
                        var putData = testEntry.Value;

                        for (int indx = 0; indx < putData.Count; indx++) {
                            var putDataItem = MemoryMarshal.AsBytes((ReadOnlySpan<char>)putData[indx]);
                            var putSpan = putBuffer.Slice(indx * putDataItem.Length);
                            putDataItem.CopyTo(putSpan);
                        }

                        var itemCount = putData.Count;
                        if (!cur.PutMultiple(keyBytes, putBuffer, ref itemCount))
                            break;
                    }
                }
                tx.Commit();
            }

            var testGetData = new Dictionary<int, List<string>>();
            int recordSize = ((FixedMultiValueDatabaseConfiguration)fixture.Db.Config).FixedDataSize;

            using (var tx = fixture.Env.BeginReadOnlyTransaction(TransactionModes.None)) {
                using (var cur = fixture.Db.OpenFixedMultiValueCursor(tx)) {
                    foreach (var entry in cur.ForwardByKey) {
                        int key = BitConverter.ToInt32(entry.Key);
                        var tempList = new List<string>();
                        testGetData[key] = tempList;

                        foreach (var dupData in cur.ForwardMultiple) {
                            int count = dupData.Length / recordSize;
                            for (int indx = 0; indx < count; indx++) {
                                var dupDataItem = dupData.Slice(indx * recordSize, recordSize);
                                var dupDataChars = MemoryMarshal.Cast<byte, char>(dupDataItem);
                                tempList.Add(new string(dupDataChars));
                            }
                        }
                    }
                }
                tx.Commit();
            }

            // the DB sorts the duplicate records, so we need to sort the input data for comparison
            var testCompData = new Dictionary<int, List<string>>(fixture.TestData);
            foreach (var entry in testCompData) {
                entry.Value.Sort();
            }

            Assert.Equal(testCompData, testGetData);
        }
    }
}
