using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// LMDB Database that allows duplicate (by key) records - multi-value database.
    public class MultiValueDatabase: Database
    {
        internal MultiValueDatabase(uint dbi, IntPtr env, string name, Action<Database> disposed, MultiValueDatabaseConfiguration config) :
            base(dbi, env, name, disposed, config) {
            //
        }

        /// <summary>
        /// Retrieve the multi-value options for the database.
        /// </summary>
        public MultiValueDatabaseOptions GetMultiValueOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => DbLib.mdb_dbi_flags(transaction.Handle, handle, out value));
            return unchecked((MultiValueDatabaseOptions)opts);
        }

        /// <summary>
        /// Store items into a database.
        /// This function stores key/data pairs in the database. The default behavior is to enter the new key/data pair,
        /// replacing any previously existing key if duplicates are disallowed, or adding a duplicate data item
        /// if duplicates are allowed(MDB_DUPSORT).
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <param name="mvOptions"></param>
        /// <returns><c>true</c> if inserted without error, <c>false</c> if <see cref="PutOptions.NoOverwrite"/>
        /// was specified and the key already exists.</returns>
        public bool Put(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, PutOptions options, MultiValuePutOptions mvOptions) {
            uint opts = unchecked((uint)options | (uint)mvOptions);
            return PutInternal(transaction, key, data, opts);
        }

        /// <summary>
        /// Delete items from a database. This function removes key/data pairs from the database.
        /// Only the matching data item will be deleted.
        /// This function will return MDB_NOTFOUND if the specified key/data pair is not in the database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public bool Delete(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data) {
            lock (rscLock) {
                var handle = CheckDisposed();
                DbRetCode ret;
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key))
                    fixed (void* dataPtr = &MemoryMarshal.GetReference(data)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        var dbData = new DbValue(dataPtr, data.Length);
                        ret = DbLib.mdb_del(transaction.Handle, handle, ref dbKey, ref dbData);
                    }
                }
                if (ret == DbRetCode.NOTFOUND)
                    return false;
                ErrorUtil.CheckRetCode(ret);
                return true;
            }
        }

        /// <summary>
        /// Compare two data items according to a particular database.
        /// This returns a comparison as if the two data items were keys in the specified database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>&lt; 0 if x &lt; y, 0 if x == y, &gt; 0 if x &gt; y</returns>
        public int DupCompare(Transaction transaction, ReadOnlySpan<byte> x, ReadOnlySpan<byte> y) {
            int result;
            lock (rscLock) {
                var handle = CheckDisposed();
                unsafe {
                    fixed (void* xPtr = &MemoryMarshal.GetReference(x))
                    fixed (void* yPtr = &MemoryMarshal.GetReference(y)) {
                        var dbx = new DbValue(xPtr, x.Length);
                        var dby = new DbValue(yPtr, y.Length);
                        result = DbLib.mdb_dcmp(transaction.Handle, handle, ref dbx, ref dby);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Create a multi-value cursor. A cursor is associated with a specific transaction and database.
        /// A cursor cannot be used when its database handle is closed. Nor when its transaction has ended, except with mdb_cursor_renew().
        /// It can be discarded with mdb_cursor_close(). A cursor in a write-transaction can be closed before its transaction ends,
        /// and will otherwise be closed when its transaction ends. A cursor in a read-only transaction must be closed explicitly,
        /// before or after its transaction ends. It can be reused with mdb_cursor_renew() before finally closing it.
        /// Note: Earlier documentation said that cursors in every transaction were closed when the transaction committed or aborted.
        /// Note: If one does not close database handles (leaving that to the environment), then one does not have to worry about
        ///       closing a cursor before closing its database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>Instance of <see cref="MultiValueCursor"/>.</returns>
        public override Cursor OpenCursor(Transaction transaction) {
            return OpenMultiValueCursor(transaction);
        }

        /// <summary>
        /// Create a multi-value cursor. A cursor is associated with a specific transaction and database.
        /// A cursor cannot be used when its database handle is closed. Nor when its transaction has ended, except with mdb_cursor_renew().
        /// It can be discarded with mdb_cursor_close(). A cursor in a write-transaction can be closed before its transaction ends,
        /// and will otherwise be closed when its transaction ends. A cursor in a read-only transaction must be closed explicitly,
        /// before or after its transaction ends. It can be reused with mdb_cursor_renew() before finally closing it.
        /// Note: Earlier documentation said that cursors in every transaction were closed when the transaction committed or aborted.
        /// Note: If one does not close database handles (leaving that to the environment), then one does not have to worry about
        ///       closing a cursor before closing its database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>Instance of <see cref="MultiValueCursor"/>.</returns>
        public MultiValueCursor OpenMultiValueCursor(Transaction transaction) {
            var cursorHandle = OpenCursorHandle(transaction);
            var cursor = new MultiValueCursor(cursorHandle, transaction is ReadOnlyTransaction);
            transaction.AddCursor(cursor);
            return cursor;
        }
    }
}
