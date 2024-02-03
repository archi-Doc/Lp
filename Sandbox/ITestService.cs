// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Drawing;
using Arc.Crypto;
using Netsphere;
using Netsphere.Net;
using Netsphere.Server;

namespace Sandbox;

[NetServiceInterface]
public interface TestService : INetService
{
    public NetTask<byte[]?> Pingpong(byte[] data);

    public NetTask<ulong> GetHash(byte[] data);

    public NetTask<ReceiveStream?> ReceiveStream(long length);

    public NetTask<SendStream?> SendStream(ulong dataId);
}

[NetServiceObject]
public class TestServiceImpl : TestService
{
    public const long MaxStreamLength = 100_000_000;

    public async NetTask<byte[]?> Pingpong(byte[] data)
        => data;

    public async NetTask<ulong> GetHash(byte[] data)
        => Arc.Crypto.FarmHash.Hash64(data);

    public async NetTask<StreamService?> ReceiveStream(long length)
    {
        length = Math.Min(length, MaxStreamLength);
        var r = new Xoshiro256StarStar((ulong)length);
        var buffer = new byte[length];
        r.NextBytes(buffer);

        var (_, stream) = await TransmissionContext.Current.SendStream(length, Arc.Crypto.FarmHash.Hash64(buffer));

        if (stream is not null)
        {
            await stream.Send(buffer);
            await stream.Complete();
        }

        return stream;
    }
}
