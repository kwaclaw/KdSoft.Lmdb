using System;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    public class EnvironmentTests
    {
        readonly ITestOutputHelper output;
        const string envPath = @"F:\Work\Private\KdSoft.Lmdb\Tests\TestEnv";

        public EnvironmentTests(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void OpenEnvironment() {
            using (var env = new Environment()) {
                env.Open(envPath);
                var envInfo = env.GetInfo();
                output.WriteLine($"MapAddr: {envInfo.MapAddr}");
                output.WriteLine($"MapSize: {envInfo.MapSize}");
                output.WriteLine($"MaxReaders: {envInfo.MaxReaders}");
                output.WriteLine($"NumReaders: {envInfo.NumReaders}");
                output.WriteLine($"LastPgNo: {envInfo.LastPgNo}");
                output.WriteLine($"LastTxnId: {envInfo.LastTxnId}");
            }
        }

        [Fact]
        public void AbortTransaction() {
            using (var env = new Environment()) {
                env.Open(envPath);
                using (var tx = env.BeginTransaction(TransactionModes.None)) {
                    //
                }
            }

        }

        [Fact]
        public void CommitTransaction() {
            using (var env = new Environment()) {
                env.Open(envPath);
                using (var tx = env.BeginTransaction(TransactionModes.None)) {
                    tx.Commit();
                }
            }
        }
    }
}
