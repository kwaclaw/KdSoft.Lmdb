using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class Database: IDisposable
    {
        public string Name { get; }

        // should we make the "creating/opening" transaction update the Database state once the transaction  is committed? Because:
        // The handle may only be closed once.
        // The database handle will be private to the current transaction until the transaction is successfully committed.
        // If the transaction is aborted the handle will be closed automatically.
        // After a successful commit the handle will reside in the shared environment, and may be used by other transactions.

        //TODO we first track a newly created database in its transaction, and when the transaction commits we pass it to the environment.
        //TODO figure out how we can prevent two transactions to open the same database name - maybe there should be a global
        //     databases dictionary (in environment), and the transaction only gets a list of "uncommitted" databases?
        //     or does the "uncommitted" state mean a database becomes only "real" once it is committed and before that
        //     we don't have to care for name clashes!


        internal Database(uint dbi, IntPtr env, string name, Action<Database> disposed) {
            this.dbi = dbi;
            this.env = env;
            this.Name = name;
            this.disposed = disposed;
        }

        Action<Database> disposed;
        internal Action<Database> Disposed {
            get => disposed;
            set => disposed = value;
        }

        public Environment Environment {
            get {
                lock (rscLock) {
                    var gcHandle = (GCHandle)Lib.mdb_env_get_userctx(env);
                    return (Environment)gcHandle.Target;
                }
            }
        }

        #region Unmanaged Resources

        protected readonly object rscLock = new object();

        readonly IntPtr env;

        volatile uint dbi;
        internal uint Handle => dbi;

        // must be executed under lock, and must not be called multiple times
        void ReleaseUnmanagedResources() {
            Lib.mdb_dbi_close(env, dbi);
        }

        #endregion

        #region IDisposable Support

        public const string disposedStr = "Database handle closed.";

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected uint CheckDisposed() {
            // avoid multiple volatile memory access
            uint result = this.dbi;
            if (result == 0)
                throw new ObjectDisposedException(disposedStr);
            return result;
        }

        internal void ClearHandle() {
            lock (rscLock) {
                dbi = 0;
            }
        }

        void SetDisposed() {
            dbi = 0;
            disposed?.Invoke(this);
        }

        public bool IsDisposed {
            get { return dbi == 0; }
        }

        protected virtual void Dispose(bool disposing) {
            lock (rscLock) {
                if (dbi == 0)  // already disposed
                    return;
                RuntimeHelpers.PrepareConstrainedRegions();
                try { /* */ }
                finally {
                    if (disposing) {
                        // dispose managed state (managed objects).
                    }
                    // free unmanaged resources (unmanaged objects) and override a finalizer below.
                    ReleaseUnmanagedResources();
                    if (disposing)
                        SetDisposed();
                }
            }
        }

        ~Database() {
            Dispose(false);
        }

        /// <summary>
        /// Close the environment and release the memory map. Same as Close().
        /// Only a single thread may call this function. All transactions, databases, and cursors must already be closed
        /// before calling this function. Attempts to use any such handles after calling this function will cause a SIGSEGV.
        /// The environment handle will be freed and must not be used again after this call.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region EqualityComparer

        public class EqualityComparer : IEqualityComparer<Database>, IComparer<Database>
        {
            readonly StringComparer comparer;

            public EqualityComparer(StringComparer comparer) {
                this.comparer = comparer;
            }

            public int Compare(Database x, Database y) {
                return comparer.Compare(x.Name, y.Name);
            }

            public bool Equals(Database x, Database y) {
                return comparer.Equals(x.Name, y.Name);
            }

            public int GetHashCode(Database obj) {
                return obj?.Name?.GetHashCode() ?? 0;
            }
        }

        #endregion
    }
}
