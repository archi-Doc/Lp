// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class ZenDirectory
{
    public const int DefaultMaxSnowflakeSize = 1024 * 1024 * 1024; // 1GB = 4MB x 256

    public ZenDirectory()
    {
    }

    public ZenDirectory(uint directoryId, string path)
    {
        this.DirectoryId = directoryId;
        this.DirectoryPath = path;
    }

    internal void Save(ref ulong io, ref long io2, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {// DirectoryId: valid, SnowflakeId: ?
        var snowflakeId = ZenIdentifier.IOToSnowflakeId(io);
        var offset = ZenIdentifier.IO2ToOffset(io2);
        var count = ZenIdentifier.IO2ToCount(io2);

        lock (this.snowflakeGoshujin)
        {
            if (this.snowflakeGoshujin.SnowflakeIdChain.TryGetValue(snowflakeId, out var snowflake))
            {// Found
                if (memoryOwner.Memory.Length <= count)
                {// Ok
                }
                else
                {// Insufficient space
                    if (snowflake.Position >= DefaultMaxSnowflakeSize)
                    {// New snowflake
                        snowflake = this.GetFreeSnowflake();
                        offset = 0;
                    }
                    else
                    {
                        offset = snowflake.Position;
                    }

                    count = memoryOwner.Memory.Length;
                    snowflake.Position += memoryOwner.Memory.Length;
                }
            }
            else
            {// Not found
                snowflake = this.GetFreeSnowflake();
                offset = 0;
                count = memoryOwner.Memory.Length;
                snowflake.Position += memoryOwner.Memory.Length;
            }

            io = ZenIdentifier.DirectorySnowflakeIdToIO(this.DirectoryId, snowflake.SnowflakeId);
            io2 = ZenIdentifier.OffsetCountToIO2(offset, count);
        }

        // Save. snowflake.SnowflakeId, offset, memoryOwner
        var m = memoryOwner.IncrementAndShare();
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

    private Snowflake GetFreeSnowflake()
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

    private Snowflake.GoshujinClass snowflakeGoshujin = new();
}
