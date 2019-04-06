using System;

namespace LoadCompress.Core.GZipFast.Data
{
    internal class GZipBlock: IEquatable<GZipBlock>
    {
        internal int Id;
        internal int Size;
        internal long OriginalSize;

        internal GZipBlock(int id, int size, long originalSize)
        {
            Id = id;
            Size = size;
            OriginalSize = originalSize;
        }

        internal const byte SelfSize = sizeof(int) + sizeof(int) + sizeof(long);

        internal int ToBytes(Span<byte> buffer)
        {
            if (SelfSize > buffer.Length)
                throw new InvalidOperationException(
                    $"Buffer size is too small: has {buffer.Length} bytes, but needs {SelfSize}");

            BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(int)), Id);
            BitConverter.TryWriteBytes(buffer.Slice(4, sizeof(int)), Size);
            BitConverter.TryWriteBytes(buffer.Slice(8, sizeof(long)), OriginalSize);
            return SelfSize;
        }

        internal static GZipBlock FromBytes(Span<byte> buffer)
        {
            if (SelfSize > buffer.Length)
                throw new InvalidOperationException(
                    $"Buffer size is too small: has {buffer.Length} bytes, but needs {SelfSize}");

            return new GZipBlock(BitConverter.ToInt32(buffer.Slice(0, sizeof(int))),
                BitConverter.ToInt32(buffer.Slice(4, sizeof(int))),
                BitConverter.ToInt64(buffer.Slice(8, sizeof(long))));
        }

        public bool Equals(GZipBlock other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Size == other.Size && OriginalSize == other.OriginalSize;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GZipBlock) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ OriginalSize.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Id)}:{Id}, {nameof(OriginalSize)}:{OriginalSize}, {nameof(Size)}:{Size}";
        }
    }
}