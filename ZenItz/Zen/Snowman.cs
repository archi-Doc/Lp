// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class Snowman
{
    public const uint DefaultSnowmanId = 1;

    public Snowman()
    {
    }

    public Snowman(string directory)
    {// Default snowman
        this.SnowmanId = DefaultSnowmanId;
        this.SnowmanDirectory = directory;
    }

    public bool Check()
    {
        try
        {
            Directory.CreateDirectory(this.SnowmanDirectory);
            /*var directoryInfo = new DirectoryInfo(this.SnowmanDirectory);
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

            /*var testFile = Path.Combine(this.SnowmanDirectory, Path.GetRandomFileName());
            using (var fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
            {
            }*/

            // Check snowman file
            using (var handle = File.OpenHandle(this.SnowmanFile, mode: FileMode.Open, access: FileAccess.ReadWrite))
            {
            }
        }
        catch
        {// No snowman file
            return false;
        }

        return true;
    }

    public void Start()
    {
        // Directory.CreateDirectory(this.SnowmanDirectory);

        if (!this.TryLoadSnowman(this.SnowmanFile))
        {
            this.TryLoadSnowman(this.SnowmanBackup);
        }
    }

    public async Task<ZenDataResult> Load(SnowFlakeIdSegment idSegment, Identifier identifier)
    {
        return new ZenDataResult(ZenResult.Success, ByteArrayPool.ReadOnlyMemoryOwner.Empty);
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint SnowmanId { get; private set; }

    [Key(1)]
    public string SnowmanDirectory { get; private set; } = string.Empty;

    public string SnowmanFile => Path.Combine(this.SnowmanDirectory, Zen.DefaultSnowmanFile);

    public string SnowmanBackup => Path.Combine(this.SnowmanDirectory, Zen.DefaultSnowmanBackup);

    private bool TryLoadSnowman(string path)
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

        if (span.Length < 8)
        {
            return false;
        }

        var data = span.Slice(8);
        if (Arc.Crypto.FarmHash.Hash64(data) != BitConverter.ToUInt64(span))
        {
            return false;
        }

        return true;
    }
}
