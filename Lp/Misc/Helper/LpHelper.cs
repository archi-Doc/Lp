// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public static class LpHelper
{
    /*public static void Sign(this Proof value, SeedKey seedKey)
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            value.PublicKey = seedKey.GetSignaturePublicKey();
            value.SignedMics = Mics.FastCorrected;

            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);
            value.Signature = sign;
        }
        finally
        {
            writer.Dispose();
        }
    }*/

    public static async ValueTask<ClientConnection?> Get(this RobustConnection? robustConnection, ILogger logger)
    {
        if (robustConnection is null)
        {
            return null;
        }

        if (await robustConnection.Get() is not { } connection)
        {
            logger.TryGet()?.Log(Hashed.Error.Connect, robustConnection.DestinationNode.ToString());
            return null;
        }

        return connection;
    }
}
