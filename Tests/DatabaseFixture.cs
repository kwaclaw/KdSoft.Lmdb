using System;
using System.Collections.Generic;
using System.Text;
using KdSoft.Utils;
using Xunit;

namespace KdSoft.Lmdb.Tests
{
    public class DatabaseFixture: EnvironmentFixture, IDisposable
    {
        public const string dbName = "Test Read Database";
        public const int FirstCount = 7;
        public const int Gap = 12;
        public const int SecondCount = 5;

        public int IntKeyCompare(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            var xInt = BitConverter.ToInt32(x.ToArray(), 0);
            var yInt = BitConverter.ToInt32(y.ToArray(), 0);
            return Comparer<int>.Default.Compare(xInt, yInt);
        }

        public int StringDataCompare(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            var xStr = Encoding.UTF8.GetString(x.ToArray());
            var yStr = Encoding.UTF8.GetString(y.ToArray());
            return StringComparer.Ordinal.Compare(xStr, yStr);
        }

        public DatabaseFixture() {
            using (var tx = Env.BeginDatabaseTransaction(TransactionModes.None)) {
                var config = new MultiValueDatabase.Configuration(
                    DatabaseOptions.Create,
                    IntKeyCompare,
                    MultiValueDatabaseOptions.None,
                    StringDataCompare);
                Db = tx.OpenMultiValueDatabase(dbName, config);
                tx.Commit();
            }

            TestData = new Dictionary<int, IList<string>>();


            for (int key = 0; key < FirstCount; key++) {
                if (key % 2 == 0) {  // if even key
                    string putData = $"Test Data {key}";
                    TestData[key] = new[] { putData };
                }
                else {
                    var putData = new string[key];
                    char dupLetter = 'a';
                    for (int indx = 0; indx < key; indx++) {
                        putData[indx] = $"Test Data {key}{dupLetter}";
                        dupLetter++;
                    }
                    TestData[key] = putData;
                }
            }

            int secondStart = FirstCount + Gap;
            for (int key = secondStart; key < secondStart + SecondCount; key++) {
                if (key % 2 == 0) {  // if even key
                    string putData = $"Test Data {key}";
                    TestData[key] = new[] { putData };
                }
                else {
                    var putData = new string[key];
                    char dupLetter = 'a';
                    for (int indx = 0; indx < key; indx++) {
                        putData[indx] = $"Test Data {key}{dupLetter}";
                        dupLetter++;
                    }
                    TestData[key] = putData;
                }
            }

            var buffer = new byte[1024];
            using (var tx = Env.BeginTransaction(TransactionModes.None)) {
                foreach (var testEntry in TestData) {
                    var key = testEntry.Key;
                    var putData = TestData[key];
                    var keyBytes = BitConverter.GetBytes(key);
                    for (int indx = 0; indx < putData.Count; indx++) {
                        var putDataItem = putData[indx];
                        int byteCount = Encoding.UTF8.GetBytes(putDataItem, 0, putDataItem.Length, buffer, 0);
                        Db.Put(tx, keyBytes, new ReadOnlySpan<byte>(buffer, 0, byteCount), PutOptions.None);
                    }
                }
                tx.Commit();
            }
        }

        public Dictionary<int, IList<string>> TestData { get; }

        public MultiValueDatabase Db { get; }

        public override void Dispose() {
            using (var tx = Env.BeginTransaction(TransactionModes.None)) {
                Db.Drop(tx);
                tx.Commit();
            }
            base.Dispose();
        }
    }

    [CollectionDefinition("Database")]
    public class DatabaseCollection: ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply to be
        // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
    }
}
