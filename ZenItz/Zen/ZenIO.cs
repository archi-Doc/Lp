// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public sealed class ZenIO
{
    public const int DirectoryRotationThreshold = 1024 * 1024 * 1; // 100 MB

    public ZenIO()
    {
    }

    public ZenDirectoryInformation[] GetDirectoryInformation()
    {
        return this.directoryGoshujin.Select(a => a.GetInformation()).ToArray();
    }

    public bool Started { get; private set; }

    internal void Save(ref ulong file, ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        ZenDirectory? directory;
        if (this.directoryGoshujin.DirectoryIdChain.Count == 0)
        {// No directory available.
            return;
        }
        else if (!ZenFile.IsValidFile(file) || !this.directoryGoshujin.DirectoryIdChain.TryGetValue(ZenFile.ToDirectoryId(file), out directory))
        {// Get valid directory.
            if (this.directoryRotationCount >= DirectoryRotationThreshold ||
                this.currentDirectory == null)
            {
                this.currentDirectory = this.GetDirectory();
                Volatile.Write(ref this.directoryRotationCount, memoryOwner.Memory.Length);
                if (this.currentDirectory == null)
                {
                    return;
                }
            }
            else
            {
                Interlocked.Add(ref this.directoryRotationCount, memoryOwner.Memory.Length);
            }

            directory = this.currentDirectory;
        }

        directory.Save(ref file, memoryOwner);
    }

    internal async Task<ZenDataResult> Load(ulong file)
    {
        ZenDirectory? directory;
        if (!ZenFile.IsValidFile(file))
        {// Invalid file.
            return new(ZenResult.NoData);
        }
        else if (!this.directoryGoshujin.DirectoryIdChain.TryGetValue(ZenFile.ToDirectoryId(file), out directory))
        {// No directory
            return new(ZenResult.NoDirectory);
        }

        return await directory.Load(file);
    }

    internal async Task<ZenStartResult> TryStart(ZenStartParam param, ReadOnlyMemory<byte>? data)
    {
        if (this.Started)
        {
            return ZenStartResult.Success;
        }

        ZenDirectory.GoshujinClass? goshujin = null;
        if (data != null)
        {
            try
            {
                goshujin = TinyhandSerializer.Deserialize<ZenDirectory.GoshujinClass>(data.Value);
            }
            catch
            {
                if (!await param.Query(ZenStartResult.ZenFileError))
                {
                    return ZenStartResult.ZenFileError;
                }
            }
        }

        goshujin ??= new();
        List<string>? errorDirectories = null;
        foreach (var x in goshujin)
        {
            if (!x.Check())
            {
                errorDirectories ??= new();
                errorDirectories.Add(x.DirectoryPath);
            }
        }

        if (errorDirectories != null &&
            !await param.Query(ZenStartResult.ZenDirectoryError, errorDirectories.ToArray()))
        {
            return ZenStartResult.ZenFileError;
        }

        if (param.DefaultFolder != null && goshujin.DirectoryIdChain.Count == 0)
        {
            try
            {
                Directory.CreateDirectory(param.DefaultFolder);
                var defaultDirectory = new ZenDirectory(this.GetFreeDirectoryId(goshujin), param.DefaultFolder);
                goshujin.Add(defaultDirectory);
            }
            catch
            {
            }
        }

        foreach (var x in goshujin)
        {
            x.Start();
        }

        if (goshujin.DirectoryIdChain.Count == 0)
        {
            return ZenStartResult.NoDirectoryAvailable;
        }

        this.directoryGoshujin = goshujin;

        this.Started = true;
        return ZenStartResult.Success;
    }

    internal async Task WaitForCompletionAsync()
    {
        foreach (var x in this.directoryGoshujin)
        {
            await x.WaitForCompletionAsync().ConfigureAwait(false);
        }
    }

    internal async Task StopAsync()
    {
        if (!this.Started)
        {
            return;
        }

        foreach (var x in this.directoryGoshujin)
        {
            await x.StopAsync().ConfigureAwait(false);
        }

        this.Started = false;
    }

    internal byte[] Serialize()
    {
        return TinyhandSerializer.Serialize(this.directoryGoshujin);
    }

    private uint GetFreeDirectoryId(ZenDirectory.GoshujinClass goshujin)
    {
        while(true)
        {
            var id = LP.Random.Pseudo.NextUInt32();
            if (id != 0 && !goshujin.DirectoryIdChain.ContainsKey(id))
            {
                return id;
            }
        }
    }

    private ZenDirectory? GetDirectory()
    {
        var array = this.directoryGoshujin.ListChain.ToArray();
        if (array == null)
        {
            return null;
        }

        foreach (var x in array)
        {
            x.CalculateUsageRatio();
        }

        return array.MinBy(a => a.UsageRatio);
    }

    private ZenDirectory.GoshujinClass directoryGoshujin = new();
    private ZenDirectory? currentDirectory;
    private int directoryRotationCount;
}
