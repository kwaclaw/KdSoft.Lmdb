using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class Database: IDisposable
    {
        public string Name { get; }
        public Configuration Config { get; }

        internal Database(uint dbi, IntPtr env, string name, Action<Database> disposed, Configuration config) {
            this.dbi = dbi;
            this.env = env;
            this.Name = name;
            this.disposed = disposed;
            this.Config = config;
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

        #region Helpers

        [CLSCompliant(false)]
        protected void RunChecked(Func<uint, DbRetCode> libFunc) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = libFunc(handle);
                Util.CheckRetCode(ret);
            }
        }

        [CLSCompliant(false)]
        protected delegate R LibFunc<T, out R>(uint handle, out T result);

        [CLSCompliant(false)]
        protected T GetChecked<T>(LibFunc<T, DbRetCode> libFunc) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = libFunc(handle, out var result);
                Util.CheckRetCode(ret);
                return result;
            }
        }

        #endregion

        /// <summary>
        /// Same as <see cref="Dispose()"/>. Close a database handle. Normally unnecessary. Use with care:
        /// This call is not mutex protected. Handles should only be closed by a single thread, and only
        /// if no other threads are going to reference the database handle or one of its cursors any further.
        /// Do not close a handle if an existing transaction has modified its database.
        /// Doing so can cause misbehavior from database corruption to errors like MDB_BAD_VALSIZE(since the DB name is gone).
        /// Closing a database handle is not necessary, but lets mdb_dbi_open() reuse the handle value.
        /// Usually it's better to set a bigger mdb_env_set_maxdbs(), unless that value would be large.
        /// </summary>
        public void Close() {
            Dispose();
        }

        /// <summary>
        /// Empty a database.
        /// </summary>
        public void Truncate(Transaction transaction) {
            RunChecked((handle) => Lib.mdb_drop(transaction.Handle, handle, false));
        }

        /// <summary>
        /// Delete and close a database. See <see cref="Close()"/> for restrictions.
        /// </summary>
        public void Drop(Transaction transaction) {
            lock (rscLock) {
                var handle = CheckDisposed();
                var ret = Lib.mdb_drop(transaction.Handle, handle, true);
                Util.CheckRetCode(ret);
                SetDisposed();
            }
        }

        /// <summary>
        /// Retrieve statistics for the database.
        /// </summary>
        public Statistics GetStats(Transaction transaction) {
            return GetChecked((uint handle, out Statistics value) => Lib.mdb_stat(transaction.Handle, handle, out value));
        }

        /// <summary>
        /// Retrieve the options for the database.
        /// </summary>
        public DatabaseOptions GetOptions(Transaction transaction) {
            return GetChecked((uint handle, out DatabaseOptions value) => Lib.mdb_dbi_flags(transaction.Handle, handle, out value));
        }

        //public ReadOnlySpan<byte> Get(Transaction transaction, ReadOnlyMemory<byte> key) {
        //    ReadOnlySpan<byte> result;
        //    lock (rscLock) {
        //        var handle = CheckDisposed();
        //        DbRetCode ret;
        //        unsafe {
        //            using (var memHandle = key.Retain(true)) {
        //                var dbKey = new DbValue(memHandle.Pointer, key.Length);
        //                var dbValue = default(DbValue);
        //                ret = Lib.mdb_get(transaction.Handle, handle, ref dbKey, ref dbValue);
        //                result = dbValue.ToReadOnlySpan();
        //            }
        //        }
        //        Util.CheckRetCode(ret);
        //    }
        //    return result;
        //}

        public ReadOnlySpan<byte> Get(Transaction transaction, ReadOnlySpan<byte> key) {
            ReadOnlySpan<byte> result;
            lock (rscLock) {
                var handle = CheckDisposed();
                DbRetCode ret;
                unsafe {
                    fixed (void* bytePtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(bytePtr, key.Length);
                        var dbValue = default(DbValue);
                        ret = Lib.mdb_get(transaction.Handle, handle, ref dbKey, ref dbValue);
                        result = dbValue.ToReadOnlySpan();
                    }
                }
                Util.CheckRetCode(ret);
            }
            return result;
        }

        public void Put(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, PutOptions options) {
            lock (rscLock) {
                var handle = CheckDisposed();
                DbRetCode ret;
                unsafe {
                    fixed (void* keyPtr = &MemoryMarshal.GetReference(key)) {
                        var dbKey = new DbValue(keyPtr, key.Length);
                        fixed (void* dataPtr = &MemoryMarshal.GetReference(data)) {
                            var dbValue = new DbValue(dataPtr, data.Length);
                            ret = Lib.mdb_put(transaction.Handle, handle, ref dbKey, ref dbValue, options);
                        }
                    }
                }
                Util.CheckRetCode(ret);
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
        /// Same as <see cref="Close"/>. Close a database handle. Normally unnecessary. Use with care:
        /// This call is not mutex protected. Handles should only be closed by a single thread, and only
        /// if no other threads are going to reference the database handle or one of its cursors any further.
        /// Do not close a handle if an existing transaction has modified its database.
        /// Doing so can cause misbehavior from database corruption to errors like MDB_BAD_VALSIZE(since the DB name is gone).
        /// Closing a database handle is not necessary, but lets mdb_dbi_open() reuse the handle value.
        /// Usually it's better to set a bigger mdb_env_set_maxdbs(), unless that value would be large.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Database configuration
        /// </summary>
        public class Configuration
        {
            public DatabaseOptions Options { get; }
            public CompareFunction Compare { get; }
            public CompareFunction DupCompare { get; }

            public Configuration(DatabaseOptions options, CompareFunction compare = null, CompareFunction dupCompare = null) {
                this.Options = options;
                this.Compare = compare;
                this.DupCompare = dupCompare;
            }
        }

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
