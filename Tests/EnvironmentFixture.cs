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
            Directory.CreateDirectory(envPath);

            var config = new EnvironmentConfiguration(10, 10, 1000000000);
            var env = new Environment(config);
            env.Open(envPath, EnvironmentOptions.NoThreadLocalStorage);

            this.Env = env;
            this.Buffers = new BufferPool();
        }

        public Environment Env { get; }

        public BufferPool Buffers { get; }

        protected virtual void Dispose(bool disposing) {
            if (disposing)
                Env.Close();
        }
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    [CollectionDefinition("Environment")]
    public class EnvironmentGroup: ICollectionFixture<EnvironmentFixture>
    {
        // This class has no code, and is never created. Its purpose is simply to be
        // the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces. 
    }
}
