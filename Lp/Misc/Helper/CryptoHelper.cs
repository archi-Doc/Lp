// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Tinyhand.IO;

namespace Lp;

public static class CryptoHelper
{
    /// <summary>
    /// Validate object members and verify that the signature is appropriate.
    /// </summary>
    /// <param name="proof">The proof to be verified.</param>
    /// <returns><see langword="true" />: Success.</returns>
    public static bool ValidateAndVerify(this Proof proof)
    {
        if (!proof.Validate(default))
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            TinyhandSerializer.SerializeObject<Proof>(ref writer, proof, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var result = proof.GetSignatureKey().Verify(rentMemory.Span, proof.Signature);
            rentMemory.Return();
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
