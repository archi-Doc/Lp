// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace LP;

/// <summary>
/// Validate that object members are appropriate.
/// </summary>
/// <typeparam name="T">.</typeparam>
public interface IIdentifierValidatable<T>
    where T : ITinyhandSerialize<T>
{
    Identifier GetIdentifier();

    bool ValidateIdentifier();

    bool Validate()
    {
        try
        {
            var writer = new TinyhandWriter(byte[]);
            TinyhandSerializer.SerializeObject(ref writer, (T)this, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan();

            var bin = TinyhandSerializer.SerializeObject((T)this, TinyhandSerializerOptions.Signature);
            var hash = Hash.ObjectPool.Get();
            var identifier = hash.GetHash(bin);
            Hash.ObjectPool.Return(hash);

            return this.GetIdentifier().Equals(identifier);
        }
        catch
        {
            return false;
        }
    }
}
