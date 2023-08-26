// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace LP;

/// <summary>
/// Validate that object members are appropriate.
/// </summary>
/// <typeparam name="T">.</typeparam>
public interface IIdentifierVerifiable<T>
    where T : ITinyhandSerialize<T>
{
    Identifier GetIdentifier();

    bool VerifyIdentifier();

    bool VerifyIdentifierDefault()
    {
        try
        {
            var hash = Hash.ObjectPool.Get();
            var identifier = hash.GetIdentifier((T)this, TinyhandSerializerOptions.Signature);
            Hash.ObjectPool.Return(hash);

            return this.GetIdentifier().Equals(identifier);
        }
        catch
        {
            return false;
        }
    }
}
