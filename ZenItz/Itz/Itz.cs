// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public class Itz : Itz<Identifier>
{
    public const string DefaultItzFile = "Itz.main";
    public const string DefaultItzBackup = "Itz.back";
}

public partial class Itz<TIdentifier>
    where TIdentifier : IEquatable<TIdentifier>
{
    public Itz()
    {
    }

    public void Register<TPayload>(IShip<TPayload> ship)
        where TPayload : IPayload
    {
        if (this.typeToShip.TryAdd(typeof(TPayload), ship))
        {
            var id = Arc.Crypto.FarmHash.Hash64(typeof(TPayload).FullName ?? string.Empty);
            this.idToShip.Add(id, ship);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IShip<TPayload> GetShip<TPayload>()
        where TPayload : IPayload
    {
        if (this.typeToShip.TryGetValue(typeof(TPayload), out var ship))
        {
            return (IShip<TPayload>)ship;
        }

        throw new InvalidOperationException(typeof(TPayload).FullName + " is not registered");
    }

    public void Set<TPayload>(in TIdentifier id, in TPayload value)
        where TPayload : IPayload
        => this.GetShip<TPayload>().Set(in id, in value);

    public ItzResult Get<TPayload>(in TIdentifier id, out TPayload value)
        where TPayload : IPayload
        => this.GetShip<TPayload>().Get(id, out value);

    public int Count<TPayload>()
        where TPayload : IPayload
        => this.GetShip<TPayload>().Count();

    public int TotalCount()
    {
        int count = 0;
        foreach (var x in this.idToShip.Values)
        {
            if (x is IShip ship)
            {
                count += ship.Count();
            }
        }

        return count;
    }

    public byte[] Serialize<TPayload>()
        where TPayload : IPayload
    {
        var writer = default(Tinyhand.IO.TinyhandWriter);
        try
        {
            this.GetShip<TPayload>().Serialize(ref writer);
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    public bool Deserialize<TPayload>(ReadOnlySpan<byte> span)
        where TPayload : IPayload
    {
        return this.GetShip<TPayload>().Deserialize(span, out _);
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

            return LP.SerializeHelper.Deserialize(this.idToShip, byteArray);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAsync(string path, string? backupPath = null)
    {
        var byteArray = LP.SerializeHelper.Serialize(this.idToShip);
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

    private readonly Dictionary<ulong, ILPSerializable> idToShip = new();
    private readonly ThreadsafeTypeKeyHashTable<IShip> typeToShip = new();
}
