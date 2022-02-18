// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class ZenDirectory
{
    public const int DefaultMaxSnowflakeSize = 1024 * 1024 * 1024; // 1GB = 4MB x 256
    public const int HashSize = 8;

    public ZenDirectory()
    {
    }

    public ZenDirectory(uint directoryId, string path)
    {
        this.DirectoryId = directoryId;
        this.DirectoryPath = path;
    }

    private void GetSnowflake(bool exclusiveSnowflake, ref Snowflake? snowflake, out int offset)
    {// lock (this.snowflakeGoshujin)
        if (exclusiveSnowflake)
        {
            offset = 0;
            if (snowflake != null)
            {// Reset
                this.MovePosition(snowflake, )
                this.DirectorySize -= snowflake.Position;
                snowflake.Position = 0;
            }
            else
            {// New
                snowflake = this.GetNewSnowflake();
            }
        }
        else
        {
            if (snowflake != null)
            {
                if (snowflake.Position < DefaultMaxSnowflakeSize)
                {
                    offset = snowflake.Position;
                    this.EnlargeSnowflake(snowflake, memoryOwner.Memory.Length);
                    return;
                }
            }

            snowflake = this.CurrentSnowflakeId;
            if (snowflake.Position < DefaultMaxSnowflakeSize)
            {
                offset = snowflake.Position;
                this.EnlargeSnowflake(snowflake, memoryOwner.Memory.Length);
                return;
            }

            offset = 0;
            snowflake = this.GetNewSnowflake();
            this.CurrentSnowflakeId = snowflake.SnowflakeId;
        }
    }

    internal void Save(ref ulong io, ref long io2, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner, bool exclusiveSnowflake)
    {// DirectoryId: valid, SnowflakeId: ?
        Snowflake? snowflake;
        var snowflakeId = ZenIdentifier.IOToSnowflakeId(io);
        var offset = ZenIdentifier.IO2ToOffset(io2);
        var count = ZenIdentifier.IO2ToCount(io2);

        lock (this.snowflakeGoshujin)
        {
            if (this.snowflakeGoshujin.SnowflakeIdChain.TryGetValue(snowflakeId, out snowflake))
            {// Found
                if (memoryOwner.Memory.Length <= count)
                {// Ok
                }
                else
                {// Insufficient space
                    this.GetSnowflake(exclusiveSnowflake, ref snowflake, out offset);
                    count = memoryOwner.Memory.Length;
                }
            }
            else
            {// Not found
                snowflake = null;
                this.GetSnowflake(exclusiveSnowflake, ref snowflake, out offset);
                count = memoryOwner.Memory.Length;
            }

            io = ZenIdentifier.DirectorySnowflakeIdToIO(this.DirectoryId, snowflake!.SnowflakeId);
            io2 = ZenIdentifier.OffsetCountToIO2(offset, count);
        }

        var path = this.GetSnowflakeFile(snowflake.SnowflakeId);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Save(path, offset, memoryOwner.IncrementAndShare());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        async static Task Save(string path, int offset, ByteArrayPool.ReadOnlyMemoryOwner m)
        {
            var hash = new byte[HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(m.Memory.Span));

            try
            {
                using (var handle = File.OpenHandle(path, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, hash, offset);
                    await RandomAccess.WriteAsync(handle, m.Memory, offset + HashSize);
                }
            }
            finally
            {
                m.Return();
            }
        }
    }

    internal bool Check()
    {
        try
        {
            Directory.CreateDirectory(this.DirectoryPath);
            /*var directoryInfo = new DirectoryInfo(this.DirectoryPath);
            if (createDirectory)
            {
                directoryInfo.Create();
            }
            else
            {
                if (!directoryInfo.Exists)
                {// No directory

                }
            }*/

            /*var testFile = Path.Combine(this.DirectoryPath, Path.GetRandomFileName());
            using (var fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
            {
            }*/

            // Check directory file
            using (var handle = File.OpenHandle(this.DirectoryFile, mode: FileMode.Open, access: FileAccess.ReadWrite))
            {
            }
        }
        catch
        {// No directory file
            return false;
        }

        return true;
    }

    internal void Start()
    {
        // Directory.CreateDirectory(this.DirectoryPath);

        if (!this.TryLoadDirectory(this.DirectoryFile))
        {
            this.TryLoadDirectory(this.DirectoryBackup);
        }
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint DirectoryId { get; private set; }

    [Key(1)]
    public string DirectoryPath { get; private set; } = string.Empty;

    [Key(2)]
    public long DirectoryCapacity { get; private set; }

    [Key(3)]
    public long DirectorySize { get; private set; }

    [Key(4)]
    public uint CurrentSnowflakeId { get; private set; }

    public string DirectoryFile => Path.Combine(this.DirectoryPath, Zen.DefaultDirectoryFile);

    public string DirectoryBackup => Path.Combine(this.DirectoryPath, Zen.DefaultDirectoryBackup);

    private bool TryLoadDirectory(string path)
    {
        ReadOnlySpan<byte> span;
        try
        {
            span = File.ReadAllBytes(path).AsSpan();
        }
        catch
        {
            return false;
        }

        if (!HashHelper.CheckFarmHashAndGetData(span, out var data))
        {
            return false;
        }

        return true;
    }

    private Snowflake GetNewSnowflake()
    {// lock (this.snoflakeGoshujin)
        while (true)
        {
            var id = LP.Random.Pseudo.NextUInt32();
            if (id != 0 && !this.snowflakeGoshujin.SnowflakeIdChain.ContainsKey(id))
            {
                var snowflake = new Snowflake(id);
                this.snowflakeGoshujin.Add(snowflake);
                return snowflake;
            }
        }
    }

    private void EnlargeSnowflake(Snowflake snowflake, int size)
    {// lock (this.snoflakeGoshujin)
        var i = size + HashSize;
        snowflake.Position += i;
        this.DirectorySize += i;
    }

    private string GetSnowflakeFile(uint snowflakeId)
    {
        Span<char> c = stackalloc char[8];
        c[0] = (char)('a' + ((snowflakeId & 0xF0000000) >> 28));
        c[1] = (char)('a' + ((snowflakeId & 0xF0000000) >> 24));
        c[2] = (char)('a' + ((snowflakeId & 0xF0000000) >> 20));
        c[3] = (char)('a' + ((snowflakeId & 0xF0000000) >> 16));
        c[4] = (char)('a' + ((snowflakeId & 0xF0000000) >> 12));
        c[5] = (char)('a' + ((snowflakeId & 0xF0000000) >> 8));
        c[6] = (char)('a' + ((snowflakeId & 0xF0000000) >> 4));
        c[7] = (char)('a' + snowflakeId & 0xF0000000);

        return Path.Combine(this.DirectoryPath, c.ToString());
    }

    private Snowflake.GoshujinClass snowflakeGoshujin = new();
}
