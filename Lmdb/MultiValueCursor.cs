﻿using System;
using System.Collections;
using System.Runtime.CompilerServices;
using KdSoft.Lmdb.Interop;

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

        /// <summary>
        /// Move cursor to first data position of next key and get the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if next key exists, false otherwise.</returns>
        public bool GetNextKey(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_NEXT_NODUP);
        }

        /// <summary>
        /// Move cursor to last data position of previous key and get the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if previous key exists, false otherwise.</returns>
        public bool GetPreviousKey(out ReadOnlySpan<byte> key) {
            return GetKey(out key, DbCursorOp.MDB_PREV_NODUP);
        }

        /// <summary>
        /// Move cursor to first data position of next key and get the record (key and data).
        /// </summary>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if next key exists, false otherwise.</returns>
        public bool GetNextByKey(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_NEXT_NODUP);
        }

        /// <summary>
        /// Move cursor to last data position of previous key and get the record (key and data).
        /// </summary>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if previous key exists, false otherwise.</returns>
        public bool GetPreviousByKey(out KeyDataPair entry) {
            return Get(out entry, DbCursorOp.MDB_PREV_NODUP);
        }

        /// <summary>
        /// Move cursor to position of key/data pair and get the record (key and data).
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if key/data pair exists, false otherwise.</returns>
        public bool GetAt(in KeyDataPair keyData, out KeyDataPair entry) {
            return Get(in keyData, out entry, DbCursorOp.MDB_GET_BOTH);
        }

        /// <summary>
        /// Move cursor to key and nearest data position. key must match, data position must
        /// be greater than or equal to specified data. Then get the record (key and data).
        /// </summary>
        /// <param name="keyData"></param>
        /// <param name="entry"></param>
        /// <returns><c>true</c> if nearest key/data pair exists, false otherwise.</returns>
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

        /// <summary>
        /// Delete current key/data pair.
        /// This function deletes the key/data pair to which the cursor refers.
        /// </summary>
        public void Delete(MultiValueCursorDeleteOption mvOption, CursorDeleteOption option = CursorDeleteOption.None) {
            var opts = unchecked((uint)option | (uint)mvOption);
            var handle = CheckDisposed();
            var ret = DbLib.mdb_cursor_del(handle, opts);
            ErrorUtil.CheckRetCode(ret);
        }

        #endregion

        /// <summary>
        /// Return count of duplicates for current key.
        /// </summary>
        public IntPtr Count() {
            var handle = CheckDisposed();
            var ret = DbLib.mdb_cursor_count(handle, out IntPtr result);
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
        /// Iterates over keys in sort order, from the next position on.
        /// </summary>
        public NextItemsIterator ForwardFromNextByKey => new NextItemsIterator(this, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over keys in sort order, from the current position on.
        /// </summary>
        public ItemsIterator ForwardFromCurrentByKey => new ItemsIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_NEXT_NODUP);

        /// <summary>
        /// Iterates over all keys in reverse sort order. Retrieves each key's first duplicate record in duplicate sort order.
        /// One can iterate over the duplicate records separately in a nested loop with <see cref="ValuesForward"/>
        /// or <see cref="ValuesReverse"/>, or one can iterate including duplicate records with <see cref="Cursor.Reverse"/>.
        /// </summary>
        public ItemsIterator ReverseByKey => new ItemsIterator(this, DbCursorOp.MDB_LAST, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over keys in reverse sort order, from the previous position on.
        /// </summary>
        public NextItemsIterator ReverseFromNextByKey => new NextItemsIterator(this, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over keys in reverse sort order, from the current position on.
        /// </summary>
        public ItemsIterator ReverseFromCurrentByKey => new ItemsIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_PREV_NODUP);

        /// <summary>
        /// Iterates over all duplicate records for the current key.
        /// </summary>
        public ValuesIterator ValuesForward => new ValuesIterator(this, DbCursorOp.MDB_FIRST_DUP, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates over duplicate records for the current key, in duplicate sort order from the next position on.
        /// </summary>
        public ValuesNextIterator ValuesForwardFromNext => new ValuesNextIterator(this, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates over duplicate records for the current key, in duplicate sort order from the current position on.
        /// </summary>
        public ValuesIterator ValuesForwardFromCurrent => new ValuesIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_NEXT_DUP);

        /// <summary>
        /// Iterates over all duplicate records for the current key, in reverse duplicate sort order.
        /// </summary>
        public ValuesIterator ValuesReverse => new ValuesIterator(this, DbCursorOp.MDB_LAST_DUP, DbCursorOp.MDB_PREV_DUP);

        /// <summary>
        /// Iterates over duplicate records for the current key, in reverse duplicate sort order from the previous position on.
        /// </summary>
        public ValuesNextIterator ValuesReverseFromPrevious => new ValuesNextIterator(this, DbCursorOp.MDB_PREV_DUP);

        /// <summary>
        /// Iterates over duplicate records for the current key, in reverse duplicate sort order from the current position on.
        /// </summary>
        public ValuesIterator ValuesReverseFromCurrent => new ValuesIterator(this, DbCursorOp.MDB_GET_CURRENT, DbCursorOp.MDB_PREV_DUP);

        #endregion

        #region Nested types
#pragma warning disable CA1815 // Override equals and operator equals on value types
#pragma warning disable CA1034 // Nested types should not be visible

        /// <summary>
        /// Iterator class that iterates over all values in a key.
        /// Equivalent to, but not an implementation of, <see cref="IEnumerable"/>.
        /// Can be used in foreach statements as if it was an implementation of <see cref="IEnumerable"/>.
        /// </summary>
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

            /// <summary>Equivalent to <see cref="IEnumerable.GetEnumerator()"/>.</summary>
            public ValuesEnumerator GetEnumerator() => new ValuesEnumerator(cursor, opFirst, opNext);
        }

        /// <summary>
        /// Iterator class that iterates over the values forward from the current duplicate position.
        /// Equivalent to, but not an implementation of, <see cref="IEnumerable"/>.
        /// Can be used in foreach statements as if it was an implementation of <see cref="IEnumerable"/>.
        /// </summary>
        public struct ValuesNextIterator
        {
            readonly MultiValueCursor cursor;
            readonly DbCursorOp opNext;

            internal ValuesNextIterator(MultiValueCursor cursor, DbCursorOp opNext) {
                this.cursor = cursor;
                this.opNext = opNext;
            }

            /// <summary>Equivalent to <see cref="IEnumerable.GetEnumerator()"/>.</summary>
            public ValuesNextEnumerator GetEnumerator() => new ValuesNextEnumerator(cursor, opNext);
        }

        /// <summary>
        /// Enumerator class that iterates over all values in a key. See <see cref="ValuesIterator"/>.
        /// Equivalent to, but not an implementation of, <see cref="IEnumerator"/>.
        /// Will be be used in foreach statements as if it was an implementation of <see cref="IEnumerator"/>.
        /// </summary>
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

            /// <summary>Equivalent to <see cref="IEnumerator.Current"/>.</summary>
            public ReadOnlySpan<byte> Current {
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
                    return isCurrent = cursor.GetData(out current, opNext);
                else {
                    isInitialized = true;
                    return isCurrent = cursor.GetData(out current, opFirst);
                }
            }
        }

        /// <summary>
        /// Enumerator class that iterates over the values forward from the current duplicate position.
        /// See <see cref="ValuesNextIterator"/>. Equivalent to, but not an implementation of, <see cref="IEnumerator"/>.
        /// Will be be used in foreach statements as if it was an implementation of <see cref="IEnumerator"/>.
        /// </summary>
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

            /// <summary>Equivalent to <see cref="IEnumerator.Current"/>.</summary>
            public ReadOnlySpan<byte> Current {
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
                return isCurrent = cursor.GetData(out current, opNext);
            }
        }

#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1815 // Override equals and operator equals on value types
        #endregion
    }
}
