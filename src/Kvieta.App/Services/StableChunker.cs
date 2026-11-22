using System;
using System.IO;

namespace Kvieta.App.Services;

// Content-defined boundaries re-synchronize after insertions in an installer.
// This implementation is shared by the release signer and the download cache.
public static class StableChunker
{
    public const int Minimum = 32 * 1024;
    public const int Maximum = 256 * 1024;
    private static readonly uint[] Gear = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        uint state = 0x91E10DA5;
        for (int i = 0; i < table.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            table[i] = state;
        }
        return table;
    }

    public static int ReadChunk(Stream input, byte[] buffer)
    {
        int available = input.ReadAtLeast(buffer.AsSpan(0, Maximum), Maximum, throwOnEndOfStream: false);
        uint rolling = 0;
        for (int i = Minimum; i < available; i++)
        {
            rolling = unchecked((rolling << 1) + Gear[buffer[i]]);
            if ((rolling & 0xFFFF) != 0) continue;
            int count = i + 1;
            input.Seek(count - available, SeekOrigin.Current);
            return count;
        }
        return available;
    }
}
