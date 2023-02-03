// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Crypto;

namespace LP;

public static class HashHelper
{
    public static bool CheckFarmHashAndGetData(ReadOnlyMemory<byte> source, out ReadOnlyMemory<byte> data)
    {
        data = default;
        if (source.Length < 8)
        {
            return false;
        }

        var s = source.Slice(8);
        if (Arc.Crypto.FarmHash.Hash64(s.Span) != BitConverter.ToUInt64(source.Span))
        {
            return false;
        }

        data = s;
        return true;
    }

    public static bool CheckFarmHashAndGetData(ReadOnlySpan<byte> source, out ReadOnlySpan<byte> data)
    {
        data = default;
        if (source.Length < 8)
        {
            return false;
        }

        var s = source.Slice(8);
        if (Arc.Crypto.FarmHash.Hash64(s) != BitConverter.ToUInt64(source))
        {
            return false;
        }

        data = s;
        return true;
    }

    public static async Task<bool> GetFarmHashAndSaveAsync(ReadOnlyMemory<byte> data, string path, string? backupPath)
    {
        var hash = new byte[8];
        var result = false;
        BitConverter.TryWriteBytes(hash, Arc.Crypto.FarmHash.Hash64(data.Span));
        try
        {
            using (var handle = File.OpenHandle(path, mode: FileMode.Create, access: FileAccess.ReadWrite))
            {
                await RandomAccess.WriteAsync(handle, hash, 0);
                await RandomAccess.WriteAsync(handle, data, hash.Length);
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
                    await RandomAccess.WriteAsync(handle, data, hash.Length).ConfigureAwait(false);
                }
            }
            catch
            {
            }
        }

        return result;
    }
}

public class Hash : Sha3_256
{
    public static readonly new string HashName = "SHA3-256";
    public static readonly new uint HashBits = 256;
    public static readonly new uint HashBytes = HashBits / 8;

    public static ObjectPool<Hash> ObjectPool { get; } = new(static () => new Hash());

    public static ObjectPool<Sha3_384> Sha3_384Pool { get; } = new(static () => new Sha3_384());

    public Identifier GetIdentifier(ReadOnlySpan<byte> input)
    {
        return new Identifier(this.GetHashUInt64(input));
    }

    public Identifier IdentifierFinal()
    {
        return new Identifier(this.HashFinalUInt64());
    }
}
