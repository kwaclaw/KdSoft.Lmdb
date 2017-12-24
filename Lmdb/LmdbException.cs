using System;

namespace KdSoft.Lmdb
{
    /// <summary>Exception class representing errors returned from MLDB API calls,
    /// or inappropriate use of the .NET bindings.</summary>
    public class LmdbException: Exception
    {
        readonly DbRetCode errorCode;

        public LmdbException() { }

        public LmdbException(string message) : base(message) { }

        public LmdbException(string message, Exception e) : base(message, e) { }

        public LmdbException(DbRetCode errorCode) {
            this.errorCode = errorCode;
        }

        public LmdbException(DbRetCode errorCode, string message) : base(message) {
            this.errorCode = errorCode;
        }

        public DbRetCode ErrorCode {
            get { return errorCode; }
        }
    }
}
