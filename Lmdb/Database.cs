using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>Delegate type for compare functions.</summary>
    /// <typeparam name="T">Type of <see cref="Span{T}"/> items.</typeparam>
    /// <param name="x">Left item to use for comparison.</param>
    /// <param name="y">Right item to use for comparison.</param>
    /// <returns><c>0</c> if items are equal, <c>&lt; 0</c> if left item is less, and <c>&gt; 0</c>
    /// if left item is greater then the right item.</returns>
    public delegate int SpanComparison<T>(in ReadOnlySpan<T> x, in ReadOnlySpan<T> y);

    /// <summary>
    /// LMDB Database that allows unique (by key) records only.
    /// </summary>
    public class Database: IDisposable
    {
        /// <summary>Database name.</summary>
        public string Name { get; }

        /// <summary>Database configuration.</summary>
        public DatabaseConfiguration Config { get; }

        internal Database(uint dbi, IntPtr env, string name, Action<Database> disposed, DatabaseConfiguration config) {
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

        /// <summary>Environment the database was created in.</summary>
        public LmdbEnvironment Environment {
            get {
                var gcHandle = (GCHandle)DbLib.mdb_env_get_userctx(env);
                return (LmdbEnvironment)gcHandle.Target;
            }
        }

        #region Helpers

        [CLSCompliant(false)]
        protected void RunChecked(Func<uint, DbRetCode> libFunc) {
            var handle = CheckDisposed();
            var ret = libFunc(handle);
            ErrorUtil.CheckRetCode(ret);
        }

        [CLSCompliant(false)]
        protected delegate R LibFunc<T, out R>(uint handle, out T result);

        [CLSCompliant(false)]
        protected T GetChecked<T>(LibFunc<T, DbRetCode> libFunc) {
            var handle = CheckDisposed();
            var ret = libFunc(handle, out T result);
            ErrorUtil.CheckRetCode(ret);
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
            RunChecked((handle) => DbLib.mdb_drop(transaction.Handle, handle, false));
        }

        /// <summary>
        /// Delete and close a database. See <see cref="Close()"/> for restrictions.
        /// </summary>
        public void Drop(Transaction transaction) {
            var handle = CheckDisposed();
            var ret = DbLib.mdb_drop(transaction.Handle, handle, true);
            ErrorUtil.CheckRetCode(ret);
            SetDisposed();
        }

        /// <summary>
        /// Retrieve statistics for the database.
        /// </summary>
        public Statistics GetStats(Transaction transaction) {
            return GetChecked((uint handle, out Statistics value) => DbLib.mdb_stat(transaction.Handle, handle, out value));
        }

        /// <summary>
        /// Retrieve the options for the database.
        /// </summary>
        public int GetAllOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => DbLib.mdb_dbi_flags(transaction.Handle, handle, out value));
            return unchecked((int)opts);
        }

        /// <summary>
        /// Retrieve the options for the database.
        /// </summary>
        public DatabaseOptions GetOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => DbLib.mdb_dbi_flags(transaction.Handle, handle, out value));
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
        /// <param name="transaction">Transaction under which the Get operation is performed.</param>
        /// <param name="key">Key to specify which data item / record to retrieve.</param>
        /// <param name="data">Data / record to be returned.</param>
        /// <returns><c>true</c> if data for key retrieved without error, <c>false</c> if key does not exist.</returns>
        public bool Get(Transaction transaction, in ReadOnlySpan<byte> key, out ReadOnlySpan<byte> data) {
            DbRetCode ret;
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(key);
                var dbData = default(DbValue);
                ret = DbLib.mdb_get(transaction.Handle, handle, in dbKey, in dbData);
                data = dbData.ToReadOnlySpan();
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal implementation of the Put operation for use by public operations.
        /// </summary>
        /// <param name="transaction">Transaction under which the Put operation is performed.</param>
        /// <param name="key">Key to identify the Data item to be stored.</param>
        /// <param name="data">Data / record to be stored.</param>
        /// <param name="options">Options to specify how the Put operation should be performed.</param>
        /// <returns><c>true</c> if data for key could be stored without error, <c>false</c> if key already exists (depending on options).</returns>
        [CLSCompliant(false)]
        protected bool PutInternal(Transaction transaction, in ReadOnlySpan<byte> key, in ReadOnlySpan<byte> data, uint options) {
            DbRetCode ret;
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(key);
                var dbData = DbValue.From(data);
                ret = DbLib.mdb_put(transaction.Handle, handle, in dbKey, in dbData, options);
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Store items into a database.
        /// This function stores key/data pairs in the database. The default behavior is to enter
        /// the new key/data pair, replacing any previously existing key.
        /// </summary>
        /// <param name="transaction">Transaction under which the Put operation is performed.</param>
        /// <param name="key">Key to identify the Data item to be stored.</param>
        /// <param name="data">Data / record to be stored.</param>
        /// <param name="options">Options to specify how the Put operation should be performed.</param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="PutOptions.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool Put(Transaction transaction, in ReadOnlySpan<byte> key, in ReadOnlySpan<byte> data, PutOptions options) {
            return PutInternal(transaction, in key, in data, unchecked((uint)options));
        }

        /// <summary>
        /// Delete items from a database. This function removes key/data pairs from the database.
        /// If this instance is a <see cref="MultiValueDatabase"/> then all of the duplicate data items for the key will be deleted.
        /// This function will return MDB_NOTFOUND if the specified key/data pair is not in the database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        public bool Delete(Transaction transaction, in ReadOnlySpan<byte> key) {
            DbRetCode ret;
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(key);
                ret = DbLib.mdb_del(transaction.Handle, handle, in dbKey, IntPtr.Zero);
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Compare two data items according to a particular database.
        /// This returns a comparison as if the two data items were keys in the specified database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>&lt; 0 if x &lt; y, 0 if x == y, &gt; 0 if x &gt; y</returns>
        public int Compare(Transaction transaction, in ReadOnlySpan<byte> x, in ReadOnlySpan<byte> y) {
            int result;
            var handle = CheckDisposed();
            unsafe {
                var dbx = DbValue.From(x);
                var dby = DbValue.From(y);
                result = DbLib.mdb_cmp(transaction.Handle, handle, in dbx, in dby);
            }
            return result;
        }

        protected IntPtr OpenCursorHandle(Transaction transaction) {
            IntPtr cur;
            var handle = CheckDisposed();
            DbRetCode ret = DbLib.mdb_cursor_open(transaction.Handle, handle, out cur);
            ErrorUtil.CheckRetCode(ret);
            return cur;
        }

        /// <summary>
        /// Create a cursor. A cursor is associated with a specific transaction and database.
        /// A cursor cannot be used when its database handle is closed. Nor when its transaction has ended, except with mdb_cursor_renew().
        /// It can be discarded with mdb_cursor_close(). A cursor in a write-transaction can be closed before its transaction ends,
        /// and will otherwise be closed when its transaction ends. A cursor in a read-only transaction must be closed explicitly,
        /// before or after its transaction ends. It can be reused with mdb_cursor_renew() before finally closing it.
        /// Note: Earlier documentation said that cursors in every transaction were closed when the transaction committed or aborted.
        /// Note: If one does not close database handles (leaving that to the environment), then one does not have to worry about
        ///       closing a cursor before closing its database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>Instance of <see cref="Cursor"/>.</returns>
        public virtual Cursor OpenCursor(Transaction transaction) {
            var cur = OpenCursorHandle(transaction);
            var cursor = new Cursor(cur, transaction is ReadOnlyTransaction);
            transaction.AddCursor(cursor);
            return cursor;
        }

        #region Unmanaged Resources

        readonly IntPtr env;

        uint dbi;
        internal uint Handle {
            get {
                Interlocked.MemoryBarrier();
                uint result = dbi;
                return result;
            }
        }

        #endregion

        #region IDisposable Support

        void ThrowDisposed() {
            throw new ObjectDisposedException($"{GetType().Name} '{Name}'");
        }

        /// <summary>
        /// Returns Database handle.
        /// Throws if Database handle is already closed/disposed of.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint CheckDisposed() {
            // avoid multiple volatile memory access
            Interlocked.MemoryBarrier();
            uint result = this.dbi;
            Interlocked.MemoryBarrier();
            if (result == 0)
                ThrowDisposed();
            return result;
        }

        internal void ClearHandle() {
            Interlocked.MemoryBarrier();
            dbi = 0;
        }

        void SetDisposed() {
            ClearHandle();
            disposed?.Invoke(this);
        }

        /// <summary>
        /// Returns if Database handle is closed/disposed.
        /// </summary>
        public bool IsDisposed {
            get {
                Interlocked.MemoryBarrier();
                bool result = dbi == 0;
                return result;
            }
        }

        /// <summary>
        /// Workaround as uint and ulong are not supported by Interlocked.CompareExchange() yet.
        /// </summary>
        static unsafe uint InterlockedCompareExchange(ref uint location, uint value, uint comparand) {
            fixed (uint* ptr = &location)
                unchecked {
                    return (uint)Interlocked.CompareExchange(ref *(int*)ptr, (int)value, (int)comparand);
                }
        }

        /// <summary>
        /// Implementation of Dispose() pattern. See <see cref="Dispose()"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> if explicity disposing (finalizer not run), <c>false</c> if disposed from finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            uint handle = 0;
            RuntimeHelpers.PrepareConstrainedRegions();
            try { /* */ }
            finally {
                handle = InterlockedCompareExchange(ref dbi, 0, dbi);
                if (handle != 0) {
                    DbLib.mdb_dbi_close(env, handle);
                }
            }

            if (handle != 0)
                disposed?.Invoke(this);
        }

        /// <summary>
        /// Same as <see cref="Close"/>. Close a database handle. Normally unnecessary, as databases dont have unmanaged
        /// resources that can be cleaned up independently of the environment. Database handles should only be disposed if one
        /// wants to reuse them so that the limit of database handles is not exceeded.
        /// </summary>
        /// <remarks>Use with care:
        /// This call is not mutex protected. Handles should only be closed by a single thread, and only
        /// if no other threads are going to reference the database handle or one of its cursors any further.
        /// Do not close a handle if an existing transaction has modified its database.
        /// Doing so can cause misbehavior from database corruption to errors like MDB_BAD_VALSIZE(since the DB name is gone).
        /// Closing a database handle is not necessary, but lets mdb_dbi_open() reuse the handle value.
        /// Usually it's better to set a bigger mdb_env_set_maxdbs(), unless that value would be large.
        /// </remarks>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
