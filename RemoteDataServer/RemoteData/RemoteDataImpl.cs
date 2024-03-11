// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[NetServiceObject]
public class RemoteDataImpl : IRemoteData
{
    public async NetTask<ReceiveStream?> Get(string identifier)
    {
        (_, var stream) = TransmissionContext.Current.GetSendStream(100);
        if (stream is not null)
        {
            await stream.Send(default);
            await stream.CompleteSend();
        }

        return default;
    }

    public async NetTask<SendStreamAndReceive<NetResult>?> Put(string identifier, long maxLength)
    {
        var stream = TransmissionContext.Current.GetReceiveStream();
        if (stream is null)
        {
            return default;
        }

        await stream.Receive(default);

        return default;
    }

    public NetTask<SendStreamAndReceive<NetResult>?> Put2(string identifier, ulong hash, long maxLength)
    {
        throw new NotImplementedException();
    }
}
