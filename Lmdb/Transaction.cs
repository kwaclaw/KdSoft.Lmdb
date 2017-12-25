using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class Transaction: IDisposable
    {
        readonly Transaction parent;
        readonly Action<IntPtr> disposed;

        internal protected Transaction(IntPtr txn, Transaction parent, Action<IntPtr> disposed) {
            this.txn = txn;
            this.parent = parent;
            this.disposed = disposed;
        }

        public Transaction Parent => this.parent;

        #region Unmanaged Resources

        protected readonly object rscLock = new object();

        // access to properly aligned types of size "native int" is atomic!
        volatile IntPtr txn;
        internal IntPtr Handle => txn;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            Lib.mdb_txn_abort(this.txn);
        }

        #endregion

        public Environment Environment {
            get {
                lock (rscLock) {
                    var env = Lib.mdb_txn_env(txn);
                    var gcHandle = (GCHandle)Lib.mdb_env_get_userctx(env);
                    return (Environment)gcHandle.Target;
                }
            }
        }

        public IntPtr Id {
            get {
                lock (rscLock) {
                    var handle = CheckDisposed();
                    return Lib.mdb_txn_id(handle);
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

        protected virtual void Committed() { }

        /// <summary>
        /// Commit all the operations of a transaction into the database.
        /// The transaction handle is freed.It and its cursors must not be used again after this call, except with mdb_cursor_renew().
        /// Note: Earlier documentation incorrectly said all cursors would be freed.Only write-transactions free cursors.
        /// </summary>
        public void Commit() {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = Lib.mdb_txn_commit(handle);
                SetDisposed();
                Util.CheckRetCode(ret);
                Committed();
            }
        }

        #region IDisposable Support

        public const string disposedStr = "Transaction handle closed.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IntPtr CheckDisposed() {
            // avoid multiple volatile memory access
            IntPtr result = this.txn;
            if (result == IntPtr.Zero)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        // must only be called once - this is not checked!!!
        void SetDisposed() {
            var txnId = Lib.mdb_txn_id(txn);
            txn = IntPtr.Zero;
            disposed?.Invoke(txnId);
        }

        public bool IsDisposed {
            get { return txn == IntPtr.Zero; }
        }

        protected virtual void ReleaseManagedResources() { }
        protected virtual void Cleanup() { }

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
