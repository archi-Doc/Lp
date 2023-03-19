// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Filer;

#pragma warning disable SA1124 // Do not use regions

namespace CrystalData.Storager;

[TinyhandObject]
internal partial class SimpleStorage : IDisposable, IStorage
{
    public SimpleStorage()
    {
    }

    internal SimpleStorage(ushort directoryId, string path)
        : base()
    {
        this.DirectoryId = directoryId;
        this.DirectoryPath = path;
    }

    public CrystalDirectoryInformation GetInformation()
    {
        this.CalculateUsageRatio();
        return new(this.DirectoryId, this.Type, this.DirectoryPath, this.DirectoryCapacity, this.DirectorySize, this.UsageRatio);
    }

    IAbortOrCompleteTask? IStorage.Get(ulong fileId)
    {
        var file = FileIdToFile(fileId);
        var size = FileIdToSize(fileId);
        if (file == 0 || this.filer == null)
        {
            return null;
        }

        // Load (snowflakeId, size)
        return this.filer.Get(FileToPath(file), size);
    }

    IAbortOrCompleteTask? IStorage.Put(ref ulong fileId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeShared)
    {
        var dataSize = dataToBeShared.Memory.Length;
        var file = FileIdToFile(fileId);
        var size = FileIdToSize(fileId);
        if (this.filer == null)
        {
            return null;
        }

        if (file != 0)
        {// Found
            if (dataSize > size)
            {
                this.DirectorySize += dataSize - size;
            }
        }
        else
        {// Not found
            file = this.GetNewSnowflake();
            this.DirectorySize += dataSize; // Forget about the hash size.
        }

        fileId = FileAndSizeToFileId(file, dataSize);
        return this.filer.Put(FileToPath(file), dataToBeShared);
    }

    IAbortOrCompleteTask? IStorage.Delete(ref ulong fileId)
    {
        var file = FileIdToFile(fileId);
        if (file == 0 || this.filer == null)
        {
            return null;
        }

        fileId = 0;
        return this.filer.Delete(FileToPath(file));
    }

    bool IStorage.PrepareAndCheck(StorageClass storage)
    {
        this.Options = storage.Options;
        try
        {
            if (Path.IsPathRooted(this.DirectoryPath))
            {
                this.RootedPath = this.DirectoryPath;
            }
            else
            {
                this.RootedPath = Path.Combine(this.Options.RootPath, this.DirectoryPath);
            }

            Directory.CreateDirectory(this.RootedPath);

            // Check directory file
            try
            {
                using (var handle = File.OpenHandle(this.SnowflakeFilePath, mode: FileMode.Open, access: FileAccess.ReadWrite))
                {
                }
            }
            catch
            {
                using (var handle = File.OpenHandle(this.SnowflakeBackupPath, mode: FileMode.Open, access: FileAccess.ReadWrite))
                {
                }
            }
        }
        catch
        {// No directory file
            return false;
        }

        if (this.Options.EnableLogger)
        {
            // this.Logger = storage.UnitLogger.GetLogger<CrystalDirectory>();
        }

        return true;
    }

    internal void Start()
    {
        if (!this.TryLoadDirectory(this.SnowflakeFilePath))
        {
            this.TryLoadDirectory(this.SnowflakeBackupPath);
        }
    }

    internal async Task WaitForCompletionAsync()
    {
        await this.worker.WaitForCompletionAsync().ConfigureAwait(false);
    }

    internal async Task StopAsync()
    {
        await this.worker.WaitForCompletionAsync().ConfigureAwait(false);
        await this.SaveDirectoryAsync(this.SnowflakeFilePath, this.SnowflakeBackupPath).ConfigureAwait(false);
    }

    [Key(0)]
    [Link(Type = ChainType.Unordered)]
    public ushort DirectoryId { get; private set; }

    [Key(1)]
    public CrystalDirectoryType Type { get; private set; }

