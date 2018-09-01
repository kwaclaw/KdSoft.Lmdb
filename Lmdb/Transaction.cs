using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// LMDB Transaction.
    /// </summary>
    public class Transaction: IDisposable
    {
        readonly Transaction parent;
        readonly Action<IntPtr> disposed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="txn">Native transaction handle.</param>
        /// <param name="parent">Parent transaction. Can be <c>null</c>.</param>
        /// <param name="disposed">Callback when transaction gets disposed.</param>
        internal protected Transaction(IntPtr txn, Transaction parent, Action<IntPtr> disposed) {
            this.txn = txn;
            this.parent = parent;
            this.disposed = disposed;
        }

        /// <summary>
        /// Parent transaction.
        /// </summary>
        public Transaction Parent => this.parent;

        #region Unmanaged Resources

        /// <summary>
        /// Resource lock object.
        /// </summary>
        protected readonly object rscLock = new object();

        // access to properly aligned types of size "native int" is atomic!
        volatile IntPtr txn;
        internal IntPtr Handle => txn;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            DbLib.mdb_txn_abort(this.txn);
        }

        #endregion

        /// <summary>
        /// Environment that owns the transaction.
        /// </summary>
        public Environment Environment {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    var env = DbLib.mdb_txn_env(handle);
                    var gcHandle = (GCHandle)DbLib.mdb_env_get_userctx(env);
                    return (Environment)gcHandle.Target;
                }
            }
        }

        /// <summary>
        /// Transaction Id.
        /// </summary>
        public IntPtr Id {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return DbLib.mdb_txn_id(handle);
                }
            }
        }

        /// <summary>
        /// Abandon all the operations of the transaction instead of saving them. Same as Dispose().
        /// The transaction handle is freed.It and its cursors must not be used again after this call, except with mdb_cursor_renew().
        /// Note: Earlier documentation incorrectly said all cursors would be freed.Only write-transactions free cursors.
        /// </summary>
        public void Abort() {
            Dispose();
        }

        /// <summary>
        /// Called at end of commit, while still under a resource lock.
        /// At this point the transaction is already closed/disposed.
        /// </summary>
        protected virtual void Committed() { }

        /// <summary>
        /// Commit all the operations of a transaction into the database.
        /// The transaction handle is freed.It and its cursors must not be used again after this call, except with mdb_cursor_renew().
        /// Note: Earlier documentation incorrectly said all cursors would be freed.Only write-transactions free cursors.
        /// </summary>
        public void Commit() {
            lock (rscLock) {
                var handle = CheckDisposed();
                ReleaseManagedResources(true);
                var ret = DbLib.mdb_txn_commit(handle);
                SetDisposed();
                ErrorUtil.CheckRetCode(ret);
                Committed();
            }
        }

        #region Cursors

        readonly object cursorLock = new object();
        readonly HashSet<Cursor> cursors = new HashSet<Cursor>();

        internal bool AddCursor(Cursor cursor) {
            lock (cursorLock) {
                cursor.Disposed = CursorDisposed;
                return cursors.Add(cursor);
            }
        }

        void CursorDisposed(Cursor cursor) {
            lock (cursorLock) {
                cursors.Remove(cursor);
            }
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Returns Transaction handle.
        /// Throws if Transaction handle is already closed/disposed of.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            IntPtr result = this.txn;
            if (result == IntPtr.Zero)
                throw new ObjectDisposedException(this.GetType().Name);
            return result;
        }

        // must only be called once - this is not checked!!!
        void SetDisposed() {
            var txnId = DbLib.mdb_txn_id(txn);
            txn = IntPtr.Zero;
            disposed?.Invoke(txnId);
        }

        /// <summary>
        /// Returns if Transaction handle is closed/disposed.
        /// </summary>
        public bool IsDisposed {
            get { return txn == IntPtr.Zero; }
        }

        /// <summary>
        /// Releases/closes managed resources - like cursors - owned by the transaction. Thread-safe.
        /// Part of Dispose() pattern.
        /// </summary>
        /// <param name="forCommit"><c>true</c> if transaction is closed/disposed due to a commit, <c>false</c> otherwise.</param>
        protected virtual void ReleaseManagedResources(bool forCommit = false) {
            // cursors must not be used after the owning transaction gets disposed
            lock (cursorLock) {
                foreach (var cursor in cursors) {
                    // cursors owned by read-only transactions must be closed explicitly
                    if (cursor.IsReadOnly)
                        cursor.Close();
                    // cursors owned by write transactions are closed by the transaction
                    else
                        cursor.ClearHandle();
                }
            }
        }

        /// <summary>
        /// Thread-safe cleanup of cursor references.
        /// </summary>
        protected virtual void Cleanup() {
            lock (cursorLock) {
                cursors.Clear();
            }
        }

        /// <summary>
        /// Implementation of Dispose() pattern.
        /// </summary>
        /// <param name="disposing"><c>true</c> if explicity disposing (finalizer not run), <c>false</c> if disposed from finalizer.</param>
        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (txn == IntPtr.Zero)  // already disposed
                    return;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { /* */ }
                finally {
                    if (disposing) {
                        // dispose managed state (managed objects)
                        ReleaseManagedResources();
                    }
                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    ReleaseUnmanagedResources();
                    if (disposing)
                        SetDisposed();

                    Cleanup();
                }
            }
        }

        /// <summary>
        /// Finalizer. Releaases unmanaged resources.
        /// </summary>
        ~Transaction() {
            Dispose(false);
        }

        /// <summary>
        /// Abandon all the operations of the transaction instead of saving them. Same as Abort().
        /// The transaction handle is freed.It and its cursors must not be used again after this call, except with mdb_cursor_renew().
        /// Note: Earlier documentation incorrectly said all cursors would be freed.Only write-transactions free cursors.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
