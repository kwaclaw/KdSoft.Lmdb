using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public delegate int CompareFunction(in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y);

    public class Database: IDisposable
    {
        public string Name { get; }
        public Configuration Config { get; }

        internal Database(uint dbi, IntPtr env, string name, Action<Database> disposed, Configuration config) {
            this.dbi = dbi;
            this.env = env;
            this.Name = name;
            this.disposed = disposed;
            this.Config = config;
        }

        Action<Database> disposed;
        internal Action<Database> Disposed {
            get => disposed;
            set => disposed = value;
        }

        public Environment Environment {
            get {
                lock (rscLock) {
                    var gcHandle = (GCHandle)Lib.mdb_env_get_userctx(env);
                    return (Environment)gcHandle.Target;
                }
            }
        }

        #region Helpers

        [CLSCompliant(false)]
        protected void RunChecked(Func<uint, DbRetCode> libFunc) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = libFunc(handle);
            }
            Util.CheckRetCode(ret);
        }

        [CLSCompliant(false)]
        protected delegate R LibFunc<T, out R>(uint handle, out T result);

        [CLSCompliant(false)]
        protected T GetChecked<T>(LibFunc<T, DbRetCode> libFunc) {
            DbRetCode ret;
            T result;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = libFunc(handle, out result);
            }
            Util.CheckRetCode(ret);
            return result;
        }

        #endregion

        /// <summary>
        /// Same as <see cref="Dispose()"/>. Close a database handle. Normally unnecessary. Use with care:
        /// This call is not mutex protected. Handles should only be closed by a single thread, and only
        /// if no other threads are going to reference the database handle or one of its cursors any further.
        /// Do not close a handle if an existing transaction has modified its database.
        /// Doing so can cause misbehavior from database corruption to errors like MDB_BAD_VALSIZE(since the DB name is gone).
        /// Closing a database handle is not necessary, but lets mdb_dbi_open() reuse the handle value.
        /// Usually it's better to set a bigger mdb_env_set_maxdbs(), unless that value would be large.
        /// </summary>
        public void Close() {
            Dispose();
        }

        /// <summary>
        /// Empty a database.
        /// </summary>
        public void Truncate(Transaction transaction) {
            RunChecked((handle) => Lib.mdb_drop(transaction.Handle, handle, false));
        }

        /// <summary>
        /// Delete and close a database. See <see cref="Close()"/> for restrictions.
        /// </summary>
        public void Drop(Transaction transaction) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = Lib.mdb_drop(transaction.Handle, handle, true);
                Util.CheckRetCode(ret);
                SetDisposed();
            }
        }

        /// <summary>
        /// Retrieve statistics for the database.
        /// </summary>
        public Statistics GetStats(Transaction transaction) {
            return GetChecked((uint handle, out Statistics value) => Lib.mdb_stat(transaction.Handle, handle, out value));
        }

        /// <summary>
        /// Retrieve the options for the database.
        /// </summary>
        public int GetAllOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => Lib.mdb_dbi_flags(transaction.Handle, handle, out value));
            return unchecked((int)opts);
        }

        /// <summary>
        /// Retrieve the options for the database.
        /// </summary>
        public DatabaseOptions GetOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => Lib.mdb_dbi_flags(transaction.Handle, handle, out value));
            return unchecked((DatabaseOptions)opts);
        }

        /// <summary>
        /// Get items from a database.
        /// This function retrieves key/data pairs from the database. The address and length of the data associated with
        /// the specified key are returned in the structure to which data refers.
        /// If the database supports duplicate keys (MDB_DUPSORT) then the first data item for the key will be returned.
        /// Retrieval of other items requires the use of mdb_cursor_get().
        /// Note: The memory pointed to by the returned values is owned by the database. The caller need not dispose of the memory,
        /// and may not modify it in any way. For values returned in a read-only transaction any modification attempts will cause a SIGSEGV.
        /// Values returned from the database are valid only until a subsequent update operation, or the end of the transaction.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        /// <returns><c>true</c> if data for key retrieved without error, <c>false</c> if key does not exist.</returns>
        public bool Get(Transaction transaction, ReadOnlySpan<byte> key, out ReadOnlySpan<byte> data) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* bytePtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(bytePtr, key.Length);
                        var dbData = default(DbValue);
                        ret = Lib.mdb_get(transaction.Handle, handle, ref dbKey, ref dbData);
                        data = dbData.ToReadOnlySpan();
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        [CLSCompliant(false)]
        protected bool PutInternal(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, uint options) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(data)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        var dbValue = new DbValue(dataPtr, data.Length);
                        ret = Lib.mdb_put(transaction.Handle, handle, ref dbKey, ref dbValue, options);
                    }
                }
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Store items into a database.
        /// This function stores key/data pairs in the database. The default behavior is to enter
        /// the new key/data pair, replacing any previously existing key.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="PutOptions.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool Put(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, PutOptions options) {
            return PutInternal(transaction, key, data, unchecked((uint)options));
        }

        /// <summary>
        /// Delete items from a database. This function removes key/data pairs from the database.
        /// If this instance is a <see cref="MultiValueDatabase"/> then all of the duplicate data items for the key will be deleted.
        /// This function will return MDB_NOTFOUND if the specified key/data pair is not in the database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        public bool Delete(Transaction transaction, ReadOnlySpan<byte> key) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* bytePtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(bytePtr, key.Length);
                        ret = Lib.mdb_del(transaction.Handle, handle, ref dbKey, IntPtr.Zero);
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        //TODO expose mdb_cmp() and mdb_dcmp()

        #region Unmanaged Resources

        protected readonly object rscLock = new object();

        readonly IntPtr env;

        volatile uint dbi;
        internal uint Handle => dbi;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            Lib.mdb_dbi_close(env, dbi);
        }

        #endregion

        #region IDisposable Support

        public const string disposedStr = "Database handle closed.";

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint CheckDisposed() {
            // avoid multiple volatile memory access
            uint result = this.dbi;
            if (result == 0)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        internal void ClearHandle() {
            lock (rscLock) {
                dbi = 0;
            }
        }

        void SetDisposed() {
            dbi = 0;
            disposed?.Invoke(this);
        }

        public bool IsDisposed {
            get { return dbi == 0; }
        }

        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (dbi == 0)  // already disposed
                    return;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { /* */ }
                finally {
                    if (disposing) {
                        // dispose managed state (managed objects).
                    }
                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    ReleaseUnmanagedResources();
                    if (disposing)
                        SetDisposed();
                }
            }
        }

        ~Database() {
            Dispose(false);
        }

        /// <summary>
        /// Same as <see cref="Close"/>. Close a database handle. Normally unnecessary. Use with care:
        /// This call is not mutex protected. Handles should only be closed by a single thread, and only
        /// if no other threads are going to reference the database handle or one of its cursors any further.
        /// Do not close a handle if an existing transaction has modified its database.
        /// Doing so can cause misbehavior from database corruption to errors like MDB_BAD_VALSIZE(since the DB name is gone).
        /// Closing a database handle is not necessary, but lets mdb_dbi_open() reuse the handle value.
        /// Usually it's better to set a bigger mdb_env_set_maxdbs(), unless that value would be large.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Database configuration
        /// </summary>
        public class Configuration
        {
            public DatabaseOptions Options { get; }
            public CompareFunction Compare { get; }
            internal Lib.CompareFunction LibCompare { get; }

            public Configuration(DatabaseOptions options, CompareFunction compare = null) {
                this.Options = options;
                this.Compare = compare;
                if (compare != null)
                    this.LibCompare = CompareWrapper;
            }

            // no check for Compare == null
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int CompareWrapper(in DbValue x, in DbValue y) {
                return Compare(x.ToReadOnlySpan(), y.ToReadOnlySpan());
            }
        }

        public class EqualityComparer : IEqualityComparer<Database>, IComparer<Database>
        {
            readonly StringComparer comparer;

            public EqualityComparer(StringComparer comparer) {
                this.comparer = comparer;
            }

            public int Compare(Database x, Database y) {
                return comparer.Compare(x.Name, y.Name);
            }

            public bool Equals(Database x, Database y) {
                return comparer.Equals(x.Name, y.Name);
            }

            public int GetHashCode(Database obj) {
                return obj?.Name?.GetHashCode() ?? 0;
            }
        }

        #endregion
    }
}
