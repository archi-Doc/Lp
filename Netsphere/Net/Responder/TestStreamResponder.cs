// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Server;

namespace Netsphere.Responder;

public class TestStreamResponder : INetResponder
{
    public const int MaxLength = 1024 * 1024 * 100;

    public static readonly INetResponder Instance = new TestStreamResponder();

    public ulong DataId
        => 123456789;

    public bool Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<int>(transmissionContext.Owner.Memory.Span, out var size))
        {
            transmissionContext.Return();
            return false;
        }

        Task.Run(async () =>
        {
            size = Math.Min(size, MaxLength);
            var r = new Xoshiro256StarStar((ulong)size);
            var buffer = new byte[size];
            r.NextBytes(buffer);

            var (_, stream) = transmissionContext.SendStream(size, FarmHash.Hash64(buffer));
            if (stream is not null)
            {
                await stream.Send(buffer);
                await stream.Complete();
            }
        });

        return true;
    }
}
