using System;
using System.Runtime.InteropServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Cursor for fixed-size multi-value database.
    /// </summary>
    public class FixedMultiValueCursor: MultiValueCursor
    {
        /// <summary>
        /// Size of fixed size data items (records).
        /// </summary>
        public readonly int DataSize;

        internal FixedMultiValueCursor(IntPtr cur, bool isReadOnly, int dataSize, Action<Cursor> disposed = null) : base(cur, isReadOnly, disposed) {
            this.DataSize = dataSize;
        }

        #region Read Operations

        /// <summary>
        /// Return up to a page of duplicate data items from current cursor position.
        /// Move cursor to prepare for <see cref="DbCursorOp.MDB_NEXT_MULTIPLE"/>.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <returns><c>true</c> if key/data pair exists, false otherwise.</returns>
        public bool GetMultiple(out ReadOnlySpan<byte> data) {
            return GetData(out data, DbCursorOp.MDB_GET_MULTIPLE);
        }

        /// <summary>
        /// Return up to a page of duplicate data items from next cursor position.
        /// Move cursor to prepare for <see cref="DbCursorOp.MDB_NEXT_MULTIPLE"/>.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <returns><c>true</c> if key/data pair exists, false otherwise.</returns>
        public bool GetNextMultiple(out ReadOnlySpan<byte> data) {
            return GetData(out data, DbCursorOp.MDB_NEXT_MULTIPLE);
        }

        /// <summary>
        /// Position at previous page and return up to a page of duplicate data items.
        /// Only for <see cref="DbLibConstants.MDB_DUPFIXED"/>.
        /// </summary>
        /// <param name="data"></param>
        /// <returns><c>true</c> if key/data pair exists, false otherwise.</returns>
        public bool GetPreviousMultiple(out ReadOnlySpan<byte> data) {
            return GetData(out data, DbCursorOp.MDB_PREV_MULTIPLE);
        }

        #endregion

        #region Update Operations

        /// <summary>
        /// Store multiple contiguous data elements by cursor.
        /// This is only valid for <see cref="FixedMultiValueDatabase"/> instances
        /// (they are opened with <see cref="MultiValueDatabaseOptions.DuplicatesFixed"/> flag).
        /// The cursor is positioned at the new item, or on failure usually near it.
        /// Note: Earlier documentation incorrectly said errors would leave the state of the cursor unchanged.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data">Fixed size records laid out in contiguous memory.</param>
        /// <param name="itemCount">On input, # of items to store, on output, number of items actually stored.</param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOption.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool PutMultiple(in ReadOnlySpan<byte> key, in ReadOnlySpan<byte> data, ref int itemCount) {
            if (DataSize * itemCount > data.Length)
                ErrorUtil.CheckRetCode(ErrorUtil.TooManyFixedItems);

            DbRetCode ret;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    var dbMultiData = stackalloc DbValue[2];
                    fixed (void* keyPtr = key)
                    fixed (void* firstDataPtr = data) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        dbMultiData[0] = new DbValue(firstDataPtr, DataSize);
                        dbMultiData[1] = new DbValue(null, itemCount);
                        ret = DbLib.mdb_cursor_put(handle, in dbKey, dbMultiData, DbLibConstants.MDB_MULTIPLE);
                        itemCount = (int)dbMultiData[1].Size;
                    }
                }
            }
            if (ret == DbRetCode.KEYEXIST)
                return false;
            ErrorUtil.CheckRetCode(ret);
            return true;
        }

        #endregion

        #region Enumeration

        /// <summary>
        /// Iterates over multiple duplicate records in sort order, from the current position on.
        /// Retrieves at most one page of records. For use in foreach loops.
        /// </summary>
        public ValuesIterator ForwardMultiple => new ValuesIterator(this, DbCursorOp.MDB_GET_MULTIPLE, DbCursorOp.MDB_NEXT_MULTIPLE);

        /// <summary>
        /// Iterates over multiple duplicate records in reverse sort order, from the current position on.
        /// Retrieves at most one page of records. For use in foreach loops.
        /// </summary>
        public ValuesIterator ReverseMultiple => new ValuesIterator(this, DbCursorOp.MDB_GET_MULTIPLE, DbCursorOp.MDB_PREV_MULTIPLE);

        #endregion
    }
}
