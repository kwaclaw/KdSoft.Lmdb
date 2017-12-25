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
        readonly Environment env;
        readonly ITestOutputHelper output;

        public DatabaseTests(EnvironmentFixture envFx, ITestOutputHelper output) {
            this.env = envFx.Env;
            this.output = output;
        }

        [Fact]
        public void OpenDatabase() {
            var config = new Database.Configuration(DatabaseOptions.Create);
            Database dbase;
            using (var tx = env.BeginOpenDbTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb1", config);
                tx.Commit();
            }

            var dbs = env.GetDatabases();
            foreach (var db in dbs)
                output.WriteLine($"Database '{db.Name}'");

            using (var tx = env.BeginTransaction(TransactionModes.None)) {
                dbase.Drop(tx);
                tx.Commit();
            }

            Assert.Empty(env.GetDatabases());
        }
    }
}
