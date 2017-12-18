using System;
using System.Collections.Generic;
using System.Text;

namespace KdSoft.Lmdb
{
    public class Database: IDisposable
    {
        volatile uint dbi;
        volatile IntPtr env;

        public readonly Environment Environment;
        public string Name { get; }

        internal Database(IntPtr txn, string name, MdbOptions flags) {
            this.env = Lib.mdb_txn_env(txn);
            this.Name = name;
            uint dbi;
            var retCode = Lib.mdb_dbi_open(txn, name, flags, out dbi);
        }

        #region Unmanaged Resources


        void ReleaseUnmanagedResources() {
            uint dbHandle = this.dbi;
            if (dbi == 0)
                return;

            // LmdbApi.mdb_env_close() could be a lengthy call, so we call Disposed() first, and the
            // CER ensures that we reach LmdbApi.mdb_env_close() without external interruption.
            // This is OK because one must not use the handle after LmdbApi.mdb_env_close() was called
            //Disposed();
            Lib.mdb_dbi_close(Environment.env, dbHandle);
        }

        #endregion


        #region IDisposable Support
        bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Database() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
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
