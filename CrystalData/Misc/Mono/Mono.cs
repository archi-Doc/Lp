// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tinyhand.IO;

namespace CrystalData;

/// <summary>
/// Monolithic data store.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public partial class Mono<TIdentifier> : ITinyhandSerialize<Mono<TIdentifier>>, ITinyhandReconstruct<Mono<TIdentifier>>
    where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    public Mono()
    {
    }

    static void ITinyhandSerialize<Mono<TIdentifier>>.Serialize(ref TinyhandWriter writer, scoped ref Mono<TIdentifier>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        foreach (var x in value.idToGroup)
        {
            writer.Write(x.Key); // Id
            x.Value.Serialize(ref writer, options);
        }
    }

    static void ITinyhandSerialize<Mono<TIdentifier>>.Deserialize(ref TinyhandReader reader, scoped ref Mono<TIdentifier>? value, TinyhandSerializerOptions options)
    {
        value ??= new();

        try
        {
            while (reader.Remaining > 0)
            {
                var id = reader.ReadUInt64();

                if (value.idToGroup.TryGetValue(id, out var x))
                {
                    x.Deserialize(ref reader, options);
                }
            }
        }
        catch
        {
        }
    }

    static void ITinyhandReconstruct<Mono<TIdentifier>>.Reconstruct([NotNull] scoped ref Mono<TIdentifier>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
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
        var options = TinyhandSerializerOptions.Standard;
        try
        {
            this.GetGroup<TMonoData>().Serialize(ref writer, options);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public void Deserialize<TMonoData>(ReadOnlySpan<byte> span)
        where TMonoData : IMonoData
    {
        var reader = new TinyhandReader(span);
        var options = TinyhandSerializerOptions.Standard;
        this.GetGroup<TMonoData>().Deserialize(ref reader, options);
    }

    /*public async Task<bool> LoadAsync(string path)
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
                var read = await RandomAccess.ReadAsync(handle, hash, 0).ConfigureAwait(false);
                if (read != hash.Length)
                {
                    return false;
                }

                length -= hash.Length;
                byteArray = new byte[length];
                await RandomAccess.ReadAsync(handle, byteArray, hash.Length).ConfigureAwait(false);
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
                await RandomAccess.WriteAsync(handle, hash, 0).ConfigureAwait(false);
                await RandomAccess.WriteAsync(handle, byteArray, hash.Length).ConfigureAwait(false);
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
                    await RandomAccess.WriteAsync(handle, hash, 0).ConfigureAwait(false);
                    await RandomAccess.WriteAsync(handle, byteArray, hash.Length).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        return result;
    }*/

    private readonly Dictionary<ulong, ITinyhandSerialize> idToGroup = new();
    private readonly ThreadsafeTypeKeyHashTable<IMonoGroup> typeToGroup = new();
}
