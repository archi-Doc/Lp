// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz.Obsolete;

public class Itz
{
    public Itz()
    {
    }

    public void Register<TPayload>(IItzShip<TPayload> ship)
        where TPayload : IItzPayload
    {
        ItzShipControl.Instance.Register<TPayload>(ship);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IItzShip<TPayload> GetShip<TPayload>()
        where TPayload : IItzPayload
        => ItzShipControl.Instance.GetShip<TPayload>();

    public void Set<TPayload>(in Identifier primaryId, in Identifier secondaryId, in TPayload value)
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Set(in primaryId, in secondaryId, in value);

    public ItzResult Get<TPayload>(in Identifier primaryId, in Identifier secondaryId, out TPayload value)
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Get(primaryId, secondaryId, out value);

    public int Count<TPayload>()
        where TPayload : IItzPayload
        => this.GetShip<TPayload>().Count();

    public int TotalCount()
    {
        int count = 0;
        foreach (var x in ItzShipControl.IdToShip.Values)
        {
            if (x is IItzShip ship)
            {
                count += ship.Count();
            }
        }

        return count;
    }

    public byte[] Serialize<TPayload>()
        where TPayload : IItzPayload
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
        where TPayload : IItzPayload
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

            return LP.SerializeHelper.Deserialize(ItzShipControl.IdToShip, byteArray);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAsync(string path, string? backupPath = null)
    {
        var byteArray = LP.SerializeHelper.Serialize(ItzShipControl.IdToShip);
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
                using (var handle = File.OpenHandle(path, mode: FileMode.CreateNew, access: FileAccess.ReadWrite))
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
}
