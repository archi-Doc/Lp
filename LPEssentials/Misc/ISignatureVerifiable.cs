// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;

namespace LP;

/*/// <summary>
/// Validate that object members are appropriate.
/// </summary>
/// <typeparam name="T">.</typeparam>
public interface ISignatureVerifiable<T> : IIdentifierVerifiable<T>
    where T : ITinyhandSerialize<T>
{
    Signature GetSignature();

    bool VerifySignature(int level)
    {
        try
        {
            var hash = Hash.ObjectPool.Get();
            var identifier = hash.GetIdentifier((T)this, level);
            Hash.ObjectPool.Return(hash);

            if (!this.GetIdentifier().Equals(identifier))
            {
                return false;
            }

            var signature = this.GetSignature();
            return signature.PublicKey.VerifyIdentifier(identifier, signature.Sign);
        }
        catch
        {
            return false;
        }
    }
}
*/
