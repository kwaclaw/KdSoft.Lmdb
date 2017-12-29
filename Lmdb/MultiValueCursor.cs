using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class MultiValueCursor: Cursor
    {
        internal MultiValueCursor(IntPtr cursor, Action<Cursor> disposed): base(cursor, disposed) {
            //
        }

        /// <summary>
        /// Retrieve by multi-value cursor.
        /// This function retrieves key/data pairs from the database.
        /// The address and length of the key are returned in the object to which key refers,
        /// and the address and length of the data are returned in the object to which data refers.
        /// See <see cref="Database.Get(Transaction, ReadOnlySpan{byte})"/> for restrictions on using the output values.
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if retrieved without error, <c>false</c> if not found.</returns>
        public bool Get(Span<byte> key, ReadOnlySpan<byte> data, MultiValueCursorOperation operation) {
            return Get(key, data, (DbCursorOp)operation);
        }

        /// <summary>
        /// Store by cursor.
        /// This function stores key/data pairs into the database.
        /// The cursor is positioned at the new item, or on failure usually near it.
        /// Note: Earlier documentation incorrectly said errors would leave the state of the cursor unchanged.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="mvOptions"></param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="CursorPutOptions.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool Put(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, MultiValueCursorPutOptions mvOptions) {
            return PutInternal(key, data, unchecked((uint)mvOptions));
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
                    DbValue* dataPtr = stackalloc DbValue[2];
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key))
                    fixed (void* firstDataPtr = &MemoryMarshal.GetReference(data)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        dataPtr[0] = new DbValue(firstDataPtr, firstDataSize);
                        dataPtr[1] = new DbValue(null, itemCount);
                        ret = Lib.mdb_cursor_put(handle, ref dbKey, ref dataPtr[0], LibConstants.MDB_MULTIPLE);
                        itemCount = (int)dataPtr[1].Size;
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
    }
}
