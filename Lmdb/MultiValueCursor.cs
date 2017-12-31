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

        public bool GetAt(in KeyDataPair keyData, out KeyDataPair entry) {
            return Get(in keyData, out entry, DbCursorOp.MDB_GET_BOTH);
        }

        public bool GetNearest(in KeyDataPair keyData, out KeyDataPair entry) {
            return Get(in keyData, out entry, DbCursorOp.MDB_GET_BOTH_RANGE);
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
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOptions.NoOverwrite"/>
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
                        ret = Lib.mdb_cursor_put(handle, ref dbKey, &dbMultiData.Val1, LibConstants.MDB_MULTIPLE);
                        itemCount = (int)dbMultiData.Val2.Size;
                    }
                }
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            Util.CheckRetCode(ret);
            return true;
        }

        /// <summary>
        /// Delete current key/data pair.
        /// This function deletes the key/data pair to which the cursor refers.
        /// </summary>
        public void Delete(MultiValueCursorDeleteOptions mvOptions, CursorDeleteOptions options = CursorDeleteOptions.None) {
            DbRetCode ret;
            var opts = unchecked((uint)options | (uint)mvOptions);
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = Lib.mdb_cursor_del(handle, opts);
            }
            Util.CheckRetCode(ret);
        }

        /// <summary>
        /// Return count of duplicates for current key.
        /// </summary>
        public IntPtr Count() {
            DbRetCode ret;
            IntPtr result;
            lock (rscLock) {
                var handle = CheckDisposed();
                ret = Lib.mdb_cursor_count(handle, out result);
            }
            Util.CheckRetCode(ret);
            return result;
        }

        #region Enumeration

        public ValuesIterator ValuesForward => new ValuesIterator(this, DbCursorOp.MDB_FIRST_DUP, DbCursorOp.MDB_NEXT_DUP);
        public ValuesIterator ValuesReverse => new ValuesIterator(this, DbCursorOp.MDB_LAST_DUP, DbCursorOp.MDB_PREV_DUP);

        #endregion

        #region Nested types

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
