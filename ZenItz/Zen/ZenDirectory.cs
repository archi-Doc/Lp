// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class ZenDirectory
{
    public const int DefaultMaxSnowflakeSize = 1024 * 1024 * 1024; // 1GB = 4MB x 256
    public const int HashSize = 8;

    public enum DirectoryType
    {
        Standard,
    }

    public ZenDirectory()
    {
    }

    public ZenDirectory(uint directoryId, string path)
    {
        this.DirectoryId = directoryId;
        this.DirectoryPath = path;
    }

    internal async Task<ZenDataResult> Load(ulong file)
    {
        Snowflake? snowflake;
        var snowflakeId = ZenFile.ToSnowflakeId(file);
        int size = 0;

        lock (this.snowflakeGoshujin)
        {
            if (snowflakeId != 0 &&
                this.snowflakeGoshujin.SnowflakeIdChain.TryGetValue(snowflakeId, out snowflake))
            {// Found
                size = snowflake.Size;
            }
            else
            {// Not found
                return new(ZenResult.NoFile);
            }
        }

        // Load (snowflakeId, size)

        return new(ZenResult.Success);
    }

    internal void Save(ref ulong file, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {// DirectoryId: valid, SnowflakeId: ?
        Snowflake? snowflake;
        var snowflakeId = ZenFile.ToSnowflakeId(file);
        var dataSize = memoryOwner.Memory.Length;

        lock (this.snowflakeGoshujin)
        {
            if (snowflakeId != 0 &&
                this.snowflakeGoshujin.SnowflakeIdChain.TryGetValue(snowflakeId, out snowflake))
            {// Found
                if (dataSize > snowflake.Size)
                {
                    this.DirectorySize += dataSize - snowflake.Size;
                }

                snowflake.Size = dataSize;
            }
            else
            {// Not found
                snowflake = this.GetNewSnowflake();
                this.DirectorySize += dataSize; // Forget about hash size.
                snowflake.Size = dataSize;
            }

            file = ZenFile.ToFile(this.DirectoryId, snowflake.SnowflakeId);
        }

        var path = this.GetSnowflakeFile(snowflake.SnowflakeId);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Save(path, memoryOwner.IncrementAndShare());
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        async static Task Save(string path, ByteArrayPool.ReadOnlyMemoryOwner m)
        {
            var hash = new byte[HashSize];
            BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(m.Memory.Span));

            try
            {
                using (var handle = File.OpenHandle(path, mode: FileMode.OpenOrCreate, access: FileAccess.Write))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0);
                    await RandomAccess.WriteAsync(handle, m.Memory, HashSize);
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
    public DirectoryType Type { get; private set; }

    [Key(2)]
    public string DirectoryPath { get; private set; } = string.Empty;

    [Key(3)]
    public long DirectoryCapacity { get; private set; }

    [Key(4)]
    public long DirectorySize { get; private set; }

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

    private string GetSnowflakeFile(uint snowflakeId)
    {
        Span<char> c = stackalloc char[9];
        c[0] = (char)('a' + ((snowflakeId & 0xF0000000) >> 28));
        c[1] = (char)('a' + ((snowflakeId & 0x0F000000) >> 24));
        c[2] = '\\';
        c[3] = (char)('a' + ((snowflakeId & 0x00F00000) >> 20));
        c[4] = (char)('a' + ((snowflakeId & 0x000F0000) >> 16));
        c[5] = (char)('a' + ((snowflakeId & 0x0000F000) >> 12));
        c[6] = (char)('a' + ((snowflakeId & 0x00000F00) >> 8));
        c[7] = (char)('a' + ((snowflakeId & 0x000000F0) >> 4));
        c[8] = (char)('a' + snowflakeId & 0x0000000F);

        return Path.Combine(this.DirectoryPath, c.ToString());
    }

    private Snowflake.GoshujinClass snowflakeGoshujin = new();
}
