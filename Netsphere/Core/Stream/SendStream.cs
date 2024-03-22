// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Net;

namespace Netsphere;

public class SendStream : SendStreamBase
{
    internal SendStream(SendTransmission sendTransmission, long maxLength, ulong dataId)
        : base(sendTransmission, maxLength, dataId)
    {
    }

    public async Task<NetResult> Complete(CancellationToken cancellationToken = default)
    {
        var resultAndValue = await this.InternalComplete<NetResult>(DataControl.Complete, cancellationToken).ConfigureAwait(false);
        if (resultAndValue.IsFailure)
        {
            return resultAndValue.Result;
        }
        else
        {
            return resultAndValue.Value;
        }
    }
}