    [Key(2)]
    [Link(Type = ChainType.Unordered)]
    public string DirectoryPath { get; private set; } = string.Empty;

    [Key(3)]
    public long DirectoryCapacity { get; internal set; }

    [Key(4)]
    public long DirectorySize { get; private set; } // lock (this.syncObject)

    [IgnoreMember]
    public CrystalOptions Options { get; private set; } = CrystalOptions.Default;

    [IgnoreMember]
    public string RootedPath { get; private set; } = string.Empty;

    public string SnowflakeFilePath => Path.Combine(this.RootedPath, this.Options.SnowflakeFile);

    public string SnowflakeBackupPath => Path.Combine(this.RootedPath, this.Options.SnowflakeBackup);

    [IgnoreMember]
    internal double UsageRatio { get; private set; }

    [IgnoreMember]
    internal ILogger? Logger { get; private set; }

    internal void CalculateUsageRatio()
    {
        if (this.DirectoryCapacity == 0)
        {
            this.UsageRatio = 0;
            return;
        }

        var ratio = (double)this.DirectorySize / this.DirectoryCapacity;
        if (ratio < 0)
        {
            ratio = 0;
        }
        else if (ratio > 1)
        {
            ratio = 1;
        }

        this.UsageRatio = ratio;
    }

    private bool TryLoadDirectory(string path)
    {
        byte[] file;
        try
        {
            file = File.ReadAllBytes(path);
        }
        catch
        {
            return false;
        }

        if (!HashHelper.CheckFarmHashAndGetData(file.AsMemory(), out var data))
        {
            return false;
        }

        try
        {
            var g = TinyhandSerializer.Deserialize<Snowflake.GoshujinClass>(data);
            if (g != null)
            {
                this.snowflakeGoshujin = g;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    private Task<bool> SaveDirectoryAsync(string path, string? backupPath = null)
    {
        byte[] data;
        lock (this.syncObject)
        {
            data = TinyhandSerializer.Serialize(this.snowflakeGoshujin);
        }

        return HashHelper.GetFarmHashAndSaveAsync(data, path, backupPath);
    }

    private Snowflake GetNewSnowflake()
    {// lock (this.syncObject)
        while (true)
        {
            var id = RandomVault.Pseudo.NextUInt32();
            if (id != 0 && !this.snowflakeGoshujin.SnowflakeIdChain.ContainsKey(id))
            {
                var snowflake = new Snowflake(id);
                snowflake.Goshujin = this.snowflakeGoshujin;
                return snowflake;
            }
        }
    }

    private object syncObject = new();
    private Snowflake.GoshujinClass snowflakeGoshujin = new(); // lock (this.syncObject)
    // private Dictionary<uint, Snowflake> dictionary = new(); // lock (this.syncObject)
    private IFiler? filer;

    #region Helper

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FileIdToFile(ulong fileId) => (uint)(fileId >> 32);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FileIdToSize(ulong fileId) => (int)fileId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FileAndSizeToFileId(uint file, int size) => (file << 32) | (uint)size;

    public static string FileToPath(uint file)
    {
        Span<char> c = stackalloc char[9];
        var n = 0;

        c[n++] = UInt32ToChar(file >> 28);
        c[n++] = UInt32ToChar(file >> 24);

        c[n++] = '/';

        c[n++] = UInt32ToChar(file >> 20);
        c[n++] = UInt32ToChar(file >> 16);
        c[n++] = UInt32ToChar(file >> 12);
        c[n++] = UInt32ToChar(file >> 8);
        c[n++] = UInt32ToChar(file >> 4);
        c[n++] = UInt32ToChar(file);

        return c.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char UInt32ToChar(uint x)
    {
        var a = x & 0xF;
        if (a < 10)
        {
            return (char)('0' + a);
        }
        else
        {
            return (char)('W' + a);
        }
    }

    #endregion

    #region IDisposable Support

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="SimpleStorage"/> class.
    /// </summary>
    ~SimpleStorage()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.worker.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
