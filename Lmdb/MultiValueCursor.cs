using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class MultiValueCursor: Cursor
    {
        internal MultiValueCursor(IntPtr cur, bool isReadOnly, Action<Cursor> disposed = null) : base(cur, isReadOnly, disposed) {
            //
        }

        #region Read Operations

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
        /// Iterates over all keys in sort order, from the given key on.
        /// Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.ForwardFrom"/>.
        /// </summary>
        public ItemsFromKeyIterator ForwardByKeyFrom(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_KEY, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over all keys in sort order, from the given key on, or if there is no matching key,
        /// from the next key in sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.ForwardFromNearest"/>.
        /// </summary>
        public ItemsFromKeyIterator ForwardByKeyFromNearest(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_RANGE, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over all records in combined key + duplicate sort order, from the given record (key/data) on.
        /// </summary>
        public ItemsFromEntryIterator ForwardFrom(in KeyDataPair entry) =>
            new ItemsFromEntryIterator(this, entry, DbCursorOp.MDB_GET_BOTH, DbCursorOp.MDB_NEXT);

        /// <summary>
        /// Iterates over all records in combined key + duplicate sort order, from the given record (key/data) on,
        /// or if there is no matching key, from the next key and its first duplicate record in combined sort order.
        /// </summary>
        public ItemsFromEntryIterator ForwardFromNearest(in KeyDataPair entry) =>
            new ItemsFromEntryIterator(this, entry, DbCursorOp.MDB_GET_BOTH_RANGE, DbCursorOp.MDB_NEXT);


        /// <summary>
        /// Iterates over all keys in reverse sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.Reverse"/>.
        /// </summary>
        public ItemsIterator ReverseByKey => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over all keys in reverse sort order, from the given key on.
        /// Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.ReverseFrom"/>.
        /// </summary>
        public ItemsFromKeyIterator ReverseByKeyFrom(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_KEY, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over all keys in reverse sort order, from the given key on, or if there is no matching key,
        /// from the next key in sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.ReverseFromNearest"/>.
        /// </summary>
        public ItemsFromKeyIterator ReverseByKeyFromNearest(in ReadOnlySpan<byte> key) =>
            new ItemsFromKeyIterator(this, key, DbCursorOp.MDB_SET_RANGE, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over all records in reverse combined key + duplicate sort order, from the given record (key/data) on.
        /// </summary>
        public ItemsFromEntryIterator ReverseFrom(in KeyDataPair entry) =>
            new ItemsFromEntryIterator(this, entry, DbCursorOp.MDB_GET_BOTH, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over all records in reverse combined key + duplicate sort order, from the given record (key/data) on,
        /// or if there is no matching key, from the next key and its first duplicate record in combined sort order.
        /// </summary>
        public ItemsFromEntryIterator ReverseFromNearest(in KeyDataPair entry) =>
            new ItemsFromEntryIterator(this, entry, DbCursorOp.MDB_GET_BOTH_RANGE, DbCursorOp.MDB_PREV);

        /// <summary>
        /// Iterates over all duplicate records for the current key.
        /// </summary>
        public ValuesIterator ValuesForward => new ValuesIterator(this, DbCursorOp.MDB_FIRST_DUP, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates in reverse over all duplicate records for the current key.
        /// </summary>
        public ValuesIterator ValuesReverse => new ValuesIterator(this, DbCursorOp.MDB_LAST_DUP, DbCursorOp.MDB_PREV_DUP);

        #endregion

        #region Nested types

        public ref struct ItemsFromEntryIterator
        {
            readonly MultiValueCursor cursor;
            readonly KeyDataPair entry;
            readonly DbCursorOp keyOp;
            readonly DbCursorOp nextOp;

            public ItemsFromEntryIterator(MultiValueCursor cursor, in KeyDataPair entry, DbCursorOp keyOp, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.entry = entry;
                this.keyOp = keyOp;
                this.nextOp = nextOp;
            }

            public ItemsFromEntryEnumerator GetEnumerator() => new ItemsFromEntryEnumerator(cursor, entry, keyOp, nextOp);
        }

        public ref struct ItemsFromEntryEnumerator
        {
            readonly MultiValueCursor cursor;
            readonly KeyDataPair entry;
            readonly DbCursorOp keyOp;
            readonly DbCursorOp nextOp;
            KeyDataPair current;
            bool isCurrent;
            bool isInitialized;

            public ItemsFromEntryEnumerator(MultiValueCursor cursor, in KeyDataPair entry, DbCursorOp keyOp, DbCursorOp nextOp) {
                this.cursor = cursor;
                this.entry = entry;
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
                    return isCurrent = cursor.Get(in entry, out current, keyOp);
                }
            }
        }

        public struct ValuesIterator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;

            public ValuesIterator(MultiValueCursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opFirst = opFirst;
                this.opNext = opNext;
            }

            public ValuesEnumerator GetEnumerator() => new ValuesEnumerator(cursor, opFirst, opNext);
        }

        public ref struct ValuesEnumerator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opFirst;
            readonly DbCursorOp opNext;
            ReadOnlySpan<byte> current;
            bool isCurrent;
            bool isInitialized;

            public ValuesEnumerator(MultiValueCursor cursor, DbCursorOp opFirst, DbCursorOp opNext) {
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

        #endregion

    }
}
