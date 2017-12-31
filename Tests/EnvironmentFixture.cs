using System;
using KdSoft.Utils;
using Xunit;

namespace KdSoft.Lmdb.Tests
{
    public class EnvironmentFixture: IDisposable
    {
        const string envPath = @"F:\Work\Private\KdSoft.Lmdb\Tests\TestEnv";

        public EnvironmentFixture() {
            var config = new Environment.Configuration(10);
            var env = new Environment(config);
            env.Open(envPath);
            this.Env = env;
            this.Buffers = new BufferPool();
        }

        public Environment Env { get; }

        public BufferPool Buffers { get; }

        public virtual void Dispose() {
            Env.Close();
        }
    }

    [CollectionDefinition("Environment")]
    public class EnvironmentCollection: ICollectionFixture<EnvironmentFixture>
    {
        // This class has no code, and is never created. Its purpose is simply to be
        // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
    }
}
