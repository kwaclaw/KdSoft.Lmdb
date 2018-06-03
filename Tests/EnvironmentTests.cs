using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    public class EnvironmentTests
    {
        readonly ITestOutputHelper output;
        const string envDirName = @"TestEnv";
        readonly string envPath;

        public EnvironmentTests(ITestOutputHelper output) {
            this.output = output;
            envPath = Path.Combine(TestUtils.ProjectDir, envDirName);
            Directory.CreateDirectory(envPath);
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
