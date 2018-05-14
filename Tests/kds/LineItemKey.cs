using System;
using KdSoft.Serialization;

namespace KdSoft.Lmdb.Tests.kds
{
    public class LineItemKey
    {
        public string ProdCode { get; set; }
        public int OrderId { get; set; }

        public override bool Equals(object obj) {
            return Equals(obj as LineItemKey);
        }

        public bool Equals(LineItemKey other) {
            return string.Equals(this.ProdCode, other.ProdCode, StringComparison.Ordinal) && this.OrderId == other.OrderId;
        }

        public override int GetHashCode() {
            return (ProdCode?.GetHashCode(StringComparison.Ordinal) ?? 0) ^ OrderId.GetHashCode();
        }
    }

    public class LineItemKeyField: ReferenceField<LineItemKey>
    {
        public LineItemKeyField(Formatter fmt, bool isDefault) : base(fmt, isDefault) { }

        protected override void DeserializeMembers(ReadOnlySpan<byte> source, LineItemKey instance) {
            instance.OrderId = Fmt.DeserializeStructDefault<int>(source);
            instance.ProdCode = Fmt.DeserializeObject<string>(source);
        }

        protected override void DeserializeInstance(ReadOnlySpan<byte> source, ref LineItemKey instance) {
            if (instance == null)
                instance = new LineItemKey();
        }

        protected override void SerializeValue(Span<byte> target, LineItemKey value) {
            Fmt.SerializeStruct(target, value.OrderId);
            Fmt.SerializeObject(target, value.ProdCode);
        }

        protected override void SkipValue(ReadOnlySpan<byte> source) {
            if (Fmt.Skip<int>(source))
                Fmt.Skip<string>(source);
        }
    }
}
