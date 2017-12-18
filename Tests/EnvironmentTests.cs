using System;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    public class EnvironmentTests
    {
        ITestOutputHelper output;

        public EnvironmentTests(ITestOutputHelper output) {
            this.output = output;
        }

        [Fact]
        public void OpenEnvironment() {
            using (var env = new Environment()) {
                env.Open(@"F:\Work\Private\KdSoft.Lmdb\Tests\TestEnv");
                var envInfo = env.GetInfo();
                output.WriteLine($"MapAddr: {envInfo.MapAddr}");
                output.WriteLine($"MapSize: {envInfo.MapSize}");
                output.WriteLine($"MaxReaders: {envInfo.MaxReaders}");
                output.WriteLine($"NumReaders: {envInfo.NumReaders}");
                output.WriteLine($"LastPgNo: {envInfo.LastPgNo}");
                output.WriteLine($"LastTxnId: {envInfo.LastTxnId}");
            }
        }
    }
}
