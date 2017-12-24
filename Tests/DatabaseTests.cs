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
            Database dbase;
            using (var tx = env.BeginOpenDbTransaction(TransactionModes.None)) {
                dbase = tx.OpenDatabase("TestDb1", DatabaseOptions.Create);
                tx.Commit();
            }

            var dbs = env.GetDatabases();
            foreach (var db in dbs)
                output.WriteLine($"Database '{db.Name}'");
        }
    }
}
