using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
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

        public IntPtr TxnHandle {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return DbLib.mdb_cursor_txn(handle);
                }
            }
        }

        public bool IsReadOnly => isReadOnly;

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

        public void Renew(ReadOnlyTransaction transaction) {
            if (!IsReadOnly)
                throw new InvalidOperationException("Only read-only cursors can be renewed.");
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = DbLib.mdb_cursor_renew(transaction.Handle, handle);
            }
            Util.CheckRetCode(ret);
        }

        #region GET implementations

        // valid operations: MDB_SET_KEY, MDB_SET_RANGE,
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
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_GET_BOTH, MDB_GET_BOTH_RANGE
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
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_SET, does not return key or data, 
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
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_GET_CURRENT, MDB_FIRST, MDB_NEXT, MDB_PREV, MDB_LAST
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
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_GET_CURRENT, MDB_FIRST, MDB_NEXT, MDB_PREV, MDB_LAST
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
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_FIRST_DUP, MDB_NEXT_DUP, MDB_NEXT_NODUP,
        //                   MDB_PREV_DUP, MDB_PREV_NODUP, MDB_LAST_DUP,
        //                   MDB_GET_MULTIPLE, MDB_NEXT_MULTIPLE, MDB_PREV_MULTIPLE
        // returns data, but not key
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
            Util.CheckRetCode(ret);
            return true;
        }

        #endregion

        #region Read and Move Operations

        public bool MoveToKey(in ReadOnlySpan<byte> key) {
            return MoveToKey(in key, DbCursorOp.MDB_SET);
        }

        public bool GetAt(in ReadOnlySpan<byte> key, out KeyDataPair entry) {
            return Get(in key, out entry, DbCursorOp.MDB_SET_KEY);
        }

        public bool GetNearest(in ReadOnlySpan<byte> key, out KeyDataPair entry) {
            return Get(in key, out entry, DbCursorOp.MDB_SET_RANGE);
        }

        public bool GetCurrent(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_GET_CURRENT);
        }

        public bool GetCurrent(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_GET_CURRENT);
        }

        #endregion

        #region Update Operations

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
            Util.CheckRetCode(ret);
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
        /// <remarks><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOption.NoOverwrite"/>
        /// was specified and the key already exists.</remarks>
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
            Util.CheckRetCode(ret);
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
        /// Iterates over all records in sort order.
        /// </summary>
        public ItemsIterator Forward => new ItemsIterator(this, DbCursorOp.MDB_FIRST, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in sort order, from the given key on.
        /// If duplicates are allowed, will start from that key's first duplicate in duplicate sort order.
        /// </summary>
        public ItemsFromKeyIterator ForwardFrom(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_KEY, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in sort order, from the given key on, or if there is no matching key,
        /// from the next key in sort order.
        /// If duplicates are allowed, will start from that key's first duplicate in duplicate sort order.
        /// </summary>
        public ItemsFromKeyIterator ForwardFromNearest(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_RANGE, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in reverse sort order.
        /// </summary>
        public ItemsIterator Reverse => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV);


        /// <summary>
        /// Iterates over all records in reverse sort order, from the given key on.
        /// If duplicates are allowed, will start from that key's first duplicate in duplicate sort order.
        /// </summary>
        public ItemsFromKeyIterator ReverseFrom(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_KEY, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over all records in reverse sort order, from the given key on, or if there is no matching key,
        /// from the next key in sort order.
        /// If duplicates are allowed, will start from that key's first duplicate in duplicate sort order.
        /// </summary>
        public ItemsFromKeyIterator ReverseFromNearest(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_RANGE, DbCursorOp.MDB_PREV);

        #endregion

        #region Nested types

        public delegate bool GetItem(out KeyDataPair item);

        public struct ItemsIterator
        {
            readonly Cursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;

            public ItemsIterator(Cursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opFirst = opFirst;
                this.opNext = opNext;
            }

            bool GetFirstItem(out KeyDataPair item) => cursor.Get(out item, opFirst);
            bool GetNextItem(out KeyDataPair item) => cursor.Get(out item, opNext);

            public ItemsEnumerator GetEnumerator() => new ItemsEnumerator(GetFirstItem, GetNextItem);
        }

        public ref struct ItemsFromKeyIterator
        {
            readonly Cursor cursor;
            readonly ReadOnlySpan<byte> key;
            readonly DbCursorOp keyOp;
            readonly DbCursorOp nextOp;

            public ItemsFromKeyIterator(Cursor cursor, in ReadOnlySpan<byte> key, DbCursorOp keyOp, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.key = key;
                this.keyOp = keyOp;
                this.nextOp = nextOp;
            }

            public ItemsFromKeyEnumerator GetEnumerator() => new ItemsFromKeyEnumerator(cursor, key, keyOp, nextOp);
        }

        public ref struct ItemsEnumerator
        {
            readonly GetItem getFirst;
            readonly GetItem getNext;
            KeyDataPair current;
            bool isCurrent;
            bool isInitialized;

            public ItemsEnumerator(GetItem getFirst, GetItem getNext) {
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

        public ref struct ItemsFromKeyEnumerator
        {
            readonly Cursor cursor;
            readonly ReadOnlySpan<byte> key;
            readonly DbCursorOp keyOp;
            readonly DbCursorOp nextOp;
            KeyDataPair current;
            bool isCurrent;
            bool isInitialized;

            public ItemsFromKeyEnumerator(Cursor cursor, in ReadOnlySpan<byte> key, DbCursorOp keyOp, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.key = key;
                this.keyOp = keyOp;
                this.nextOp = nextOp;
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
                    return isCurrent = cursor.Get(out current, nextOp);
                else {
                    isInitialized = true;
                    return isCurrent = cursor.Get(in key, out current, keyOp);
                }
            }
        }

        #endregion
    }
}
