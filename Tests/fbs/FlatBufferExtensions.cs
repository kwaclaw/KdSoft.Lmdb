using System;
using Google.FlatBuffers;

namespace KdSoft.Lmdb.Tests.fbs
{
    public static class FlatBufferExtensions
    {
        public static ReadOnlySpan<byte> AsBytes(this ByteBuffer byteBuffer) {
            var dataSegment = byteBuffer.ToArraySegment(byteBuffer.Position, byteBuffer.Length - byteBuffer.Position);
            return new ReadOnlySpan<byte>(dataSegment.Array, dataSegment.Offset, dataSegment.Count);
        }
    }
}
