﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    //TODO Implement .NET 5.0 native interop improvements
    //     see https://devblogs.microsoft.com/dotnet/improvements-in-native-code-interop-in-net-5-0/

    public delegate void AssertFunction(LmdbEnvironment env, string msg);

    /// <summary>LMDB environment.</summary>
    /// <remarks>
    /// We make Environment the only <see cref="CriticalFinalizerObject"/> because it can
    /// clean up all resources it owns (databases, transactions, cursors) in its finalizer.
    /// </remarks>
    public class LmdbEnvironment: CriticalFinalizerObject, IDisposable
    {
        readonly bool autoReduceMapSizeIn32BitProcess;

        /// <summary>Constructor.</summary>
        /// <param name="config">Configuration to use.</param>
        public LmdbEnvironment(LmdbEnvironmentConfiguration config = null) {
            // so that we can refer back to the Environment instance
            instanceHandle = GCHandle.Alloc(this, GCHandleType.WeakTrackResurrection);

            DbRetCode ret = DbLib.mdb_env_create(out IntPtr envHandle);
            if (ret == DbRetCode.SUCCESS) {
                var ret2 = DbLib.mdb_env_set_userctx(envHandle, (IntPtr)instanceHandle);
                if (ret2 == DbRetCode.SUCCESS)
                    this.env = envHandle;
                else {
                    ret = ret2;
                    DbLib.mdb_env_close(envHandle);
                }
            }
            ErrorUtil.CheckRetCode(ret);

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

        /// <summary>
        /// Helper method to run a native library function with disposed checking and exception processing.
        /// </summary>
        /// <param name="libFunc">Native function delegate that does not return a result.</param>
        [CLSCompliant(false)]
        protected void RunChecked(Func<IntPtr, DbRetCode> libFunc) {
            var handle = CheckDisposed();
            var ret = libFunc(handle);
            ErrorUtil.CheckRetCode(ret);
        }

        /// <summary>
        /// Helper delegate for calling library functions with a common signature.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <typeparam name="R">Return code type.</typeparam>
        /// <param name="handle">Native handle to pass to the library function.</param>
        /// <param name="result">Result returned from the library function call.</param>
        /// <returns>Code returned by the native liv=brary function call.</returns>
        [CLSCompliant(false)]
        protected delegate R LibFunc<T, out R>(IntPtr handle, out T result);

        /// <summary>
        /// Helper method to run a native library function with disposed checking and exception processing.
        /// </summary>
        /// <typeparam name="T">Result type returned by native function.</typeparam>
        /// <param name="libFunc">Native function delegate that resturns a result.</param>
        /// <returns>Result returned from the native library function call.</returns>
        [CLSCompliant(false)]
        protected T GetChecked<T>(LibFunc<T, DbRetCode> libFunc) {
            var handle = CheckDisposed();
            var ret = libFunc(handle, out T result);
            ErrorUtil.CheckRetCode(ret);
            return result;
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
            RunChecked((handle) => DbLib.mdb_env_set_mapsize(handle, (IntPtr)newValue));
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
                RunChecked((handle) => DbLib.mdb_env_set_maxdbs(handle, unchecked((uint)value)));
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
            get => unchecked((int)GetChecked((IntPtr handle, out uint value) => DbLib.mdb_env_get_maxreaders(handle, out value)));
            set => RunChecked((handle) => DbLib.mdb_env_set_maxreaders(handle, unchecked((uint)value)));
        }

        /// <summary>
        /// Set environment flags.
        /// This may be used to set some flags in addition to those from mdb_env_open(), or to unset these flags.
        /// If several threads change the flags at the same time, the result is undefined.
        /// </summary>
        /// <param name="options">Option flags to set, bitwise OR'ed together.</param>
        /// <param name="onoff">A <c>true</c> value sets the flags, <c>false</c> clears them.</param>
        public void SetOptions(LmdbEnvironmentOptions options, bool onoff) {
            RunChecked((handle) => DbLib.mdb_env_set_flags(handle, options, onoff));
        }

        /// <summary>
        /// Return information about the LMDB environment.
        /// </summary>
        public LmdbEnvironmentOptions GetOptions() {
            return GetChecked((IntPtr handle, out LmdbEnvironmentOptions value) => DbLib.mdb_env_get_flags(handle, out value));
        }

        /// <summary>
        /// Return information about the LMDB environment.
        /// </summary>
        public LmdbEnvironmentInfo GetInfo() {
            return GetChecked((IntPtr handle, out LmdbEnvironmentInfo value) => DbLib.mdb_env_info(handle, out value));
        }

        /// <summary>
        /// Return statistics about the LMDB environment.
        /// </summary>
        /// <returns></returns>
        public Statistics GetStats() {
            return GetChecked((IntPtr handle, out Statistics value) => DbLib.mdb_env_stat(handle, out value));
        }

        /// <summary>
        /// Get the maximum size of keys and MDB_DUPSORT data we can write.
        /// Depends on the compile-time constant MDB_MAXKEYSIZE.Default 511. See MDB_val.
        /// </summary>
        public int GetMaxKeySize() {
            var handle = CheckDisposed();
            return DbLib.mdb_env_get_maxkeysize(handle);
        }

        /// <summary>
        /// Return the path that was used in mdb_env_open().
        /// </summary>
        public string GetPath() {
            return GetChecked((IntPtr handle, out string value) => DbLib.mdb_env_get_path(handle, out value));
        }

        #endregion

        /// <summary>
        /// Open an environment handle. Do not open multiple times in the same process.
        /// If this function fails, mdb_env_close() must be called to discard the MDB_env handle.
        /// </summary>
        /// <param name="path">The directory in which the database files reside. This directory must already exist and be writable.</param>
        /// <param name="options">Special options for this environment. See <see cref="LmdbEnvironmentOptions"/>.
        /// This parameter must be set to 0 or by bitwise OR'ing together one or more of the values.
        /// Flags set by mdb_env_set_flags() are also used.</param>
        /// <param name="fileMode">The UNIX permissions to set on created files and semaphores. This parameter is ignored on Windows.</param>
        public void Open(string path, LmdbEnvironmentOptions options = LmdbEnvironmentOptions.None, UnixFileModes fileMode = UnixFileModes.Default) {
            RunChecked((handle) => DbLib.mdb_env_open(handle, path, options, fileMode));
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
            RunChecked((handle) => DbLib.mdb_env_sync(handle, force));
        }

        //TODO expose mdb_reader_list() and mdb_reader_check()

        #region Databases and Transactions

        DatabaseTransaction activeDbTxn;
        readonly object dbTxnLock = new object();
        readonly ConcurrentDictionary<IntPtr, Transaction> transactions = new ConcurrentDictionary<IntPtr, Transaction>();
        readonly Dictionary<string, Database> databases = new Dictionary<string, Database>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns database by name.</summary>
        public Database this[string name] {
            get {
                lock (dbTxnLock) {
                    return databases[name];
                }
            }
        }

        /// <summary>Enumerates databases in the environment.</summary>
        public IEnumerable<Database> Databases {
            get {
                lock (dbTxnLock) {
                    return databases.Values;
                }
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
            var txn = GetChecked((IntPtr handle, out IntPtr value) => DbLib.mdb_txn_begin(handle, parentTxn, modes, out value));
            var txnId = DbLib.mdb_txn_id(txn);
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
        public Transaction BeginTransaction(TransactionModes modes = TransactionModes.None, Transaction parent = null) {
            var (txn, txnId) = BeginTransactionInternal(modes, parent);
            Transaction result;

            bool checkConcurrent = true;
            if ((modes & TransactionModes.ReadOnly) == 0) {
                result = new Transaction(txn, parent, TransactionDisposed);
            }
            else {
                result = new ReadOnlyTransaction(txn, parent, TransactionDisposed);
                var opts = GetOptions();
                if (opts.HasFlag(LmdbEnvironmentOptions.NoThreadLocalStorage))
                    checkConcurrent = false;
            }

            if (!transactions.TryAdd(txnId, result) & checkConcurrent) {  // no smart boolean evaluation!
                DbLib.mdb_txn_abort(txn);
                throw new LmdbException($"Transaction with same Id {txnId} exists already.");
            }
            return result;
        }

        /// <summary>
        /// Creates a <see cref="ReadOnlyTransaction"/>. The <see cref="TransactionModes.ReadOnly"/> flag will be set automatically.
        /// For details, see <see cref="BeginTransaction(TransactionModes, Transaction)"/>.
        /// </summary>
        public ReadOnlyTransaction BeginReadOnlyTransaction(TransactionModes modes = TransactionModes.None, Transaction parent = null) {
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
        public DatabaseTransaction BeginDatabaseTransaction(TransactionModes modes, Transaction parent = null) {
            if ((modes & TransactionModes.ReadOnly) != 0)
                throw new LmdbException("An OpenDbTransaction must not be read-only");
            lock (dbTxnLock) {
                if (activeDbTxn != null)
                    throw new LmdbException("Only one OpenDbTransaction can be active at a time.");
                var (txn, txnId) = BeginTransactionInternal(modes, parent);
                return new DatabaseTransaction(txn, parent, DatabaseTransactionClosed, databases, DatabaseDisposed);
            }
        }

        #endregion

        #region Unmanaged Resources

        // access to properly aligned types of size "native int" is atomic!
        IntPtr env;
        readonly GCHandle instanceHandle;

        internal IntPtr Handle => env;

        #endregion

        #region IDisposable Support

        void ThrowDisposed() {
            throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Returns Environment handle.
        /// Throws if Environment handle is already closed/disposed of.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            Interlocked.MemoryBarrier();
            IntPtr result = this.env;
            Interlocked.MemoryBarrier();
            if (result == IntPtr.Zero)
                ThrowDisposed();
            return result;
        }

        /// <summary>
        /// Returns if Environment handle is closed/disposed.
        /// </summary>
        public bool IsDisposed {
            get {
                Interlocked.MemoryBarrier();
                bool result = env == IntPtr.Zero;
                return result;
            }
        }

        /// <summary>
        /// Implementation of Dispose() pattern. See <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> if explicity disposing (finalizer not run), <c>false</c> if disposed from finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            var handle = Interlocked.Exchange(ref env, IntPtr.Zero);
            // if the env handle was valid before we cleared it, lets close the handle
            if (handle != IntPtr.Zero) {
                if (disposing) {
                    // dispose managed state (managed objects).
                    foreach (var txEntry in transactions) {
                        txEntry.Value.Dispose(); // this will also close the cursors owened by the transaction
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
                // free unmanaged resources
                DbLib.mdb_env_close(handle);
            }

            if (instanceHandle.IsAllocated)
                instanceHandle.Free();

            // set large fields to null.
            transactions.Clear();
            lock (dbTxnLock) {
                activeDbTxn = null;
                databases.Clear();
            }
        }

        /// <summary>
        /// Finalizer. Releases unmanaged resources.
        /// </summary>
        ~LmdbEnvironment() {
            Dispose(false);
        }

        /// <summary>
        /// Close the environment and release the memory map. Same as Close().
        /// Only a single thread may call this function.
        /// The environment handle will be freed and must not be used again after this call.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
