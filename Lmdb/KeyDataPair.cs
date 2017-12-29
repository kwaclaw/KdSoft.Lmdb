using System;

namespace KdSoft.Lmdb
{
    public readonly ref struct KeyDataPair
    {
        public ReadOnlySpan<byte> Key { get; }
        public ReadOnlySpan<byte> Data { get; }

        public KeyDataPair(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data) {
            this.Key = key;
            this.Data = data;
        }

        public KeyDataPair(ReadOnlySpan<byte> key) {
            this.Key = key;
            this.Data = default(ReadOnlySpan<byte>);
        }
    }
}
