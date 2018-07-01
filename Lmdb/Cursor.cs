using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Cursor for LMDB database.
    /// </summary>
    public class Cursor: IDisposable
    {
        readonly bool isReadOnly;

        internal Cursor(IntPtr cur, bool isReadOnly, Action<Cursor> disposed = null) {
            this.cur = cur;
            this.isReadOnly = isReadOnly;
            this.disposed = disposed;
        }

        Action<Cursor> disposed;
        internal Action<Cursor> Disposed {
            get => disposed;
            set => disposed = value;
        }

        /// <summary>Native transaction handle. </summary>
        public IntPtr TxnHandle {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return DbLib.mdb_cursor_txn(handle);
                }
            }
        }

        /// <summary>Indicates if cursor is read-only. </summary>
        public bool IsReadOnly => isReadOnly;

        /// <summary>Native database handle. </summary>
        public IntPtr DbiHandle {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return DbLib.mdb_cursor_dbi(handle);
                }
            }
        }

        /// <summary>
        /// Same as <see cref="Dispose()"/>. Close a cursor handle.
        /// The cursor handle will be freed and must not be used again after this call.
        /// Its transaction must still be live if it is a write-transaction.
        /// </summary>
        public void Close() {
            Dispose();
        }

        /// <summary>
        /// Renews cursor, allows re-use without re-allocation.
        /// Only valid for read-only cursors.
        /// </summary>
        public void Renew(ReadOnlyTransaction transaction) {
            if (!IsReadOnly)
                throw new InvalidOperationException("Only read-only cursors can be renewed.");
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = DbLib.mdb_cursor_renew(transaction.Handle, handle);
            }
            ErrorUtil.CheckRetCode(ret);
        }

        #region GET implementations

        /// <summary>
        /// Internal GET operation. Valid for <see cref="DbCursorOp.MDB_SET_KEY"/> and <see cref="DbCursorOp.MDB_SET_RANGE"/>.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool Get(in ReadOnlySpan<byte> key, out KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        var dbData = default(DbValue);
                        ret = DbLib.mdb_cursor_get(handle, ref dbKey, &dbData, op);
                        entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal GET operation. Valid for <see cref="DbCursorOp.MDB_GET_BOTH"/> and <see cref="DbCursorOp.MDB_GET_BOTH_RANGE"/>.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool Get(in KeyDataPair keyData, out KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(keyData.Key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(keyData.Data)) {
                        var dbKey = new DbValue(keyPtr, keyData.Key.Length);
                        var dbData = new DbValue(dataPtr, keyData.Data.Length);
                        ret = DbLib.mdb_cursor_get(handle, ref dbKey, &dbData, op);
                        entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal MOVE TO KEY operation. Valid for <see cref="DbCursorOp.MDB_SET"/>.
        /// Does not return key or data.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool MoveToKey(in ReadOnlySpan<byte> key, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        ret = DbLib.mdb_cursor_get(handle, ref dbKey, null, op);
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal GET KEY operation. Valid for <see cref="DbCursorOp.MDB_GET_CURRENT"/>,
        /// <see cref="DbCursorOp.MDB_FIRST"/>, <see cref="DbCursorOp.MDB_NEXT"/>, <see cref="DbCursorOp.MDB_PREV"/>
        /// and <see cref="DbCursorOp.MDB_LAST"/>. Returns key, but not data.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool GetKey(out ReadOnlySpan<byte> key, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    var dbKey = default(DbValue);
                    ret = DbLib.mdb_cursor_get(handle, ref dbKey, null, op);
                    key = dbKey.ToReadOnlySpan();
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal GET operation. Valid for <see cref="DbCursorOp.MDB_GET_CURRENT"/>,
        /// <see cref="DbCursorOp.MDB_FIRST"/>, <see cref="DbCursorOp.MDB_NEXT"/>, <see cref="DbCursorOp.MDB_PREV"/>
        /// and <see cref="DbCursorOp.MDB_LAST"/>. Returns both, key and data.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool Get(out KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    var dbKey = default(DbValue);
                    var dbData = default(DbValue);
                    ret = DbLib.mdb_cursor_get(handle, ref dbKey, &dbData, op);
                    entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Internal GET DATA operation. Valid for <see cref="DbCursorOp.MDB_FIRST_DUP"/>, <see cref="DbCursorOp.MDB_NEXT_DUP"/>, 
        /// <see cref="DbCursorOp.MDB_NEXT_NODUP"/>, <see cref="DbCursorOp.MDB_PREV_DUP"/>, <see cref="DbCursorOp.MDB_PREV_NODUP"/>
        /// <see cref="DbCursorOp.MDB_LAST_DUP"/>, <see cref="DbCursorOp.MDB_GET_MULTIPLE"/>, <see cref="DbCursorOp.MDB_NEXT_MULTIPLE"/>
        /// and <see cref="DbCursorOp.MDB_PREV_MULTIPLE"/>. Returns data, but not key.
        /// </summary>
        /// <returns><c>true</c>if record was found, <c>false</c> otherwise.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        protected bool GetData(out ReadOnlySpan<byte> data, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    var dbKey = default(DbValue);
                    var dbData = default(DbValue);
                    ret = DbLib.mdb_cursor_get(handle, ref dbKey, &dbData, op);
                    data = dbData.ToReadOnlySpan();
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        #endregion

        #region Read and Move Operations

        /// <summary>
        /// Moves cursor to key position.
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if successful (key exists), false otherwise.</returns>
        public bool MoveToKey(in ReadOnlySpan<byte> key) {
            return MoveToKey(in key, DbCursorOp.MDB_SET);
        }

        /// <summary>
        /// Gets record (key and data) at key position.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if successful (key exists), false otherwise.</returns>
        public bool GetAt(in ReadOnlySpan<byte> key, out KeyDataPair entry) {
            return Get(in key, out entry, DbCursorOp.MDB_SET_KEY);
        }

        /// <summary>
        /// Move cursor to first position greater than or equal to specified key and get the record (key and data).
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if successful (nearest key exists), false otherwise.</returns>
        public bool GetNearest(in ReadOnlySpan<byte> key, out KeyDataPair entry) {
            return Get(in key, out entry, DbCursorOp.MDB_SET_RANGE);
        }

        /// <summary>
        /// Gets key at current position.
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if successful (key exists), false otherwise.</returns>
        public bool GetCurrent(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_GET_CURRENT);
        }

        /// <summary>
        /// Get record (key and data) at current position.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if successful (key exists), false otherwise.</returns>
        public bool GetCurrent(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_GET_CURRENT);
        }

        /// <summary>
        /// Move cursor to next position and get the record (key and data).
        /// </summary>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if successful (next key exists), false otherwise.</returns>
        public bool GetNext(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_NEXT);
        }

        /// <summary>
        /// Move cursor to previous position and get the record (key and data).
        /// </summary>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if successful (previous key exists), false otherwise.</returns>
        public bool GetPrevious(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_PREV);
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Internal PUT operation. Inserts or updates data at key position.
        /// </summary>
        /// <returns><c>true</c> if inserted or updated without error, <c>false</c> if
        /// <see cref="CursorPutOption.NoOverwrite"/> was specified and the key already exists.</returns>
        /// <exception cref="LmdbException">Exception based on native return code.</exception>
        [CLSCompliant(false)]
        protected bool PutInternal(in KeyDataPair entry, uint option) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(entry.Key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(entry.Data)) {
                        var dbKey = new DbValue(keyPtr, entry.Key.Length);
                        var dbValue = new DbValue(dataPtr, entry.Data.Length);
                        ret = DbLib.mdb_cursor_put(handle, ref dbKey, &dbValue, option);
                    }
                }
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Store by cursor.
        /// This function stores key/data pairs into the database.
        /// The cursor is positioned at the new item, or on failure usually near it.
        /// Note: Earlier documentation incorrectly said errors would leave the state of the cursor unchanged.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="option"></param>
        /// <returns><c>true</c> if inserted or updated without error, <c>false</c> if <see cref="CursorPutOption.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool Put(in KeyDataPair entry, CursorPutOption option) {
            return PutInternal(in entry, unchecked((uint)option));
        }

        /// <summary>
        /// Delete current key/data pair.
        /// This function deletes the key/data pair to which the cursor refers.
        /// </summary>
        public void Delete(CursorDeleteOption option = CursorDeleteOption.None) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = DbLib.mdb_cursor_del(handle, unchecked((uint)option));
            }
            ErrorUtil.CheckRetCode(ret);
        }

        #endregion

        #region Unmanaged Resources

        protected readonly object rscLock = new object();

        volatile IntPtr cur;
        internal IntPtr Handle => cur;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            DbLib.mdb_cursor_close(cur);
        }

        #endregion

        #region IDisposable Support

        public const string disposedStr = "Database handle closed.";

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            IntPtr result = this.cur;
            if (result == IntPtr.Zero)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        internal void ClearHandle() {
            lock (rscLock) {
                cur = IntPtr.Zero;
            }
        }

        void SetDisposed() {
            cur = IntPtr.Zero;
            disposed?.Invoke(this);
        }

        public bool IsDisposed {
            get { return cur == IntPtr.Zero; }
        }

        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (cur == IntPtr.Zero)  // already disposed
                    return;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { /* */ }
                finally {
                    if (disposing) {
                        // dispose managed state (managed objects).
                    }
                    ReleaseUnmanagedResources();
                    if (disposing)
                        SetDisposed();
                }
            }
        }

        ~Cursor() {
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

        #region Enumeration

        /// <summary>
        /// Iterates over all records in sort order. For use in foreach loop.
        /// </summary>
        public ItemsIterator Forward => new ItemsIterator(this, DbCursorOp.MDB_FIRST, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over records in sort order, from the next position on. For use in foreach loops.
        /// If duplicates are allowed, then the next position may be on the same key, in duplicate sort order.
        /// </summary>
        public NextItemsIterator ForwardFrom => new NextItemsIterator(this, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in reverse sort order. For use in foreach loops.
        /// </summary>
        public ItemsIterator Reverse => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over records in reverse sort order, from the previous position on. For use in foreach loops.
        /// If duplicates are allowed, then the previous position may be on the same key, in reverse duplicate sort order.
        /// </summary>
        public NextItemsIterator ReverseFrom => new NextItemsIterator(this, DbCursorOp.MDB_PREV);

        #endregion

        #region Nested types
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible

        public delegate bool GetItem(out KeyDataPair item);

        /// <summary>
        /// Implements the Enumerable pattern for use with foreach loops when the items to be iterated over
        /// can only live on the stack. This works because the foreach loop does not require an explicit
        /// implementaion of the <see cref="System.Collections.IEnumerable"/> interface.
        /// </summary>
        /// <remarks><a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable#remarks">
        /// See Remarks in the <c>IEnumerable</c> documentation.
        /// </a></remarks>  
        public struct ItemsIterator
        {
            readonly Cursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;

            internal ItemsIterator(Cursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opFirst = opFirst;
                this.opNext = opNext;
            }

            bool GetFirstItem(out KeyDataPair item) => cursor.Get(out item, opFirst);
            bool GetNextItem(out KeyDataPair item) => cursor.Get(out item, opNext);

            public ItemsEnumerator GetEnumerator() => new ItemsEnumerator(GetFirstItem, GetNextItem);
        }

        /// <summary>
        /// Implements the Enumerable pattern for use with foreach loops when the items to be iterated over
        /// can only live on the stack. This works because the foreach loop does not require an explicit
        /// implementaion of the <see cref="System.Collections.IEnumerable"/> interface.
        /// </summary>
        /// <remarks><a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable#remarks">
        /// See Remarks in the <c>IEnumerable</c> documentation.
        /// </a></remarks>  
        public ref struct NextItemsIterator
        {
            readonly Cursor cursor;
            readonly DbCursorOp nextOp;

            internal NextItemsIterator(Cursor cursor, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.nextOp = nextOp;
            }

            public NextItemsEnumerator GetEnumerator() => new NextItemsEnumerator(cursor, nextOp);
        }

        /// <summary>
        /// Implements Enumerator pattern for use with foreach loops when the items to be iterated over
        /// can only live on the stack. This works because the foreach loop does not require an explicit
        /// implementaion of the <see cref="System.Collections.IEnumerator"/> interface.
        /// </summary>
        /// <remarks><a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable#remarks">
        /// See Remarks in the <c>IEnumerable</c> documentation.
        /// </a></remarks>  
        public ref struct ItemsEnumerator
        {
            readonly GetItem getFirst;
            readonly GetItem getNext;
            KeyDataPair current;
            bool isCurrent;
            bool isInitialized;

            internal ItemsEnumerator(GetItem getFirst, GetItem getNext) {
                this.getFirst = getFirst;
                this.getNext = getNext;
                this.current = default;
                this.isCurrent = false;
                this.isInitialized = false;
            }

            public KeyDataPair Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    if (isCurrent)
                        return current;
                    else
                        throw new InvalidOperationException("Invalid cursor position.");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() {
                if (isInitialized)
                    return isCurrent = getNext(out current);
                else {
                    isInitialized = true;
                    return isCurrent = getFirst(out current);
                }
            }
        }

        /// <summary>
        /// Implements Enumerator pattern for use with foreach loops when the items to be iterated over
        /// can only live on the stack. This works because the foreach loop does not require an explicit
        /// implementation of the <see cref="System.Collections.IEnumerator"/> interface.
        /// </summary>
        /// <remarks><a href="https://docs.microsoft.com/en-us/dotnet/api/system.collections.ienumerable#remarks">
        /// See Remarks in the <c>IEnumerable</c> documentation.
        /// </a></remarks>  
        public ref struct NextItemsEnumerator
        {
            readonly Cursor cursor;
            readonly DbCursorOp nextOp;
            KeyDataPair current;
            bool isCurrent;

            internal NextItemsEnumerator(Cursor cursor, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.nextOp = nextOp;
                this.current = default;
                this.isCurrent = false;
            }

            public KeyDataPair Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    if (isCurrent)
                        return current;
                    else
                        throw new InvalidOperationException("Invalid cursor position.");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() {
                return isCurrent = cursor.Get(out current, nextOp);
            }
        }

#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1815 // Override equals and operator equals on value types
        #endregion
    }
}
