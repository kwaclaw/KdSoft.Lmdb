using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class Cursor: IDisposable
    {
        //TODO make internal
        public Cursor(IntPtr cursor, Action<Cursor> disposed) {
            this.cursor = cursor;
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
                    return Lib.mdb_cursor_txn(handle);
                }
            }
        }

        public IntPtr DbiHandle {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return Lib.mdb_cursor_dbi(handle);
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

        // valid operations: MDB_GET_BOTH, MDB_GET_BOTH_RANGE
        protected bool Get(ref KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(entry.Key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(entry.Data)) {
                        var dbKey = new DbValue(keyPtr, entry.Key.Length);
                        var dbData = new DbValue(dataPtr, entry.Data.Length);
                        ret = Lib.mdb_cursor_get(handle, ref dbKey, ref dbData, op);
                        entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_SET, MDB_SET_KEY, MDB_SET_RANGE,
        // ignores entry.Data on input
        protected bool GetData(ref KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(entry.Key)) {
                        var dbKey = new DbValue(keyPtr, entry.Key.Length);
                        var dbData = default(DbValue);
                        ret = Lib.mdb_cursor_get(handle, ref dbKey, ref dbData, op);
                        entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
                    }
                }
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        // valid operations: MDB_GET_CURRENT, MDB_FIRST, MDB_FIRST_DUP,
        //                   MDB_NEXT, MDB_NEXT_DUP, MDB_NEXT_NODUP
        //                   MDB_PREV, MDB_PREV_DUP, MDB_PREV_NODUP,
        //                   MDB_LAST, MDB_LAST_DUP,
        //                   MDB_GET_MULTIPLE, MDB_NEXT_MULTIPLE, MDB_PREV_MULTIPLE
        protected bool GetAll(out KeyDataPair entry, DbCursorOp op) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                var dbKey = default(DbValue);
                var dbData = default(DbValue);
                ret = Lib.mdb_cursor_get(handle, ref dbKey, ref dbData, op);
                entry = new KeyDataPair(dbKey.ToReadOnlySpan(), dbData.ToReadOnlySpan());
            }
            if (ret == DbRetCode.NOTFOUND)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        [CLSCompliant(false)]
        protected bool PutInternal(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, uint options) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(data)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        var dbValue = new DbValue(dataPtr, data.Length);
                        ret = Lib.mdb_cursor_put(handle, ref dbKey, ref dbValue, options);
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
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <remarks><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOptions.NoOverwrite"/>
        /// was specified and the key already exists.</remarks>
        public bool Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, CursorPutOptions options) {
            return PutInternal(key, data, unchecked((uint)options));
        }

        /// <summary>
        /// Delete current key/data pair.
        /// This function deletes the key/data pair to which the cursor refers.
        /// </summary>
        public void Delete(CursorDeleteOptions options = CursorDeleteOptions.None) {
            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = Lib.mdb_cursor_del(handle, unchecked((uint)options));
            }
            Util.CheckRetCode(ret);
        }

        #region Unmanaged Resources

        protected readonly object rscLock = new object();

        volatile IntPtr cursor;
        internal IntPtr Handle => cursor;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            Lib.mdb_cursor_close(cursor);
        }

        #endregion

        #region IDisposable Support

        public const string disposedStr = "Database handle closed.";

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            IntPtr result = this.cursor;
            if (result == IntPtr.Zero)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        internal void ClearHandle() {
            lock (rscLock) {
                cursor = IntPtr.Zero;
            }
        }

        void SetDisposed() {
            cursor = IntPtr.Zero;
            disposed?.Invoke(this);
        }

        public bool IsDisposed {
            get { return cursor == IntPtr.Zero; }
        }

        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (cursor == IntPtr.Zero)  // already disposed
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

        public ForwardIterator ItemsForward => new ForwardIterator(this);

        #endregion

        #region Nested types

        public struct ForwardIterator
        {
            readonly Cursor cursor;

            public ForwardIterator(Cursor cursor) {
                this.cursor = cursor;
            }

            public ItemsForwardEnumerator GetEnumerator() => new ItemsForwardEnumerator(cursor);
        }

        public ref struct ItemsForwardEnumerator
        {
            readonly Cursor cursor;
            KeyDataPair current;
            bool isCurrent;
            bool isInitialized;

            public ItemsForwardEnumerator(Cursor cursor) {
                this.cursor = cursor;
                this.current = default(KeyDataPair);
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
                    return isCurrent = cursor.GetAll(out current, DbCursorOp.MDB_NEXT);
                else {
                    isInitialized = true;
                    return isCurrent = cursor.GetAll(out current, DbCursorOp.MDB_FIRST);
                }
            }
        }

        #endregion
    }
}
