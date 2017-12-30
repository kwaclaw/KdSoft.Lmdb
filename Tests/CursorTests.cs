using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace KdSoft.Lmdb.Tests
{
    [Collection("Environment")]
    public class CursorTests
    {
        readonly EnvironmentFixture fixture;
        readonly ITestOutputHelper output;

        public CursorTests(EnvironmentFixture fixture, ITestOutputHelper output) {
            this.fixture = fixture;
            this.output = output;
        }

        //[Fact]
        //public void BasicIteration() {
        //    var cursor = new Cursor(IntPtr.Zero, null);
        //    foreach (var entry in cursor.ItemsForward) {
        //        entry.Key.CopyTo(new Span<byte>(new byte[255]));
        //        entry.Data.CopyTo(new Span<byte>(new byte[2048]));
        //    }
        //}
    }
}
