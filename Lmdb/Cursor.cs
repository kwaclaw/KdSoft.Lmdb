using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Cursor for LMDB database.
    /// </summary>
    public class Cursor: IDisposable
    {
        internal Cursor(IntPtr cur, bool isReadOnly, Action<Cursor> disposed = null) {
            this.cur = cur;
            this.IsReadOnly = isReadOnly;
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
                var handle = CheckDisposed();
                return DbLib.mdb_cursor_txn(handle);
            }
        }

        /// <summary>Indicates if cursor is read-only. </summary>
        public bool IsReadOnly { get; }

        /// <summary>Native database handle. </summary>
        public IntPtr DbiHandle {
            get {
                var handle = CheckDisposed();
                return DbLib.mdb_cursor_dbi(handle);
            }
        }

        /// <summary>
        /// Same as <see cref="Dispose()"/>. Close a cursor handle.
        /// The cursor handle will be freed and must not be used again after this call.
        /// Its transaction must still be live if it is a write-transaction.
        /// </summary>
        /// <remarks>
        /// Cursors will be automatically closed when they are owned by a write transaction.
        /// However, in a read-only transaction, cursors must be closed explicitly.
        /// </remarks>
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
            var handle = CheckDisposed();
            var ret = DbLib.mdb_cursor_renew(transaction.Handle, handle);
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(key);
                var dbData = default(DbValue);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, &dbData, op);
                entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(keyData.Key);
                var dbData = DbValue.From(keyData.Data);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, &dbData, op);
                entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(key);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, null, op);
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = default(DbValue);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, null, op);
                key = dbKey.ToReadOnlySpan();
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = default(DbValue);
                var dbData = default(DbValue);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, &dbData, op);
                entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = default(DbValue);
                var dbData = default(DbValue);
                ret = DbLib.mdb_cursor_get(handle, in dbKey, &dbData, op);
                data = dbData.ToReadOnlySpan();
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
        /// Move cursor to key position and get the record (key and data).
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
        /// Get key at current position.
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
            var handle = CheckDisposed();
            unsafe {
                var dbKey = DbValue.From(entry.Key);
                var dbData = DbValue.From(entry.Data);
                ret = DbLib.mdb_cursor_put(handle, in dbKey, &dbData, option);
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
            var handle = CheckDisposed();
            DbRetCode ret = DbLib.mdb_cursor_del(handle, unchecked((uint)option));
            ErrorUtil.CheckRetCode(ret);
        }

        #endregion

        #region Unmanaged Resources

        IntPtr cur;
        internal IntPtr Handle {
            get {
                Interlocked.MemoryBarrier();
                IntPtr result = cur;
                Interlocked.MemoryBarrier();
                return result;
            }
        }

        #endregion

        #region IDisposable Support

        void ThrowDisposed() {
            throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Returns Cursor handle.
        /// Throws if Cursor handle is already closed/disposed of.
        /// </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            Interlocked.MemoryBarrier();
            IntPtr result = this.cur;
            Interlocked.MemoryBarrier();
            if (result == IntPtr.Zero)
                ThrowDisposed();
            return result;
        }

        internal void ClearHandle() {
            Interlocked.MemoryBarrier();
            cur = IntPtr.Zero;
            Interlocked.MemoryBarrier();
        }

        void SetDisposed() {
            ClearHandle();
            disposed?.Invoke(this);
        }

        /// <summary>
        /// Returns if Cursor handle is closed/disposed.
        /// </summary>
        public bool IsDisposed {
            get {
                Interlocked.MemoryBarrier();
                bool result = cur == IntPtr.Zero;
                return result;
            }
        }

        /// <summary>
        /// Close a cursor handle. Sames as <see cref="Close"/>.
        /// The cursor handle will be freed and must not be used again after this call.
        /// Its transaction must still be live if it is a write-transaction. 
        /// </summary>
        /// <remarks>
        /// Cursors will be automatically closed when they are owned by a write transaction.
        /// However, in a read-only transaction, cursors must be closed explicitly.
        /// </remarks>
        public void Dispose() {
            IntPtr handle = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try { /* */ }
            finally {
                handle = Interlocked.CompareExchange(ref cur, IntPtr.Zero, cur);
                if (handle != IntPtr.Zero) {
                    DbLib.mdb_cursor_close(handle);
                }
            }

            if (handle != IntPtr.Zero)
                disposed?.Invoke(this);
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
        public NextItemsIterator ForwardFromNext => new NextItemsIterator(this, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over records in sort order, from the current position on. For use in foreach loops.
        /// If duplicates are allowed, then the next position may be on the same key, in duplicate sort order.
        /// </summary>
        public ItemsIterator ForwardFromCurrent => new ItemsIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in reverse sort order. For use in foreach loops.
        /// </summary>
        public ItemsIterator Reverse => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over records in reverse sort order, from the previous position on. For use in foreach loops.
        /// If duplicates are allowed, then the previous position may be on the same key, in reverse duplicate sort order.
        /// </summary>
        public NextItemsIterator ReverseFromPrevious => new NextItemsIterator(this, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over records in reverse sort order, from the current position on. For use in foreach loops.
        /// If duplicates are allowed, then the previous position may be on the same key, in reverse duplicate sort order.
        /// </summary>
        public ItemsIterator ReverseFromCurrent => new ItemsIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_PREV);

        #endregion

        #region Nested types
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible

        internal delegate bool GetItem(out KeyDataPair item);

        /// <summary>
        /// Implements the Enumerable pattern for use with foreach loops when the items to be iterated over
        /// can only live on the stack. This works because the foreach loop does not require an explicit
        /// implementaion of the <see cref="IEnumerable"/> interface.
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

            /// <summary>Equivalent to <see cref="IEnumerable.GetEnumerator()"/>.</summary>
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

            /// <summary>Equivalent to <see cref="IEnumerable.GetEnumerator()"/>.</summary>
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

            /// <summary>Equivalent to <see cref="IEnumerator.Current"/>.</summary>
            public KeyDataPair Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    if (isCurrent)
                        return current;
                    else
                        throw new InvalidOperationException("Invalid cursor position.");
                }
            }

            /// <summary>Equivalent to <see cref="IEnumerator.MoveNext()"/>.</summary>
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

            /// <summary>Equivalent to <see cref="IEnumerator.Current"/>.</summary>
            public KeyDataPair Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get {
                    if (isCurrent)
                        return current;
                    else
                        throw new InvalidOperationException("Invalid cursor position.");
                }
            }

            /// <summary>Equivalent to <see cref="IEnumerator.MoveNext()"/>.</summary>
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
