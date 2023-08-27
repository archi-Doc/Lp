// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;

namespace LP;

public static class LPExtentions
{
    public static string To4Hex(this ulong gene) => $"{(ushort)gene:x4}";

    public static string To4Hex(this uint id) => $"{(ushort)id:x4}";

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
