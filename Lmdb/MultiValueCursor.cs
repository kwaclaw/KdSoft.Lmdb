using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Cursor for multi-value database.
    /// </summary>
    public class MultiValueCursor: Cursor
    {
        internal MultiValueCursor(IntPtr cur, bool isReadOnly, Action<Cursor> disposed = null) : base(cur, isReadOnly, disposed) {
            //
        }

        #region Read Operations

        public bool GetNextKey(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_NEXT_NODUP);
        }

        public bool GetPreviousKey(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_PREV_NODUP);
        }

        /// <summary>
        /// Returns first record for next key.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool GetNextByKey(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_NEXT_NODUP);
        }

        /// <summary>
        /// Returns last record for previous key.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool GetPreviousByKey(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_PREV_NODUP);
        }

        public bool GetAt(in KeyDataPair keyData, out KeyDataPair entry) {
            return Get(in keyData, out entry, DbCursorOp.MDB_GET_BOTH);
        }

        public bool GetNearest(in KeyDataPair keyData, out KeyDataPair entry) {
            return Get(in keyData, out entry, DbCursorOp.MDB_GET_BOTH_RANGE);
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Store by cursor.
        /// This function stores key/data pairs into the database.
        /// The cursor is positioned at the new item, or on failure usually near it.
        /// Note: Earlier documentation incorrectly said errors would leave the state of the cursor unchanged.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="option"></param>
        /// <remarks><c>true</c> if inserted without error, <c>false</c> if <see cref="MultiValueCursorPutOption.NoDuplicateData"/>
        /// was specified and the key/data pair already exists.</remarks>
        public bool Put(in KeyDataPair entry, MultiValueCursorPutOption option) {
            return PutInternal(in entry, unchecked((uint)option));
        }

        //TODO remove when not needed to work around compiler bug - crashes Visual Studio
        [StructLayout(LayoutKind.Sequential, Pack = Compile.PackSize)]
        ref struct MultiDbValue
        {
            public DbValue Val1;
            public DbValue Val2;
        }

        /// <summary>
        /// Store multiple contiguous data elements by cursor.
        /// This is only valid for <see cref="MultiValueDatabase"/> instances opened with <see cref="MultiValueDatabaseOptions.DuplicatesFixed"/>.
        /// The cursor is positioned at the new item, or on failure usually near it.
        /// Note: Earlier documentation incorrectly said errors would leave the state of the cursor unchanged.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="itemCount">On input, # of items to store, on output, number of items actually stored.</param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOption.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool PutMultiple(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, ref int itemCount) {
            DbRetCode ret;
            int firstDataSize = data.Length / itemCount;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    MultiDbValue dbMultiData;
                    //var dbMultiData = stackalloc DbValue[2];  //TODO compiler bug, use once it is fixed
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key))
                    fixed (void* firstDataPtr = &MemoryMarshal.GetReference(data)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        dbMultiData.Val1 = new DbValue(firstDataPtr, firstDataSize);
                        dbMultiData.Val2 = new DbValue(null, itemCount);
                        //TODO change this to use stackalloc once compiler bug is fixed
                        // dbMultiData[0] = new DbValue(firstDataPtr, firstDataSize);
                        // dbMultiData[1] = new DbValue(null, itemCount);
                        // ret = Lib.mdb_cursor_put(handle, ref dbKey, &dbMultiData, LibConstants.MDB_MULTIPLE);
                        // itemCount = (int)dbMultiData[1].Size;
                        ret = DbLib.mdb_cursor_put(handle, ref dbKey, &dbMultiData.Val1, DbLibConstants.MDB_MULTIPLE);
                        itemCount = (int)dbMultiData.Val2.Size;
                    }
                }
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Delete current key/data pair.
        /// This function deletes the key/data pair to which the cursor refers.
        /// </summary>
        public void Delete(MultiValueCursorDeleteOption mvOption, CursorDeleteOption option = CursorDeleteOption.None) {
            DbRetCode ret;
            var opts = unchecked((uint)option | (uint)mvOption);
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = DbLib.mdb_cursor_del(handle, opts);
            }
            ErrorUtil.CheckRetCode(ret);
        }

        #endregion

        /// <summary>
        /// Return count of duplicates for current key.
        /// </summary>
        public IntPtr Count() {
            DbRetCode ret;
            IntPtr result;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = DbLib.mdb_cursor_count(handle, out result);
            }
            ErrorUtil.CheckRetCode(ret);
            return result;
        }

        #region Enumeration

        /// <summary>
        /// Iterates over all keys in sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.Forward"/>.
        /// </summary>
        public ItemsIterator ForwardByKey => new ItemsIterator(this, DbCursorOp.MDB_FIRST, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over all keys in sort order, from the next position on.
        /// </summary>
        public NextItemsIterator ForwardFromByKey => new NextItemsIterator(this, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over all keys in reverse sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.Reverse"/>.
        /// </summary>
        public ItemsIterator ReverseByKey => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over keys in reverse sort order, from the previous position on.
        /// </summary>
        public NextItemsIterator ReverseFromByKey => new NextItemsIterator(this, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over all duplicate records for the current key.
        /// </summary>
        public ValuesIterator ValuesForward => new ValuesIterator(this, DbCursorOp.MDB_FIRST_DUP, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates over duplicate records for the current key, in duplicate sort order from the next position on.
        /// </summary>
        public ValuesNextIterator ValuesForwardFrom => new ValuesNextIterator(this, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates in reverse over all duplicate records for the current key.
        /// </summary>
        public ValuesIterator ValuesReverse => new ValuesIterator(this, DbCursorOp.MDB_LAST_DUP, DbCursorOp.MDB_PREV_DUP);

        /// <summary>
        /// Iterates in reverse over duplicate records for the current key, in reverse duplicate sort order from the previous position on.
        /// </summary>
        public ValuesNextIterator ValuesReverseFrom => new ValuesNextIterator(this, DbCursorOp.MDB_PREV_DUP);

        #endregion

        #region Nested types
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible

        public struct ValuesIterator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;

            internal ValuesIterator(MultiValueCursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opFirst = opFirst;
                this.opNext = opNext;
            }

            public ValuesEnumerator GetEnumerator() => new ValuesEnumerator(cursor, opFirst, opNext);
        }

        public struct ValuesNextIterator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opNext;

            internal ValuesNextIterator(MultiValueCursor cursor, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opNext = opNext;
            }

            public ValuesNextEnumerator GetEnumerator() => new ValuesNextEnumerator(cursor, opNext);
        }

        public ref struct ValuesEnumerator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;
            ReadOnlySpan<byte> current;
            bool isCurrent;
            bool isInitialized;

            internal ValuesEnumerator(MultiValueCursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opFirst = opFirst;
                this.opNext = opNext;
                this.current = default(ReadOnlySpan<byte>);
                this.isCurrent = false;
                this.isInitialized = false;
            }

            public ReadOnlySpan<byte> Current {
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
                    return isCurrent = cursor.GetData(out current, opNext);
                else {
                    isInitialized = true;
                    return isCurrent = cursor.GetData(out current, opFirst);
                }
            }
        }

        public ref struct ValuesNextEnumerator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opNext;
            ReadOnlySpan<byte> current;
            bool isCurrent;

            internal ValuesNextEnumerator(MultiValueCursor cursor, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opNext = opNext;
                this.current = default;
                this.isCurrent = false;
            }

            public ReadOnlySpan<byte> Current {
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
                return isCurrent = cursor.GetData(out current, opNext);
            }
        }

#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1815 // Override equals and operator equals on value types
        #endregion

    }
}
