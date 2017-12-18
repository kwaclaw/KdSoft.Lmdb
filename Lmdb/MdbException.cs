using System;

namespace KdSoft.Lmdb
{
    /// <summary>Exception class representing errors returned from Berkeley DB API calls,
    /// or inappropriate use of the .NET bindings.</summary>
    public class MbException: Exception
    {
        readonly DbRetCode errorCode;

        public MbException() { }

        public MbException(string message) : base(message) { }

        public MbException(string message, Exception e) : base(message, e) { }

        public MbException(DbRetCode errorCode) {
            this.errorCode = errorCode;
        }

        public MbException(DbRetCode errorCode, string message) : base(message) {
            this.errorCode = errorCode;
        }

        public DbRetCode ErrorCode {
            get { return errorCode; }
        }
    }
}
