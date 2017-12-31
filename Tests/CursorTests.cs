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
        public void BasicIteration() {
            var getData = new Dictionary<int, IList<string>>();
            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                using (var cursor = fixture.Db.OpenCursor(tx)) {
                    foreach (var entry in cursor.KeysForward) {
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
        public void MultiValueIteration() {
            var getData = new Dictionary<int, IList<string>>();
            using (var tx = fixture.Env.BeginTransaction(TransactionModes.None)) {
                using (var cursor = fixture.Db.OpenMultiValueCursor(tx)) {
                    foreach (var keyEntry in cursor.KeysForward) {
                        var key = BitConverter.ToInt32(keyEntry.Key.ToArray(), 0);
                        var valueList = new List<string>();
                        foreach (var valueEntry in cursor.ValuesForward) {
                            var data = Encoding.UTF8.GetString(valueEntry.Data.ToArray());
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
    }
}
