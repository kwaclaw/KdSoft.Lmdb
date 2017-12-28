using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KdSoft.Lmdb
{
    public class MultiValueDatabase: Database
    {
        internal MultiValueDatabase(uint dbi, IntPtr env, string name, Action<Database> disposed, MultiValueDatabase.Configuration config):
            base(dbi, env, name, disposed, config)
        {
            //
        }

        /// <summary>
        /// Retrieve the multi-value options for the database.
        /// </summary>
        public MultiValueDatabaseOptions GetMultiValueOptions(Transaction transaction) {
            uint opts = GetChecked((uint handle, out uint value) => Lib.mdb_dbi_flags(transaction.Handle, handle, out value));
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
        public void Put(Transaction transaction, ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, PutOptions options, MultiValuePutOptions mvOptions) {
            uint opts = unchecked((uint)options | (uint)mvOptions);
            PutInternal(transaction, key, data, opts);
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
                        ret = Lib.mdb_del(transaction.Handle, handle, ref dbKey, ref dbData);
                    }
                }
                if (ret == DbRetCode.NOTFOUND)
                    return false;
                Util.CheckRetCode(ret);
                return true;
            }
        }

        #region Nested Types

        /// <summary>
        /// Database configuration
        /// </summary>
        public new class Configuration: Database.Configuration
        {
            public MultiValueDatabaseOptions DupOptions { get; }
            public CompareFunction DupCompare { get; }
            internal Lib.CompareFunction LibDupCompare { get; }

            public Configuration(
                DatabaseOptions options,
                CompareFunction compare = null,
                MultiValueDatabaseOptions dupOptions = MultiValueDatabaseOptions.None,
                CompareFunction dupCompare = null
            ) : base(options, compare) {
                this.DupOptions = dupOptions;
                this.DupCompare = dupCompare;
                if (dupCompare != null)
                    this.LibDupCompare = DupCompareWrapper;
            }

            // no check for Compare == null
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int DupCompareWrapper(in DbValue x, in DbValue y) {
                return DupCompare(x.ToReadOnlySpan(), y.ToReadOnlySpan());
            }
        }

        #endregion
    }
}
