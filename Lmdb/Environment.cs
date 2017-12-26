using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class Environment: CriticalFinalizerObject, IDisposable
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

        #region Helpers

        [CLSCompliant(false)]
        protected void RunChecked(Func<IntPtr, DbRetCode> libFunc) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = libFunc(handle);
                Util.CheckRetCode(ret);
            }
        }

        [CLSCompliant(false)]
        protected delegate R LibFunc<T, out R>(IntPtr handle, out T result);

        [CLSCompliant(false)]
        protected T GetChecked<T>(LibFunc<T, DbRetCode> libFunc) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = libFunc(handle, out var result);
                Util.CheckRetCode(ret);
                return result;
            }
        }

        #endregion

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
            RunChecked((handle) => Lib.mdb_env_set_mapsize(handle, (IntPtr)newValue));
        }

        uint maxDatabases;
        /// <summary>
        /// Set/get the maximum number of named databases for the environment.
        /// This function is only needed if multiple databases will be used in the environment.
        /// Simpler applications that use the environment as a single unnamed database can ignore this option.
        /// This function may only be called after mdb_env_create() and before mdb_env_open().
        /// Currently a moderate number of slots are cheap but a huge number gets expensive: 7-120 words per transaction,
        /// and every mdb_dbi_open() does a linear search of the opened slots.
        /// </summary>
        public int MaxDatabases {
            get {
                CheckDisposed();
                return unchecked((int)maxDatabases);
            }
            set {
                RunChecked((handle) => Lib.mdb_env_set_maxdbs(handle, unchecked((uint)value)));
                maxDatabases = unchecked((uint)value);
            }
        }

        /// <summary>
        /// This defines the number of slots in the lock table that is used to track readers in the the environment.
        /// The default is 126. Starting a read-only transaction normally ties a lock table slot to the current thread
        /// until the environment closes or the thread exits. If MDB_NOTLS is in use, mdb_txn_begin() instead
        /// ties the slot to the MDB_txn object until it or the MDB_env object is destroyed.
        /// This function may only be called after mdb_env_create() and before mdb_env_open().
        /// </summary>
        public int MaxReaders {
            get => unchecked((int)GetChecked((IntPtr handle, out uint value) => Lib.mdb_env_get_maxreaders(handle, out value)));
            set => RunChecked((handle) => Lib.mdb_env_set_maxreaders(handle, unchecked((uint)value)));
        }

        /// <summary>
        /// Set environment flags.
        /// This may be used to set some flags in addition to those from mdb_env_open(), or to unset these flags.
        /// If several threads change the flags at the same time, the result is undefined.
        /// </summary>
        /// <param name="options">Option flags to set, bitwise OR'ed together.</param>
        /// <param name="onoff">A <c>true</c> value sets the flags, <c>false</c> clears them.</param>
        public void SetOptions(EnvironmentOptions options, bool onoff) {
            RunChecked((handle) => Lib.mdb_env_set_flags(handle, options, onoff));
        }

        /// <summary>
        /// Return information about the LMDB environment.
        /// </summary>
        public EnvironmentOptions GetOptions() {
            return GetChecked((IntPtr handle, out EnvironmentOptions value) => Lib.mdb_env_get_flags(handle, out value));
        }

        /// <summary>
        /// Return information about the LMDB environment.
        /// </summary>
        public EnvironmentInfo GetInfo() {
            return GetChecked((IntPtr handle, out EnvironmentInfo value) => Lib.mdb_env_info(handle, out value));
        }

        /// <summary>
        /// Return statistics about the LMDB environment.
        /// </summary>
        /// <returns></returns>
        public Statistics GetStats() {
            return GetChecked((IntPtr handle, out Statistics value) => Lib.mdb_env_stat(handle, out value));
        }

        /// <summary>
        /// Get the maximum size of keys and MDB_DUPSORT data we can write.
        /// Depends on the compile-time constant MDB_MAXKEYSIZE.Default 511. See MDB_val.
        /// </summary>
        public int GetMaxKeySize() {
            lock (rscLock) {
                var handle = CheckDisposed();
                return Lib.mdb_env_get_maxkeysize(handle);
            }
        }

        /// <summary>
        /// Return the path that was used in mdb_env_open().
        /// </summary>
        public string GetPath() {
            return GetChecked((IntPtr handle, out string value) => Lib.mdb_env_get_path(handle, out value));
        }

        #endregion

        /// <summary>
        /// Open an environment handle.
        /// If this function fails, mdb_env_close() must be called to discard the MDB_env handle.
        /// </summary>
        /// <param name="path">The directory in which the database files reside. This directory must already exist and be writable.</param>
        /// <param name="options">Special options for this environment. See <see cref="EnvironmentOptions"/>.
        /// This parameter must be set to 0 or by bitwise OR'ing together one or more of the values.
        /// Flags set by mdb_env_set_flags() are also used.</param>
        /// <param name="fileMode">The UNIX permissions to set on created files and semaphores. This parameter is ignored on Windows.</param>
        public void Open(string path, EnvironmentOptions options = EnvironmentOptions.None, UnixFileMode fileMode = UnixFileMode.Default) {
            RunChecked((handle) => Lib.mdb_env_open(handle, path, options, fileMode));
        }

        /// <summary>
        /// Close the environment and release the memory map. Same as Dispose().
        /// Only a single thread may call this function. All transactions, databases, and cursors must already be closed
        /// before calling this function. Attempts to use any such handles after calling this function will cause a SIGSEGV.
        /// The environment handle will be freed and must not be used again after this call.
        /// </summary>
        public void Close() {
            Dispose();
        }

        /// <summary>
        /// Flush the data buffers to disk. Data is always written to disk when mdb_txn_commit() is called,
        /// but the operating system may keep it buffered.LMDB always flushes the OS buffers upon commit as well,
        /// unless the environment was opened with MDB_NOSYNC or in part MDB_NOMETASYNC.
        /// This call is not valid if the environment was opened with MDB_RDONLY.
        /// </summary>
        /// <param name="force">
        /// If <c>true</c>, force a synchronous flush. Otherwise if the environment has the MDB_NOSYNC flag set
        /// the flushes will be omitted, and with MDB_MAPASYNC they will be asynchronous.
        /// </param>
        public void Sync(bool force) {
            RunChecked((handle) => Lib.mdb_env_sync(handle, force));
        }

        #region Databases and Transactions

        OpenDbTransaction activeDbTxn;
        readonly object dbTxnLock = new object();
        readonly ConcurrentDictionary<IntPtr, Transaction> transactions = new ConcurrentDictionary<IntPtr, Transaction>();
        readonly Dictionary<string, Database> databases = new Dictionary<string, Database>(StringComparer.OrdinalIgnoreCase);

        public Database this[string name] {
            get {
                lock (dbTxnLock) {
                    return databases[name];
                }
            }
        }

        public IEnumerable<Database> GetDatabases() {
            lock (dbTxnLock) {
                return databases.Values;
            }
        }

        void TransactionDisposed(IntPtr txnId) {
            transactions.TryRemove(txnId, out Transaction value);
        }

        void DatabaseTransactionClosed(IntPtr txnId) {
            lock (dbTxnLock) {
                activeDbTxn = null;
            }
        }

        void DatabaseDisposed(Database db) {
            lock (dbTxnLock) {
                databases.Remove(db.Name);
            }
        }

        (IntPtr txn, IntPtr txnId) BeginTransactionInternal(TransactionModes modes, Transaction parent) {
            var parentTxn = parent?.Handle ?? IntPtr.Zero;
            var txn = GetChecked((IntPtr handle, out IntPtr value) => Lib.mdb_txn_begin(handle, parentTxn, modes, out value));
            var txnId = Lib.mdb_txn_id(txn);
            return (txn, txnId);
        }

        /// <summary>
        /// Create a transaction for use with the environment.
        /// The transaction handle may be discarded using mdb_txn_abort() or mdb_txn_commit().
        /// Note: A transaction and its cursors must only be used by a single thread, and a thread may only have a single transaction at a time.
        /// If MDB_NOTLS is in use, this does not apply to read-only transactions.
        /// Cursors may not span transactions.
        /// </summary>
        /// <param name="modes">Special options for this transaction. This parameter must be set to 0 or by bitwise OR'ing together.</param>
        /// <param name="parent">
        /// If this parameter is non-NULL, the new transaction will be a nested transaction, with the transaction
        /// indicated by parent as its parent. Transactions may be nested to any level.
        /// A parent transaction and its cursors may not issue any other operations than
        /// mdb_txn_commit and mdb_txn_abort while it has active child transactions.
        /// </param>
        /// <returns>New transaction instance.</returns>
        public Transaction BeginTransaction(TransactionModes modes, Transaction parent = null) {
            var (txn, txnId) = BeginTransactionInternal(modes, parent);
            Transaction result;
            if ((modes & TransactionModes.ReadOnly) == 0)
                result = new Transaction(txn, parent, TransactionDisposed);
            else
                result = new ReadOnlyTransaction(txn, parent, TransactionDisposed);
            if (!transactions.TryAdd(txnId, result)) {
                Lib.mdb_txn_abort(txn);
                throw new LmdbException($"Transaction with same Id {txnId} exists already.");
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="ReadOnlyTransaction"/>. The <see cref="TransactionModes.ReadOnly"/> flag will be set automatically.
        /// For details, see <see cref="BeginTransaction(TransactionModes, Transaction)"/>.
        /// </summary>
        public ReadOnlyTransaction BeginReadOnlyTransaction(TransactionModes modes, Transaction parent = null) {
            modes = modes | TransactionModes.ReadOnly;
            return (ReadOnlyTransaction)BeginTransaction(modes, parent);
        }

        /// <summary>
        /// Create a transaction that allows opening databases in the environment. <see cref="BeginTransaction"/>.
        /// A newly opened database handle will be private to the transaction until the transaction is successfully committed.
        /// If the transaction is aborted the handle will be closed automatically.
        /// After a successful commit the handle will reside in the shared environment, and may be used by other transactions.
        /// The OpenDatabase function must not be called from multiple concurrent transactions in the same process.
        /// A transaction that uses that function must finish (either commit or abort) before any other transaction in the process
        /// may use the OpenDatabase function, therefore only one such transaction is allowed to be active in the environment at a time.
        /// </summary>
        /// <param name="modes">Special options for this transaction..</param>
        /// <param name="parent">If this parameter is non-NULL, the new transaction will be a nested transaction.</param>
        /// <returns>New transaction instance.</returns>
        public OpenDbTransaction BeginOpenDbTransaction(TransactionModes modes, Transaction parent = null) {
            if ((modes & TransactionModes.ReadOnly) != 0)
                throw new LmdbException("An OpenDbTransaction must not be read-only");
            lock (dbTxnLock) {
                if (activeDbTxn != null)
                    throw new LmdbException("Only one OpenDbTransaction can be active at a time.");
                var (txn, txnId) = BeginTransactionInternal(modes, parent);
                return new OpenDbTransaction(txn, parent, DatabaseTransactionClosed, databases, DatabaseDisposed);
            }
        }

        #endregion

        #region Unmanaged Resources

        protected internal readonly object rscLock = new object();

        // access to properly aligned types of size "native int" is atomic!
        volatile IntPtr env;
        readonly GCHandle instanceHandle;

        internal IntPtr Handle => env;

        void ReleaseUnmanagedResources() {
            IntPtr handle = this.env;
            // LmdbApi.mdb_env_close() could be a lengthy call, so we call SetDisposed() first, and the
            // CER ensures that we reach LmdbApi.mdb_env_close() without external interruption.
            // This is OK because one must not use the handle after LmdbApi.mdb_env_close() was called
            this.env = IntPtr.Zero;  //  SetDisposed();
            Lib.mdb_env_close(handle);
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
                        foreach (var txEntry in transactions) {
                            txEntry.Value.Dispose();
                        }
                        lock (dbTxnLock) {
                            activeDbTxn?.Dispose();
                            // It is very rarely necessary to close a database handle, and in general they should just be left open.
                            // Therefore we just set the database handle to 0 when the environment is Disposed(), so that
                            // using the Database instance will raise the appropriate exception
                            foreach (var dbEntry in databases) {
                                dbEntry.Value.ClearHandle();
                            }
                        }
                    }
                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    ReleaseUnmanagedResources();
                }

                if (instanceHandle.IsAllocated)
                    instanceHandle.Free();

                // set large fields to null.
                transactions.Clear();
                databases.Clear();
            }
        }

        ~Environment() {
            Dispose(false);
        }

        /// <summary>
        /// Close the environment and release the memory map. Same as Close().
        /// Only a single thread may call this function. All transactions, databases, and cursors must already be closed
        /// before calling this function. Attempts to use any such handles after calling this function will cause a SIGSEGV.
        /// The environment handle will be freed and must not be used again after this call.
        /// </summary>
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
            public int? MaxReaders { get; }
            public int? MaxDatabases { get; }
            public bool AutoReduceMapSizeIn32BitProcess { get; }

            public Configuration(int? maxDatabases = null, int? maxReaders = null, long? mapSize = null, bool autoReduceMapSizeIn32BitProcess = false) {
                this.MaxDatabases = maxDatabases;
                this.MaxReaders = maxReaders;
                this.MapSize = mapSize;
                this.AutoReduceMapSizeIn32BitProcess = autoReduceMapSizeIn32BitProcess;
            }
        }

        #endregion
    }
}
