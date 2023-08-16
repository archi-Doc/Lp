// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
