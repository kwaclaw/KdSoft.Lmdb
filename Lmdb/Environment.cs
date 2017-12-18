using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace KdSoft.Lmdb
{
    public class Environment : CriticalFinalizerObject, IDisposable
    {
        //TODO keep track of databases: databases should only ever be opened once, so on additional calls we return the same handle

        //TODO we do not have to close transactions and cursors, but we must invalidate their handles, so we still need to track them
        readonly bool autoReduceMapSizeIn32BitProcess;

        public Environment(Configuration config = null) {
            // so that we can refer back to the Environment instance
            instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);

            DbRetCode ret;
            RuntimeHelpers.PrepareConstrainedRegions();
            try { /* */ }
            finally {
                IntPtr envHandle;
                ret = Lib.mdb_env_create(out envHandle);
                if (ret == DbRetCode.SUCCESS) {
                    var ret2 = Lib.mdb_env_set_userctx(envHandle, (IntPtr)instanceHandle);
                    if (ret2 == DbRetCode.SUCCESS)
                        this.env = envHandle;
                    else {
                        ret = ret2;
                        Lib.mdb_env_close(envHandle);
                    }
                }
            }
            Util.CheckRetCode(ret);

            if (config != null) {
                this.autoReduceMapSizeIn32BitProcess = config.AutoReduceMapSizeIn32BitProcess;
                if (config.MapSize.HasValue)
                    this.SetMapSize(config.MapSize.Value);
                if (config.MaxDatabases.HasValue)
                    this.MaxDatabases = config.MaxDatabases.Value;
                if (config.MaxReaders.HasValue)
                    this.MaxReaders = config.MaxReaders.Value;
            }
        }

        void RunChecked(Func<DbRetCode> mdbFunc) {
            lock (rscLock) {
                CheckDisposed();
                var ret = mdbFunc();
                Util.CheckRetCode(ret);
            }
        }

        public delegate R MdbFunc<T, out R>(out T result);

        T GetChecked<T>(MdbFunc<T, DbRetCode> mdbFunc) {
            lock (rscLock) {
                CheckDisposed();
                var ret = mdbFunc(out var result);
                Util.CheckRetCode(ret);
                return result;
            }
        }

        public void Open(string path, MdbEnvOpenOptions options = MdbEnvOpenOptions.None, UnixFileMode fileMode = UnixFileMode.Default) {
            RunChecked(() => Lib.mdb_env_open(env, path, options, fileMode));
        }


        #region Configuration and Stats

        /// <summary>
        /// Set the size of the memory map to use for this environment.
        /// The size should be a multiple of the OS page size. The default is 10485760 bytes. The size of the memory map 
        /// is also the maximum size of the database. The value should be chosen as large as possible, to accommodate
        /// future growth of the database. This function should be called after mdb_env_create() and before mdb_env_open().
        /// It may be called at later times if no transactions are active in this process.
        /// Note that the library does not check for this condition, the caller must ensure it explicitly.
        /// </summary>
        /// <remarks>
        /// The new size takes effect immediately for the current process but will not be persisted to any others
        /// until a write transaction has been committed by the current process. Also, only mapsize increases are persisted
        /// into the environment.
        /// If the mapsize is increased by another process, and data has grown beyond the range of the current mapsize,
        /// mdb_txn_begin() will return MDB_MAP_RESIZED. This function may be called with a size of zero to adopt the new size.
        /// Any attempt to set a size smaller than the space already consumed by the environment will be silently changed
        /// to the current size of the used space.
        /// Note: the current MapSize can be obtained by calling GetEnvInfo().
        /// </remarks>
        /// <param name="newValue"></param>
        public void SetMapSize(long newValue) {
            if (autoReduceMapSizeIn32BitProcess && (IntPtr.Size == 4) && (newValue > int.MaxValue))
                newValue = int.MaxValue;
            RunChecked(() => Lib.mdb_env_set_mapsize(env, (IntPtr)newValue));
        }

        uint maxDatabases;
        /// <summary>
        /// Set the maximum number of named databases for the environment.
        /// This function is only needed if multiple databases will be used in the environment.
        /// Simpler applications that use the environment as a single unnamed database can ignore this option.
        /// This function may only be called after mdb_env_create() and before mdb_env_open().
        /// Currently a moderate number of slots are cheap but a huge number gets expensive: 7-120 words per transaction,
        /// and every mdb_dbi_open() does a linear search of the opened slots.
        /// </summary>
        /// <param name="newValue"></param>
        public uint MaxDatabases {
            get {
                CheckDisposed();
                return maxDatabases;
            }
            set {
                RunChecked(() => Lib.mdb_env_set_maxdbs(env, value));
                maxDatabases = value;
            }
        }

        /// <summary>
        /// This defines the number of slots in the lock table that is used to track readers in the the environment.
        /// The default is 126. Starting a read-only transaction normally ties a lock table slot to the current thread
        /// until the environment closes or the thread exits. If MDB_NOTLS is in use, mdb_txn_begin() instead
        /// ties the slot to the MDB_txn object until it or the MDB_env object is destroyed.
        /// This function may only be called after mdb_env_create() and before mdb_env_open().
        /// </summary>
        /// <param name="newValue"></param>
        public uint MaxReaders {
            get => GetChecked((out uint value) => Lib.mdb_env_get_maxreaders(env, out value));
            set => RunChecked(() => Lib.mdb_env_set_maxreaders(env, value));
        }

        /// <summary>
        /// Return information about the LMDB environment.
        /// </summary>
        public MdbEnvInfo GetInfo() {
            return GetChecked((out MdbEnvInfo value) => Lib.mdb_env_info(env, out value));
        }

        /// <summary>
        /// Get the maximum size of keys and MDB_DUPSORT data we can write.
        /// Depends on the compile-time constant MDB_MAXKEYSIZE.Default 511. See MDB_val.
        /// </summary>
        public int GetMaxKeySize() {
            lock (rscLock) {
                CheckDisposed();
                return Lib.mdb_env_get_maxkeysize(env);
            }
        }

        /// <summary>
        /// Return the path that was used in mdb_env_open().
        /// </summary>
        public string GetPath() {
            return GetChecked((out string value) => Lib.mdb_env_get_path(env, out value));
        }

        #endregion

        #region Unmanaged Resources

        protected internal readonly object rscLock = new object();

        // access to properly aligned types of size "native int" is atomic!
        internal volatile IntPtr env;
        readonly GCHandle instanceHandle;

        HashSet<Database> databases = new HashSet<Database>(new Database.EqualityComparer(StringComparer.OrdinalIgnoreCase));

        void ReleaseUnmanagedResources() {
            IntPtr resHandle = this.env;
            // LmdbApi.mdb_env_close() could be a lengthy call, so we call SetDisposed() first, and the
            // CER ensures that we reach LmdbApi.mdb_env_close() without external interruption.
            // This is OK because one must not use the handle after LmdbApi.mdb_env_close() was called
            this.env = IntPtr.Zero;  //  SetDisposed();
            Lib.mdb_env_close(resHandle);
        }

        #endregion

        #region IDisposable Support

        public const string disposedStr = "Environment handle closed.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            IntPtr result = this.env;
            if (result == IntPtr.Zero)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        void SetDisposed() {
            env = IntPtr.Zero;
        }

        public bool IsDisposed {
            get { return env == IntPtr.Zero; }
        }

        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (env == IntPtr.Zero)  // already disposed
                    return;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { /* */ }
                finally {
                    if (disposing) {
                        // dispose managed state (managed objects).
                        foreach (var db in databases)
                            db.Dispose();
                    }
                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    ReleaseUnmanagedResources();
                }

                if (instanceHandle.IsAllocated)
                    instanceHandle.Free();

                // set large fields to null.
                databases.Clear();
                databases = null;
            }
        }

        ~Environment() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Nested Types

        /// <summary>
        /// Basic environment configuration
        /// </summary>
        public class Configuration
        {
            public long? MapSize { get; }
            public uint? MaxReaders { get; }
            public uint? MaxDatabases { get; }
            public bool AutoReduceMapSizeIn32BitProcess { get; }

            public Configuration(uint? maxDatabases = null, uint? maxReaders = null, long? mapSize = null, bool autoReduceMapSizeIn32BitProcess = false) {
                this.MaxDatabases = maxDatabases;
                this.MaxReaders = maxReaders;
                this.MapSize = mapSize;
                this.AutoReduceMapSizeIn32BitProcess = autoReduceMapSizeIn32BitProcess;
            }
        }

        #endregion
    }
}
