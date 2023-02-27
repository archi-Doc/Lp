// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

/// <summary>
/// Monolithic data store.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public partial class Mono<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    public Mono()
    {
    }

    public void Register<TMonoData>(IMonoGroup<TMonoData> group)
        where TMonoData : IMonoData
    {
        if (this.typeToGroup.TryAdd(typeof(TMonoData), group))
        {
            var id = Arc.Crypto.FarmHash.Hash64(typeof(TMonoData).FullName ?? string.Empty);
            this.idToGroup.Add(id, group);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IMonoGroup<TMonoData> GetGroup<TMonoData>()
        where TMonoData : IMonoData
    {
        if (this.typeToGroup.TryGetValue(typeof(TMonoData), out var group))
        {
            return (IMonoGroup<TMonoData>)group;
        }

        throw new InvalidOperationException(typeof(TMonoData).FullName + " is not registered");
    }

    public void Set<TMonoData>(in TIdentifier id, in TMonoData value)
        where TMonoData : IMonoData
        => this.GetGroup<TMonoData>().Set(in id, in value);

    public bool TryGet<TMonoData>(in TIdentifier id, out TMonoData value)
        where TMonoData : IMonoData
        => this.GetGroup<TMonoData>().TryGet(id, out value);

    public bool Remove<TMonoData>(in TIdentifier id)
        where TMonoData : IMonoData
        => this.GetGroup<TMonoData>().Remove(id);

    public int TotalCount()
    {
        int count = 0;
        foreach (var x in this.idToGroup.Values)
        {
            if (x is IMonoGroup group)
            {
                count += group.Count();
            }
        }

        return count;
    }

    public byte[] Serialize<TMonoData>()
        where TMonoData : IMonoData
    {
        var writer = default(Tinyhand.IO.TinyhandWriter);
        try
        {
            this.GetGroup<TMonoData>().Serialize(ref writer);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public bool Deserialize<TMonoData>(ReadOnlySpan<byte> span)
        where TMonoData : IMonoData
    {
        return this.GetGroup<TMonoData>().Deserialize(span, out _);
    }

    public async Task<bool> LoadAsync(string path)
    {
        try
        {
            byte[]? byteArray;
            using (var handle = File.OpenHandle(path, mode: FileMode.Open, access: FileAccess.Read))
            {
                var length = RandomAccess.GetLength(handle);
                if (length < 8)
                {
                    return false;
                }

                var hash = new byte[8];
                var read = await RandomAccess.ReadAsync(handle, hash, 0);
                if (read != hash.Length)
                {
                    return false;
                }

                length -= hash.Length;
                byteArray = new byte[length];
                await RandomAccess.ReadAsync(handle, byteArray, hash.Length);
                if (Arc.Crypto.FarmHash.Hash64(byteArray) != BitConverter.ToUInt64(hash))
                {
                    return false;
                }
            }

            return SerializeHelper.Deserialize(this.idToGroup, byteArray);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAsync(string path, string? backupPath = null)
    {
        var byteArray = SerializeHelper.Serialize(this.idToGroup);
        var hash = new byte[8];
        var result = false;
        BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(byteArray));
        try
        {
            using (var handle = File.OpenHandle(path, mode: FileMode.Create, access: FileAccess.ReadWrite))
            {
                await RandomAccess.WriteAsync(handle, hash, 0);
                await RandomAccess.WriteAsync(handle, byteArray, hash.Length);
                result = true;
            }
        }
        catch
        {
            return false;
        }

        if (backupPath != null)
        {
            try
            {
                using (var handle = File.OpenHandle(backupPath, mode: FileMode.Create, access: FileAccess.ReadWrite))
                {
                    await RandomAccess.WriteAsync(handle, hash, 0);
                    await RandomAccess.WriteAsync(handle, byteArray, hash.Length);
                }
            }
            catch
            {
            }
        }

        return result;
    }

    private readonly Dictionary<ulong, ISimpleSerializable> idToGroup = new();
    private readonly ThreadsafeTypeKeyHashTable<IMonoGroup> typeToGroup = new();
}
