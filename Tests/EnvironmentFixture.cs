using System;
using System.IO;
using KdSoft.Utils;
using Xunit;

namespace KdSoft.Lmdb.Tests
{
    public class EnvironmentFixture: IDisposable
    {
        const string envDirName = @"TestEnv";
        readonly string envPath;

        public EnvironmentFixture() {
            envPath = Path.Combine(TestUtils.ProjectDir, envDirName);

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
    public class EnvironmentGroup: ICollectionFixture<EnvironmentFixture>
    {
        // This class has no code, and is never created. Its purpose is simply to be
        // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
    }
}
