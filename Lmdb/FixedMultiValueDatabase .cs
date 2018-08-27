using System;

namespace KdSoft.Lmdb
{
    /// LMDB Database that allows duplicate (by key) records of fixed size - fixed multi-value database.
    public class FixedMultiValueDatabase: MultiValueDatabase
    {
        internal FixedMultiValueDatabase(uint dbi, IntPtr env, string name, Action<Database> disposed, FixedMultiValueDatabaseConfiguration config) :
            base(dbi, env, name, disposed, config) {
            //
        }

        /// <summary>
        /// Create a fixed multi-value cursor. A cursor is associated with a specific transaction and database.
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
            return OpenFixedMultiValueCursor(transaction);
        }

        /// <summary>
        /// Create a fixed multi-value cursor. A cursor is associated with a specific transaction and database.
        /// A cursor cannot be used when its database handle is closed. Nor when its transaction has ended, except with mdb_cursor_renew().
        /// It can be discarded with mdb_cursor_close(). A cursor in a write-transaction can be closed before its transaction ends,
        /// and will otherwise be closed when its transaction ends. A cursor in a read-only transaction must be closed explicitly,
        /// before or after its transaction ends. It can be reused with mdb_cursor_renew() before finally closing it.
        /// Note: Earlier documentation said that cursors in every transaction were closed when the transaction committed or aborted.
        /// Note: If one does not close database handles (leaving that to the environment), then one does not have to worry about
        ///       closing a cursor before closing its database.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns>Instance of <see cref="FixedMultiValueCursor"/>.</returns>
        public FixedMultiValueCursor OpenFixedMultiValueCursor(Transaction transaction) {
            var cursorHandle = OpenCursorHandle(transaction);
            int dataSize = ((FixedMultiValueDatabaseConfiguration)Config).FixedDataSize;
            var cursor = new FixedMultiValueCursor(cursorHandle, transaction is ReadOnlyTransaction, dataSize);
            transaction.AddCursor(cursor);
            return cursor;
        }
    }
}
