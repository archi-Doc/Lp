// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection.Emit;
using LP.T3CS;
using Tinyhand.IO;

namespace LP;

public static class LPExtentions
{
    public static string To4Hex(this ulong gene) => $"{(ushort)gene:x4}";

    public static string To4Hex(this uint id) => $"{(ushort)id:x4}";

    public static ulong GetFarmHash<T>(this T value)
        where T : ITinyhandSerialize<T>
    {
        TinyhandSerializer.SerializeObjectAndGetTemporarySpan(value, out var temporary, TinyhandSerializerOptions.Selection);
        return FarmHash.Hash64(temporary);
    }

    /*public static ulong GetFarmHash<T>(this T value)
        where T : ITinyhandSerialize<T>, IUnity
    {
        if (value.Hash != 0)
        {
            return value.Hash;
        }

        var hash = Hash.ObjectPool.Get();
        var farmhash = hash.GetFarmHash(value);
        Hash.ObjectPool.Return(hash);
        value.Hash = farmhash;

        return farmhash;
    }*/

    public static bool VerifySignature<T>(this T value, int level, Signature signature)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var hash = Hash.ObjectPool.Get();
            var identifier2 = hash.GetIdentifier(value, level);
            Hash.ObjectPool.Return(hash);

            return signature.PublicKey.VerifyIdentifier(identifier2, signature.Sign);
        }
        catch
        {
            return false;
        }
    }

    public static bool VerifyIdentifierAndSignature<T>(this T value, int level, Identifier identifier, Signature signature)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var hash = Hash.ObjectPool.Get();
            var identifier2 = hash.GetIdentifier(value, level);
            Hash.ObjectPool.Return(hash);

            if (!identifier2.Equals(identifier))
            {
                return false;
            }

            return signature.PublicKey.VerifyIdentifier(identifier2, signature.Sign);
        }
        catch
        {
            return false;
        }
    }

    public static bool VerifyValueToken<T>(this T value, int level, ValueToken valueToken)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            if (!valueToken.Validate())
            {
                return false;
            }

            var hash = Hash.ObjectPool.Get();
            var identifier2 = hash.GetIdentifier(value, level);
            Hash.ObjectPool.Return(hash);

            return valueToken.Signature.PublicKey.VerifyIdentifier(identifier2, valueToken.Signature.Sign);
        }
        catch
        {
            return false;
        }
    }
}
