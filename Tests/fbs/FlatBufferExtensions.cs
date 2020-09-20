using System;
using Google.FlatBuffers;

namespace KdSoft.Lmdb.Tests.fbs
{
    public static class FlatBufferExtensions
    {
        public static ReadOnlySpan<byte> ToSpan(this ByteBuffer byteBuffer) {
            return byteBuffer.ToSpan(byteBuffer.Position, byteBuffer.Length - byteBuffer.Position);
        }
    }
}
