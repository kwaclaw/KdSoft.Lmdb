using System;
using System.Collections.Generic;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Transaction that allows creation/opening of databases. Only one of these can be active at a time.
    /// </summary>
    public class DOpenDbTransaction: AbstractTransaction
    {
        readonly Dictionary<string, Database> committedDatabases;
        readonly Action<Database> committedDisposed;
        readonly List<Database> newDatabases = new List<Database>();

        internal DOpenDbTransaction(
            IntPtr handle,
            Transaction parent,
            Action<UIntPtr> closed,
            Dictionary<string, Database> committedDatabases,
            Action<Database> committedDisposed
        ) : base(handle, parent, closed) {
            this.committedDatabases = committedDatabases;
            this.committedDisposed = committedDisposed;
        }

        readonly object dbLock = new object();

        /// <summary>
        /// Open a database in the environment.
        /// A database handle denotes the name and parameters of a database, independently of whether such a database exists.
        /// The database handle may be discarded by calling mdb_dbi_close().
        /// The old database handle is returned if the database was already open.
        /// The handle may only be closed once.
        /// The database handle will be private to the current transaction until the transaction is successfully committed.
        /// If the transaction is aborted the handle will be closed automatically.
        /// After a successful commit the handle will reside in the shared environment, and may be used by other transactions.
        /// This function must not be called from multiple concurrent transactions in the same process.
        /// A transaction that uses this function must finish (either commit or abort) before any other transaction in the process
        /// may use this function.
        /// To use named databases(with name != NULL), mdb_env_set_maxdbs() must be called before opening the environment.
        /// Database names are keys in the unnamed database, and may be read but not written.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Database OpenDatabase(string name, DatabaseOptions options) {
            lock (rscLock) {
                lock (dbLock) {
                    // we won't allow database name conflicts, even before the new database is committed
                    if (committedDatabases.ContainsKey(name))
                        throw new LmdbException($"Database '{name}' exists already.");
                    var txn = CheckDisposed();
                    var ret = Lib.mdb_dbi_open(txn, name, options, out uint dbi);
                    Util.CheckRetCode(ret);
                    var env = Lib.mdb_txn_env(Handle);
                    var result = new Database(dbi, env, name, NewDatabaseDisposed);
                    newDatabases.Add(result);
                    return result;
                }
            }
        }

        void NewDatabaseDisposed(Database db) {
            lock (dbLock) {
                newDatabases.Remove(db);
            }
        }

        // part of Commit()
        protected override void Committed() {
            base.Committed();
            lock (dbLock) {
                foreach (var newDb in newDatabases) {
                    committedDatabases.Add(newDb.Name, newDb);
                    newDb.Disposed = committedDisposed;
                }
                newDatabases.Clear();
            }

        }

        // part of Dispose() / Abort()
        protected override void ReleaseManagedResources() {
            base.ReleaseManagedResources();
            lock (dbLock) {
                foreach (var newDb in newDatabases)
                    newDb.ClearHandle();
            }
        }

        // part of Dispose() / Abort()
        protected override void Cleanup() {
            base.Cleanup();
            lock (dbLock) {
                newDatabases.Clear();
            }
        }
    }
}
