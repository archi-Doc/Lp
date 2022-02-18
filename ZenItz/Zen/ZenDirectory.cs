// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

namespace ZenItz;

[TinyhandObject]
[ValueLinkObject]
internal partial class ZenDirectory
{
    public const uint DefaultDirectoryId = 1;

    public ZenDirectory()
    {
    }

    public ZenDirectory(uint directoryId, string path)
    {
        this.DirectoryId = directoryId;
        this.DirectoryPath = path;
    }

    public bool Check()
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

    public void Start()
    {
        // Directory.CreateDirectory(this.DirectoryPath);

        if (!this.TryLoadDirectory(this.DirectoryFile))
        {
            this.TryLoadDirectory(this.DirectoryBackup);
        }
    }

    public async Task<ZenDataResult> Load(ZenIdentifier idSegment, Identifier identifier)
    {
        return new ZenDataResult(ZenResult.Success, ByteArrayPool.ReadOnlyMemoryOwner.Empty);
    }

    [Key(0)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public uint DirectoryId { get; private set; }

    [Key(1)]
    public string DirectoryPath { get; private set; } = string.Empty;

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
}
