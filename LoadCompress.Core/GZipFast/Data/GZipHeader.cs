using System;
using System.Collections.Generic;
using System.IO;

namespace LoadCompress.Core.GZipFast.Data
{
    internal readonly ref struct GZipHeader
    {
        internal const byte MinimalSize = sizeof(uint) + sizeof(int) + sizeof(long);

        internal readonly int BlocksCount;

        internal readonly long BlockSize;

        internal GZipHeader(Span<GZipBlock> blocks, long blockSize)
        {
            BlockSize = blockSize;
            BlocksCount = blocks.Length;
            _blocks = blocks;
        }

        internal GZipBlock this[int i]
        {
            get => _blocks[i];
            set => _blocks[i] = value;
        }

        internal int GetSize() => MinimalSize + _blocks.Length * GZipBlock.SelfSize;

        internal int ToBytes(Span<byte> buffer)
        {
            var headerSize = GetSize();
            if (headerSize > buffer.Length)
                throw new InvalidOperationException(
                    $"Buffer size is too small: has {buffer.Length} bytes, but needs {headerSize}");

            BitConverter.TryWriteBytes(buffer.Slice(0, sizeof(uint)), MagicNumber);
            BitConverter.TryWriteBytes(buffer.Slice(4, sizeof(long)), BlockSize);
            BitConverter.TryWriteBytes(buffer.Slice(12, sizeof(int)), BlocksCount);
            var offset = 16;
            for (var i = 0; i < BlocksCount; i++, offset += GZipBlock.SelfSize)
            {
                _blocks[i].ToBytes(buffer.Slice(offset, GZipBlock.SelfSize));
            }

            return headerSize;
        }

        internal void MergeBlocks(List<GZipBlock> updatedBlocks)
        {
            if (_blocks.Length != updatedBlocks.Count)
                throw new ArgumentException(
                    $"Header has {_blocks.Length} blocks, but requested update of {updatedBlocks.Count} blocks");

            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = updatedBlocks[i];
            }
        }

        internal static GZipHeader Read(Stream source)
        {
            Span<byte> buffer = stackalloc byte[MinimalSize];

            var bytesRead = source.Read(buffer);

            if (MinimalSize > bytesRead)
                throw new InvalidOperationException("Wrong source format: too short");

            var marker = BitConverter.ToUInt32(buffer.Slice(0, sizeof(uint)));
            if (marker != MagicNumber)
                throw new InvalidOperationException("Wrong source format: could not find maker");

            var blocksSize = BitConverter.ToInt64(buffer.Slice(4, sizeof(long)));
            var blocksCount = BitConverter.ToInt32(buffer.Slice(12, sizeof(int)));

            var header = new GZipHeader(new Span<GZipBlock>(new GZipBlock[blocksCount]), blocksSize);

            buffer = stackalloc byte[GZipBlock.SelfSize];
            for (var i = 0; i < header.BlocksCount; i++)
            {
                bytesRead = source.Read(buffer.Slice(0, GZipBlock.SelfSize));
                header[i] = GZipBlock.FromBytes(buffer.Slice(0, bytesRead));
            }

            return header;
        }

        internal static GZipHeader CreateEmpty(int blocksCount, int blockSize)
        {
            Span<GZipBlock> blocks = new GZipBlock[blocksCount];
            return new GZipHeader(blocks, blockSize);
        }

        private const uint MagicNumber = 0xCAFED00D; // Dec 3405697037

        private readonly Span<GZipBlock> _blocks;
    }
}