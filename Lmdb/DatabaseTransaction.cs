using System;
using System.Collections.Generic;
using KdSoft.Lmdb.Interop;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// LMDB Transaction that allows creating or opening databases.
    /// Only one of these can be active in the environment at a time.
    /// </summary>
    public class DatabaseTransaction: Transaction
    {
        readonly Dictionary<string, Database> committedDatabases;
        readonly Action<Database> committedDisposed;
        readonly List<Database> newDatabases = new List<Database>();

        internal DatabaseTransaction(
            IntPtr txn,
            Transaction parent,
            Action<IntPtr> closed,
            Dictionary<string, Database> committedDatabases,
            Action<Database> committedDisposed
        ) : base(txn, parent, closed) {
            this.committedDatabases = committedDatabases;
            this.committedDisposed = committedDisposed;
        }

        readonly object dbLock = new object();

#if NETSTANDARD2_0
        (uint dbi, IntPtr handle, IntPtr env) OpenDatabaseInternal(string name, uint options, CompareHolder compareHolder) {
#else
        unsafe (uint dbi, IntPtr handle, IntPtr env) OpenDatabaseInternal(string name, uint options, CompareHolder compareHolder) {
#endif
            // we won't allow database name conflicts, even before the new database is committed
            if (committedDatabases.ContainsKey(name))
                throw new LmdbException($"Database '{name}' exists already.");
            var handle = CheckDisposed();
            var ret = DbLib.mdb_dbi_open(handle, name, options, out uint dbi);
            ErrorUtil.CheckRetCode(ret);

            var env = DbLib.mdb_txn_env(handle);

#if NETSTANDARD2_0
            if (compareHolder.LibCompare != null) {
                ret = DbLib.mdb_set_compare(handle, dbi, compareHolder.LibCompare);
            }
#else
            if (compareHolder.UnsafeCompare != null) {
                ret = DbLib.mdb_set_compare(handle, dbi, compareHolder.UnsafeCompare);
            }
            else if (compareHolder.LibCompare != null) {
                ret = DbLib.mdb_set_compare(handle, dbi, compareHolder.LibCompare);
            }
#endif
            
            if (ret != DbRetCode.SUCCESS)
                DbLib.mdb_dbi_close(env, dbi);
            ErrorUtil.CheckRetCode(ret);

            return (dbi, handle, env);
        }

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
        /// <param name="name">Database name. Can be <c>null</c> for the default database.</param>
        /// <param name="config">Database configuration instance.</param>
        /// <returns>Database instance open for use.</returns>
        public Database OpenDatabase(string name, DatabaseConfiguration config) {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            lock (dbLock) {
                var (dbi, handle, env) = OpenDatabaseInternal(name, unchecked((uint)config.Options), config.CompareHolder);
                var result = new Database(dbi, env, name, NewDatabaseDisposed, config);
                newDatabases.Add(result);
                return result;
            }
        }

        /// <summary>
        /// Open a multi-value database in the environment.
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
        /// <param name="name">Database name. Can be <c>null</c> for the default database.</param>
        /// <param name="config">Database configuration instance.</param>
        /// <returns><c>MultiValueDatabase</c> instance.</returns>
#if NETSTANDARD2_0
        public MultiValueDatabase OpenMultiValueDatabase(string name, MultiValueDatabaseConfiguration config) {
#else
        public unsafe MultiValueDatabase OpenMultiValueDatabase(string name, MultiValueDatabaseConfiguration config) {
#endif
            uint options = unchecked((uint)config.Options | (uint)config.DupOptions | DbLibConstants.MDB_DUPSORT /* to make sure */);
            lock (dbLock) {
                var (dbi, handle, env) = OpenDatabaseInternal(name, options, config.CompareHolder);
                DbRetCode ret = DbRetCode.SUCCESS;
#if NETSTANDARD2_0
                if (config.DupCompareHolder.LibCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.LibCompare);
                }
#else
                if (config.DupCompareHolder.UnsafeCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.UnsafeCompare);
                }
                else if (config.DupCompareHolder.LibCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.LibCompare);
                }
#endif

                if (ret != DbRetCode.SUCCESS)
                    DbLib.mdb_dbi_close(env, dbi);
                ErrorUtil.CheckRetCode(ret);

                var result = new MultiValueDatabase(dbi, env, name, NewDatabaseDisposed, config);
                newDatabases.Add(result);
                return result;
            }
        }

        /// <summary>
        /// Open a fixed multi-value database in the environment. Each value must have the same size.
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
        /// <param name="name">Database name. Can be <c>null</c> for the default database.</param>
        /// <param name="config">Database configuration instance.</param>
        /// <returns><c>FixedMultiValueDatabase</c> instance.</returns>
#if NETSTANDARD2_0
        public FixedMultiValueDatabase OpenFixedMultiValueDatabase(string name, FixedMultiValueDatabaseConfiguration config) {
#else
        public unsafe FixedMultiValueDatabase OpenFixedMultiValueDatabase(string name, FixedMultiValueDatabaseConfiguration config) {
#endif
            uint options = unchecked((uint)config.Options | (uint)config.DupOptions | DbLibConstants.MDB_DUPSORT | DbLibConstants.MDB_DUPFIXED /* to make sure */);
            lock (dbLock) {
                var (dbi, handle, env) = OpenDatabaseInternal(name, options, config.CompareHolder);
                DbRetCode ret = DbRetCode.SUCCESS;
#if NETSTANDARD2_0
                if (config.DupCompareHolder.LibCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.LibCompare);
                }
#else
                if (config.DupCompareHolder.UnsafeCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.UnsafeCompare);
                }
                else if (config.DupCompareHolder.LibCompare != null) {
                    ret = DbLib.mdb_set_dupsort(handle, dbi, config.DupCompareHolder.LibCompare);
                }
#endif

                if (ret != DbRetCode.SUCCESS)
                    DbLib.mdb_dbi_close(env, dbi);
                ErrorUtil.CheckRetCode(ret);

                var result = new FixedMultiValueDatabase(dbi, env, name, NewDatabaseDisposed, config);
                newDatabases.Add(result);
                return result;
            }
        }

        void NewDatabaseDisposed(Database db) {
            lock (dbLock) {
                newDatabases.Remove(db);
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override void ReleaseManagedResources(bool forCommit = false) {
            base.ReleaseManagedResources(forCommit);
            if (!forCommit) {
                lock (dbLock) {
                    foreach (var newDb in newDatabases) {
                        newDb.ClearHandle();
                    }
                    newDatabases.Clear();
                }
            }
        }
    }
}
