// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp;

public static class VerificationHelper
{
    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority)
    {
        var context = connection.GetContext();
        var token = new AuthenticationToken(connection.Salt);
        authority.GetSeedKey().Sign(token);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority, Credit credit)
    {
        var context = connection.GetContext();
        var token = new AuthenticationToken(connection.Salt);
        authority.GetSeedKey(credit).Sign(token);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static Identifier GetIdentifier<T>(this T? value, int level)
        where T : ITinyhandSerializable<T>
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = level;
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            var identifier = new Identifier(Blake3.Get256_UInt64(span));
            return identifier;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Validate object members and verify that the signature is appropriate.
    /// </summary>
    /// <param name="proof">The proof to be verified.</param>
    /// <returns><see langword="true" />: Success.</returns>
    public static bool ValidateAndVerify(this Proof proof)
    {
        if (!proof.Validate())
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            TinyhandSerializer.SerializeObject<Proof>(ref writer, proof, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var result = proof.GetPublicKey().Verify(rentMemory.Span, proof.Signature);
            rentMemory.Return();
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
