namespace KdSoft.Lmdb
{
    /// <summary>
    /// Basic environment configuration
    /// </summary>
    public class LmdbEnvironmentConfiguration
    {
        public long? MapSize { get; }
        public int? MaxReaders { get; }
        public int? MaxDatabases { get; }
        public bool AutoReduceMapSizeIn32BitProcess { get; }

        public LmdbEnvironmentConfiguration(int? maxDatabases = null, int? maxReaders = null, long? mapSize = null, bool autoReduceMapSizeIn32BitProcess = false) {
            this.MaxDatabases = maxDatabases;
            this.MaxReaders = maxReaders;
            this.MapSize = mapSize;
            this.AutoReduceMapSizeIn32BitProcess = autoReduceMapSizeIn32BitProcess;
        }
    }
}
