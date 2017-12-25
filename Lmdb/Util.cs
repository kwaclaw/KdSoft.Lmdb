using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace KdSoft.Lmdb
{
    public static class Util
    {
        // keep in sync with user defined codes in DbRetCode
        static string[] dotNetStr = {
          "Key generator callback failed.",
          "Append record number callback failed.",
          "Duplicate comparison callback failed.",
          "BTree key comparison callback failed.",
          "BTree prefix comparison callback failed.",
          "Hash function callback failed.",
          "Feedback callback failed.",
          "Panic callback failed.",
          "Application recovery callback failed.",
          "Verify callback failed.",
          "Replication send callback failed.",
          "Cache page-in callback failed.",
          "Cache page-out callback failed.",
          "Event notification failed.",
          "IsAlive callback failed.",
          "ThreadId callback failed.",
          "ThreadIdString callback failed."
        };

        public static string DotNetStr(DbRetCode ret) {
            if (ret > DotNetLowError && ret <= (DotNetLowError + dotNetStr.Length))
                return dotNetStr[(int)ret - (int)DotNetLowError - 1];
            else
                return UnknownStr(ret);
        }

        public static string UnknownStr(DbRetCode ret) {
            return string.Format("Unknown error code: {0}.", ret);
        }

        public const DbRetCode MdbLowError = (DbRetCode)(-30800);
        public const DbRetCode DotNetLowError = (DbRetCode)(-41000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckRetCode(DbRetCode ret) {
            if (ret != DbRetCode.SUCCESS) {
                string errStr;
                if (ret > MdbLowError)
                    errStr = Lib.mdb_strerror(ret);
                else if (ret > DotNetLowError)
                    errStr = DotNetStr(ret);
                else
                    errStr = UnknownStr(ret);
                throw new LmdbException(ret, errStr);
            }
        }
    }
}
